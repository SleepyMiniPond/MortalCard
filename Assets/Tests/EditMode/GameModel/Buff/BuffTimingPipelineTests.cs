using System;
using MortalGame.GameModel;
using System.Linq;
using NUnit.Framework;
using MortalGame.GameData;
using Optional;

namespace MortalGame.Tests
{

    public class BuffTimingPipelineTests
    {
        [Test]
        public void Constructor_WithInitialStatus_UsesControlledStatus()
        {
            var built = new GameplayManagerTestBuilder().Build();

            Assert.AreSame(built.Status, ((IGameplayModel)built.Manager).GameStatus);
            Assert.AreSame(built.Ally, built.Status.Ally);
            Assert.AreSame(built.Enemy, built.Status.Enemy);
        }

        [Test]
        public void TriggerTiming_PlayerBuffAtMatchingTiming_EmitsTriggerBuffStartUpdates()
        {
            var conditionalEffect = new ConditionalPlayerBuffEffect
            {
                Conditions = { new ConstCondition { Value = true } },
                Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                {
                    Type = EffectAttributeAdditionType.PowerAddition,
                    Value = new ConstInteger { Value = 1 }
                }
            };
            var playerBuffData = BuffTestBuilder.CreatePlayerBuffData(
                BuffTestBuilder.PlayerBuffId,
                GameTiming.BeforeTurnEnd,
                conditionalEffect);
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(playerBuffData)
                .Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff());

