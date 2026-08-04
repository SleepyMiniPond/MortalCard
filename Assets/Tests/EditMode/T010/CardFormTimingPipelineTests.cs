using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.Presenter;
using NUnit.Framework;

namespace MortalGame.Tests.T010
{
    public sealed class CardFormTimingPipelineTests
    {
        [Test]
        public void TriggerTiming_ApplySelfForm_EmitsLatestCardInfoAndKeepsEffectiveForm()
        {
            var baseCard = CreateCardWithApplyRule(
                CardTransformationTestBuilder.BaseCardId,
                CardTransformationTestBuilder.AlternateCardId);
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            var changed = events.OfType<CardFormChangedEvent>().Single();
            Assert.That(changed.CardIdentity, Is.EqualTo(built.Card.Identity));
            Assert.That(changed.BeforeCardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(changed.AfterCardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(changed.TransformKey, Is.EqualTo("stance"));
            Assert.That(changed.Cause, Is.EqualTo(CardFormChangeCause.SelfTransformApplied));
            Assert.That(changed.CardInfo.CardDataID, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(built.Card.ToInfo(built.Gameplay.Manager).CardDataID,
                Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
        }

        [Test]
        public void TriggerTiming_WhenOperationIsNoOp_DoesNotEmitFormChangedEvent()
        {
            var baseCard = CreateCardWithApplyRule(
                CardTransformationTestBuilder.BaseCardId,
                CardTransformationTestBuilder.BaseCardId);
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(events.OfType<CardFormChangedEvent>(), Is.Empty);
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        [Test]
        public void TriggerTiming_RevertSelfForm_EmitsRevertedEventWithBaseCardInfo()
        {
            var baseCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                cost: 2,
                power: 3);
            baseCard.TransformRules.Add(new CardTransformRule
            {
                RuleId = "revert-stance",
                TransformKey = "stance",
                Timing = GameTiming.BeforeTurnEnd,
                Conditions = { new ConstCondition { Value = true } },
                Operation = new RevertCardTransformOperationData()
            });
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            built.Card.TryApplySelfForm(
                "stance",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);

            var changed = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .OfType<CardFormChangedEvent>()
                .Single();

            Assert.That(changed.Cause, Is.EqualTo(CardFormChangeCause.SelfTransformReverted));
            Assert.That(changed.BeforeCardDataId,
                Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(changed.AfterCardDataId,
                Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(changed.CardInfo.CardDataID,
                Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        [Test]
        public void TriggerTiming_FormChanged_ExecutesOnlyNewEffectiveFormEffectsOnce()
        {
            var baseCard = CreateCardWithApplyRule(
                CardTransformationTestBuilder.BaseCardId,
                CardTransformationTestBuilder.AlternateCardId);
            baseCard.TriggeredEffects.Add(CreateFormChangedEnergyEffect(1));

            var alternateCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.AlternateCardId,
                cost: 5,
                power: 8);
            alternateCard.TriggeredEffects.Add(CreateFormChangedEnergyEffect(2));
            alternateCard.TransformRules.Add(new CardTransformRule
            {
                RuleId = "alternate-revert",
                TransformKey = "stance",
                Timing = GameTiming.BeforeTurnEnd,
                Conditions = { new ConstCondition { Value = true } },
                Operation = new RevertCardTransformOperationData()
            });

            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .WithCard(alternateCard)
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(events.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Gameplay.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
        }

        [Test]
        public void CardFormChangedEvent_CanUpdateViewModelWithoutGeneralUpdate()
        {
            var baseCard = CreateCardWithApplyRule(
                CardTransformationTestBuilder.BaseCardId,
                CardTransformationTestBuilder.AlternateCardId);
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            var initialInfo = built.Card.ToInfo(built.Gameplay.Manager);
            var viewModel = new GameViewModel();
            viewModel.UpdateCardInfo(initialInfo);

            var changed = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .OfType<CardFormChangedEvent>()
                .Single();
            viewModel.UpdateCardInfo(changed.CardInfo);

            Assert.That(
                viewModel.ObservableCardInfo(built.Card.Identity).TryGetValue(out var infoProperty),
                Is.True);
            var currentInfo = infoProperty.Value;
            Assert.That(currentInfo.CardDataID,
                Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
        }

        private static CardData CreateCardWithApplyRule(
            string cardId,
            string targetCardDataId)
        {
            var card = CardTransformationTestBuilder.CreateCardData(cardId, cost: 2, power: 3);
            card.TransformRules.Add(new CardTransformRule
            {
                RuleId = "apply-stance",
                TransformKey = "stance",
                Priority = 10,
                Timing = GameTiming.BeforeTurnEnd,
                Conditions = { new ConstCondition { Value = true } },
                Operation = new ApplyCardTransformOperationData
                {
                    TargetCardDataId = targetCardDataId,
                    Persistence = CardFormPersistence.Persistent
                }
            });
            return card;
        }

        private static CardData.TriggeredCardEffect CreateFormChangedEnergyEffect(int value)
        {
            return new CardData.TriggeredCardEffect
            {
                Timing = CardTriggeredTiming.FormChanged,
                Effects = new ICardEffect[]
                {
                    new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection
                        {
                            Target = new CardOwner { Card = new SelectedCard() }
                        },
                        Value = new ConstInteger { Value = value }
                    }
                }
            };
        }
    }
}
