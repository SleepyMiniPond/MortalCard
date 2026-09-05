using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests.CardTransformation
{
    /// <summary>
    /// 驗證 Self Form、External Override、Buff Layer、PlayerBuff 與 Clone 的跨層整合邊界。
    /// </summary>
    public sealed class CardTransformationIntegrationTests
    {
        private const string OverrideCardId = "integration-override";
        private const string PlayerBuffId = "integration-player-buff";

        [Test]
        public void FullFlow_AtoBtoCThenRelease_PreservesUnderlyingStateAndKeepsCloneIndependent()
        {
            var baseCard = _CreateBaseCardWithTransformRule();
            var overrideCard = new OverrideCardData
            {
                ID = OverrideCardId,
                Cost = 9,
                Power = 13,
                PropertyDatas = { new SealedPropertyData() }
            };
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .WithCard(overrideCard)
                .WithPlayerBuff(_CreatePlayerBuffData())
                .BuildFromInstance(new RecyclePropertyData());
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            var originalIdentity = built.Card.Identity;
            var originalHandOrder = built.Gameplay.Ally.CardManager.HandCard.Cards
                .Select(card => card.Identity)
                .ToArray();

            var selfTransformEvents = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(selfTransformEvents.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Card.CardDataId,
                Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(
                built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Initialize, CardProperty.Recycle }));
            Assert.That(
                built.Gameplay.Ally.CardManager.HandCard.Cards.Select(card => card.Identity),
                Is.EqualTo(originalHandOrder));

            var baseBuff = BuffTestBuilder.CreateCardBuff(
                built.Context,
                built.Gameplay.ContextManager.CardBuffLibrary,
                CardTransformationTestBuilder.CardBuffId);
            built.Card.BuffManager.AddBuff(baseBuff);
            var applyOverrideResult = built.Card.TryApplyOverrideForm(
                "negative-form",
                OverrideCardId,
                SystemSource.Instance,
                new[]
                {
                    new CardFormOverrideReleaseRule
                    {
                        Timing = GameTiming.AfterPlayCardEnd,
                        Conditions = { new ConstCondition { Value = true } }
                    }
                },
                new Dictionary<string, IReactionSessionEntity>());

            Assert.That(applyOverrideResult.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(built.Card.CardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(
                built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Sealed }));
            Assert.That(built.Card.BuffManager.Buffs, Is.Empty);

            built.Gameplay.Ally.BuffManager.AddBuff(
                BuffTestBuilder.CreatePlayerBuff(PlayerBuffId, built.Gameplay.Ally));
            built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            var overrideBuff = built.Card.BuffManager.Buffs.Single();
            Assert.That(overrideBuff.CardBuffDataID,
                Is.EqualTo(CardTransformationTestBuilder.CardBuffId));
            Assert.That(overrideBuff, Is.Not.SameAs(baseBuff));
            Assert.That(baseBuff.Level, Is.EqualTo(1));

            var clone = built.Card.Clone();

            Assert.That(clone.Identity, Is.Not.EqualTo(originalIdentity));
            Assert.That(clone.OriginCardInstanceGuid.HasValue, Is.False);
            Assert.That(clone.BaseCardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(clone.CardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(clone.SelfFormState.HasValue, Is.False);
            Assert.That(clone.OverrideFormState.HasValue, Is.False);
            Assert.That(clone.BuffManager.Buffs, Is.Empty);
            Assert.That(
                clone.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Sealed }));
            Assert.That(clone.IsDisposable(), Is.False);
            Assert.That(clone.ToInfo(built.Gameplay.Manager).CardDataID, Is.EqualTo(OverrideCardId));

            var releaseEvents = built.Gameplay.Manager
                .TriggerTiming(GameTiming.AfterPlayCardEnd, SystemSource.Instance)
                .ToList();

            Assert.That(releaseEvents.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Card.Identity, Is.EqualTo(originalIdentity));
            Assert.That(built.Card.OverrideFormState.HasValue, Is.False);
            Assert.That(built.Card.CardDataId,
                Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(
                built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Initialize, CardProperty.Recycle }));
            Assert.That(
                built.Gameplay.Ally.CardManager.HandCard.Cards.Select(card => card.Identity),
                Is.EqualTo(originalHandOrder));
            Assert.That(built.Card.BuffManager.Buffs.Single(), Is.SameAs(baseBuff));
            Assert.That(
                built.Card.BuffManager.Buffs.Any(buff => ReferenceEquals(buff, overrideBuff)),
                Is.False);
        }

        private static StandardCardData _CreateBaseCardWithTransformRule()
        {
            var card = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                cost: 2,
                power: 3);
            card.PropertyDatas.Add(new PreservedPropertyData());
            card.TransformRules.Add(new CardTransformRule
            {
                RuleId = "apply-alternate",
                TransformKey = "stance",
                Priority = 10,
                Timing = GameTiming.BeforeTurnEnd,
                Conditions = { new ConstCondition { Value = true } },
                Operation = new ApplyCardTransformOperationData
                {
                    TargetCardDataId = CardTransformationTestBuilder.AlternateCardId,
                    Persistence = CardFormPersistence.Persistent
                }
            });
            return card;
        }

        private static PlayerBuffData _CreatePlayerBuffData()
        {
            return BuffTestBuilder.CreatePlayerBuffData(
                PlayerBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new AddCardBuffPlayerBuffEffect
                    {
                        Targets = new CardsOfPlayer
                        {
                            Player = new TriggeredPlayer(),
                            Zone = CardCollectionType.HandCard
                        },
                        AddCardBuffDatas = new List<AddCardBuffData>
                        {
                            new()
                            {
                                CardBuffId = CardTransformationTestBuilder.CardBuffId,
                                Level = new ConstInteger { Value = 1 }
                            }
                        }
                    }
                });
        }
    }
}