            var events = built.Manager.TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance).ToList();

            Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(6));
        }

        [Test]
        public void TriggerTiming_PlayerBuffConditionFalse_DoesNotEmitTriggerBuffStartUpdates()
        {
            var conditionalEffect = new ConditionalPlayerBuffEffect
            {
                Conditions = { new ConstCondition { Value = false } },
                Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                {
                    Type = EffectAttributeAdditionType.PowerAddition,
                    Value = new ConstInteger { Value = 1 }
                }
            };
            var playerBuffData = BuffTestBuilder.CreatePlayerBuffData(
                BuffTestBuilder.PlayerBuffId,
                GameTiming.BeforeTurnEnd,
                conditionalEffect);
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(playerBuffData)
                .Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff());

            var events = built.Manager.TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance).ToList();

            Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void TriggerTiming_CharacterBuffAtMatchingTiming_EmitsTriggerBuffStartUpdates()
        {
            var conditionalEffect = new ConditionalCharacterBuffEffect
            {
                Conditions = new ICondition[] { new ConstCondition { Value = true } },
                Effect = new EffectiveDamageCharacterBuffEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new ConstInteger { Value = 1 }
                }
            };
            var characterBuffData = BuffTestBuilder.CreateCharacterBuffData(
                BuffTestBuilder.CharacterBuffId,
                GameTiming.BeforeTurnEnd,
                conditionalEffect);
            var built = new GameplayManagerTestBuilder()
                .WithCharacterBuff(characterBuffData)
                .Build();
            built.Ally.MainCharacter.BuffManager.AddBuff(BuffTestBuilder.CreateCharacterBuff());

            var events = built.Manager.TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance).ToList();

            Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(6));
        }

        [Test]
        public void TriggerTiming_CardBuffAtMatchingTiming_UsesTriggeredCardAndPreservesSelectedCard()
        {
            ICardEntity observedCard = null;
            ICardEntity playerSelectedCard = null;
            var conditionalEffect = new ConditionalCardBuffEffect
            {
                Conditions =
                {
                    new TriggeredAndSelectedCardCondition(
                        () => observedCard.Identity,
                        () => playerSelectedCard.Identity)
                },
                Effect = new NoOpCardBuffEffect()
            };
            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.BeforeTurnEnd,
                conditionalEffect);
            var built = new GameplayManagerTestBuilder()
                .WithCard(CardTestBuilder.CreateCardData())
                .WithCardBuff(cardBuffData)
                .Build();
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            observedCard = CardTestBuilder.CreateCardWithBuff(
                context,
                built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary);
            playerSelectedCard = CardEntity.RuntimeCreateFromId(
                CardTestBuilder.CardId,
                built.ContextManager.CardLibrary,
                built.ContextManager.CardPropertyEntityFactory);
            built.Ally.CardManager.HandCard.AddCard(observedCard);
            built.Ally.CardManager.HandCard.AddCard(playerSelectedCard);

            using var selectedCardScope = built.ContextManager
                .SetSelectedCard(playerSelectedCard.Some());

            var events = built.Manager.TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance).ToList();

            Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(6));
            Assert.That(
                built.ContextManager.Context.SelectedCard,
                Is.EqualTo(playerSelectedCard.Identity));
        }

        [Test]
        public void TimingDispatchPlanner_WhenAllBuffTypesMatchTiming_CreatesGeneralReactionItemsForEachBuffType()
        {
            var playerBuffData = BuffTestBuilder.CreatePlayerBuffData(
                BuffTestBuilder.PlayerBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                    {
                        Type = EffectAttributeAdditionType.PowerAddition,
                        Value = new ConstInteger { Value = 1 }
                    }
                });
            var characterBuffData = BuffTestBuilder.CreateCharacterBuffData(
                BuffTestBuilder.CharacterBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalCharacterBuffEffect
                {
                    Conditions = new ICondition[] { new ConstCondition { Value = true } },
                    Effect = new EffectiveDamageCharacterBuffEffect
                    {
                        Targets = new NoneCharacters(),
                        Value = new ConstInteger { Value = 1 }
                    }
                });
            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalCardBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new NoOpCardBuffEffect()
                });
            var built = new GameplayManagerTestBuilder()
                .WithCard(CardTestBuilder.CreateCardData())
                .WithPlayerBuff(playerBuffData)
                .WithCharacterBuff(characterBuffData)
                .WithCardBuff(cardBuffData)
                .Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff());
            built.Ally.MainCharacter.BuffManager.AddBuff(BuffTestBuilder.CreateCharacterBuff());
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var card = CardTestBuilder.CreateCardWithBuff(
                context,
                built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);

            var snapshot = built.Manager.CreateTimingReactionSnapshot(
                GameTiming.BeforeTurnEnd,
                SystemSource.Instance);
            built.Manager.ObserveRootAction(snapshot.Action).ToList();
            var items = TimingDispatchPlanner
                .Create(built.Manager, snapshot)
                .GeneralReactionItems;

            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(items, Has.Exactly(1).TypeOf<TriggeredPlayerBuffEffectQueueItem>());
            Assert.That(items, Has.Exactly(1).TypeOf<TriggeredCharacterBuffEffectQueueItem>());
            Assert.That(items, Has.Exactly(1).TypeOf<TriggeredCardBuffEffectQueueItem>());
            Assert.AreEqual(GameContext.EMPTY, built.ContextManager.Context);
        }

        [Test]
        public void TriggerTiming_WhenBuffTriggers_ThenAfterTriggerBuffEffectTimingIsProcessed()
        {
            var firstBuffData = BuffTestBuilder.CreatePlayerBuffData(
                "first-buff",
                GameTiming.BeforeTurnEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                    {
                        Type = EffectAttributeAdditionType.PowerAddition,
                        Value = new ConstInteger { Value = 1 }
                    }
                });
            var secondBuffData = BuffTestBuilder.CreatePlayerBuffData(
                "second-buff",
                GameTiming.AfterTriggerBuffEffect,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new PlayerBuffSourceIdCondition("first-buff") },
                    Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                    {
                        Type = EffectAttributeAdditionType.PowerAddition,
                        Value = new ConstInteger { Value = 1 }
                    }
                });
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(firstBuffData)
                .WithPlayerBuff(secondBuffData)
                .Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff("first-buff"));
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff("second-buff"));

            var events = built.Manager.TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance).ToList();

            Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(10));
        }
    }
}

public sealed class TriggeredAndSelectedCardCondition : ICondition
{
    private readonly Func<Guid> _expectedTriggeredCardId;
    private readonly Func<Guid> _expectedSelectedCardId;

    public TriggeredAndSelectedCardCondition(
        Func<Guid> expectedTriggeredCardId,
        Func<Guid> expectedSelectedCardId)
    {
        _expectedTriggeredCardId = expectedTriggeredCardId;
        _expectedSelectedCardId = expectedSelectedCardId;
    }

    public bool Eval(TriggerContext triggerContext)
    {
        return new TriggeredCard()
                .Eval(triggerContext)
                .Map(card => card.Identity == _expectedTriggeredCardId())
                .ValueOr(false) &&
            triggerContext.Model.ContextManager.Context.SelectedCard == _expectedSelectedCardId();
    }
}

public sealed class NoOpCardBuffEffect : ICardBuffEffect
{
}

public sealed class PlayerBuffSourceIdCondition : ICondition
{
    private readonly string _buffId;

    public PlayerBuffSourceIdCondition(string buffId)
    {
        _buffId = buffId;
    }

    public bool Eval(TriggerContext triggerContext)
    {
        return triggerContext.Action.Source is PlayerBuffSource source &&
            source.Buff.PlayerBuffDataId == _buffId;
    }
}
