using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests.T010
{
    public class CardTransformationCharacterizationTests
    {
        [Test]
        public void CardInstance_Create_DefaultsToNoPersistentForm()
        {
            var cardData = CardTransformationTestBuilder.CreateCardData(
                "t010-card-instance-base",
                cost: 1,
                power: 1);

            var instance = CardInstance.Create(cardData);

            Assert.That(instance.CardDataId, Is.EqualTo(cardData.ID));
            Assert.That(instance.PersistentFormState.HasValue, Is.False);
        }

        [Test]
        public void CardInstance_CanCarryPersistentFormWithoutOverwritingBaseCardDataId()
        {
            var persistentForm = new PersistentCardFormState(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId);

            var instance = new CardInstance(
                Guid.NewGuid(),
                CardTransformationTestBuilder.BaseCardId,
                Array.Empty<ICardPropertyData>(),
                persistentForm.Some());

            Assert.That(instance.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(
                instance.PersistentFormState.ValueOr((PersistentCardFormState)null),
                Is.EqualTo(persistentForm));
        }

        [Test]
        public void CreateFromInstance_RestoresPersistentFormAndKeepsBaseAndInstanceProperties()
        {
            var built = new CardTransformationTestBuilder().BuildFromInstance(
                new PersistentCardFormState(
                    "alternate",
                    CardTransformationTestBuilder.AlternateCardId),
                new RecyclePropertyData());

            var restoredForm = built.Card.SelfFormState.ValueOr((CardFormState)null);

            Assert.That(built.Card.BaseCardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(restoredForm.TransformKey, Is.EqualTo("alternate"));
            Assert.That(restoredForm.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(restoredForm.Persistence, Is.EqualTo(CardFormPersistence.Persistent));
            Assert.That(
                built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Initialize, CardProperty.Recycle }));
            Assert.That(built.Card.OriginCardInstanceGuid.HasValue, Is.True);
        }

        [Test]
        public void CreateFromInstance_RestoredPersistentFormCanRevertToBaseForm()
        {
            var built = new CardTransformationTestBuilder().BuildFromInstance(
                new PersistentCardFormState(
                    "alternate",
                    CardTransformationTestBuilder.AlternateCardId));

            var result = built.Card.TryRevertSelfForm("alternate");

            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.Reverted));
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(built.Card.SelfFormState.HasValue, Is.False);
            Assert.That(
                built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Preserved }));
        }

        [Test]
        public void CardInstancePersistenceMapper_NoSelfFormOrBattleOnlyForm_ClearsPersistentForm()
        {
            var withoutSelfForm = new CardTransformationTestBuilder().BuildFromInstance();
            var battleOnlyForm = new CardTransformationTestBuilder().BuildFromInstance();
            battleOnlyForm.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.BattleOnly);

            var withoutSelfFormResult = CardInstancePersistenceMapper.TryUpdate(
                withoutSelfForm.Card,
                _CreateMatchingInstance(withoutSelfForm.Card));
            var battleOnlyFormResult = CardInstancePersistenceMapper.TryUpdate(
                battleOnlyForm.Card,
                _CreateMatchingInstance(battleOnlyForm.Card));

            Assert.That(
                withoutSelfFormResult.ValueOr((CardInstance)null).PersistentFormState.HasValue,
                Is.False);
            Assert.That(
                battleOnlyFormResult.ValueOr((CardInstance)null).PersistentFormState.HasValue,
                Is.False);
        }

        [Test]
        public void CardInstancePersistenceMapper_PersistentForm_ReturnsCurrentForm()
        {
            var built = new CardTransformationTestBuilder().BuildFromInstance();
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);

            var updatedInstance = CardInstancePersistenceMapper
                .TryUpdate(built.Card, _CreateMatchingInstance(built.Card))
                .ValueOr((CardInstance)null);

            var persistentForm = updatedInstance.PersistentFormState.ValueOr((PersistentCardFormState)null);
            Assert.That(persistentForm.TransformKey, Is.EqualTo("alternate"));
            Assert.That(persistentForm.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
        }

        [Test]
        public void CardInstancePersistenceMapper_PersistentReplacedByBattleOnly_DoesNotRestorePreviousForm()
        {
            const string battleOnlyCardId = "t010-battle-only-persistence-card";
            var built = new CardTransformationTestBuilder()
                .WithCard(CardTransformationTestBuilder.CreateCardData(battleOnlyCardId, 1, 1))
                .BuildFromInstance();
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);
            built.Card.TryApplySelfForm(
                "alternate",
                battleOnlyCardId,
                CardFormPersistence.BattleOnly);

            var result = CardInstancePersistenceMapper.TryUpdate(
                built.Card,
                _CreateMatchingInstance(built.Card));

            Assert.That(result.ValueOr((CardInstance)null).PersistentFormState.HasValue, Is.False);
        }

        [Test]
        public void CardInstancePersistenceMapper_PersistentReplacedByPersistent_ReturnsLatestForm()
        {
            const string latestCardId = "t010-latest-persistent-card";
            var built = new CardTransformationTestBuilder()
                .WithCard(CardTransformationTestBuilder.CreateCardData(latestCardId, 1, 1))
                .BuildFromInstance();
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);
            built.Card.TryApplySelfForm(
                "alternate",
                latestCardId,
                CardFormPersistence.Persistent);

            var result = CardInstancePersistenceMapper
                .TryUpdate(built.Card, _CreateMatchingInstance(built.Card))
                .ValueOr((CardInstance)null)
                .PersistentFormState
                .ValueOr((PersistentCardFormState)null);

            Assert.That(result.TransformKey, Is.EqualTo("alternate"));
            Assert.That(result.CardDataId, Is.EqualTo(latestCardId));
        }

        [Test]
        public void CardInstancePersistenceMapper_MatchingOriginUpdatesOnlyPersistentForm()
        {
            var additionProperties = new ICardPropertyData[] { new RecyclePropertyData() };
            var built = new CardTransformationTestBuilder().BuildFromInstance(additionProperties);
            var originGuid = built.Card.OriginCardInstanceGuid.ValueOr(Guid.Empty);
            var originalInstance = new CardInstance(
                originGuid,
                CardTransformationTestBuilder.BaseCardId,
                additionProperties);
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);

            var updatedInstance = CardInstancePersistenceMapper
                .TryUpdate(built.Card, originalInstance)
                .ValueOr((CardInstance)null);

            Assert.That(updatedInstance.InstanceGuid, Is.EqualTo(originalInstance.InstanceGuid));
            Assert.That(updatedInstance.CardDataId, Is.EqualTo(originalInstance.CardDataId));
            Assert.That(updatedInstance.AdditionPropertyDatas, Is.SameAs(originalInstance.AdditionPropertyDatas));
            Assert.That(originalInstance.PersistentFormState.HasValue, Is.False);
            Assert.That(
                updatedInstance.PersistentFormState.ValueOr((PersistentCardFormState)null),
                Is.EqualTo(new PersistentCardFormState(
                    "alternate",
                    CardTransformationTestBuilder.AlternateCardId)));
        }

        [Test]
        public void CardInstancePersistenceMapper_BattleOnlyFormClearsPreviousPersistentForm()
        {
            var previousForm = new PersistentCardFormState(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId);
            const string battleOnlyCardId = "t010-writeback-battle-only-card";
            var built = new CardTransformationTestBuilder()
                .WithCard(CardTransformationTestBuilder.CreateCardData(battleOnlyCardId, 1, 1))
                .BuildFromInstance(previousForm);
            var originalInstance = new CardInstance(
                built.Card.OriginCardInstanceGuid.ValueOr(Guid.Empty),
                CardTransformationTestBuilder.BaseCardId,
                Array.Empty<ICardPropertyData>(),
                previousForm.Some());
            built.Card.TryApplySelfForm(
                "alternate",
                battleOnlyCardId,
                CardFormPersistence.BattleOnly);

            var updatedInstance = CardInstancePersistenceMapper
                .TryUpdate(built.Card, originalInstance)
                .ValueOr((CardInstance)null);

            Assert.That(originalInstance.PersistentFormState.HasValue, Is.True);
            Assert.That(updatedInstance.PersistentFormState.HasValue, Is.False);
        }

        [Test]
        public void CardInstancePersistenceMapper_MismatchedOrMissingOriginReturnsNone()
        {
            var builtFromInstance = new CardTransformationTestBuilder().BuildFromInstance();
            var runtimeCard = new CardTransformationTestBuilder().Build().Card;
            var clone = builtFromInstance.Card.Clone();
            var mismatchedInstance = new CardInstance(
                Guid.NewGuid(),
                CardTransformationTestBuilder.BaseCardId,
                Array.Empty<ICardPropertyData>());

            var mismatchedResult = CardInstancePersistenceMapper.TryUpdate(
                builtFromInstance.Card,
                mismatchedInstance);
            var runtimeCardResult = CardInstancePersistenceMapper.TryUpdate(
                runtimeCard,
                mismatchedInstance);
            var cloneResult = CardInstancePersistenceMapper.TryUpdate(
                clone,
                mismatchedInstance);

            Assert.That(mismatchedResult.HasValue, Is.False);
            Assert.That(runtimeCardResult.HasValue, Is.False);
            Assert.That(cloneResult.HasValue, Is.False);
        }

        [Test]
        public void CardLibrary_OverrideCardSupportsCommonLookupButNotStandardLookup()
        {
            const string overrideCardId = "t010-override-card";
            var overrideCardData = new OverrideCardData
            {
                ID = overrideCardId,
                Cost = 3,
                Power = 4
            };
            var library = new CardLibrary(new Dictionary<string, CardData>
            {
                [overrideCardId] = overrideCardData
            });

            var commonCardData = library.GetCardData(overrideCardId);
            LogAssert.Expect(
                LogType.Error,
                $"Card ID[{overrideCardId}] 不是 Standard CardData。");
            var standardCardData = library.GetStandardCardData(overrideCardId);

            Assert.That(commonCardData, Is.SameAs(overrideCardData));
            Assert.That(commonCardData.Cost, Is.EqualTo(3));
            Assert.That(commonCardData.Power, Is.EqualTo(4));
            Assert.That(standardCardData, Is.Null);
        }

        [Test]
        public void CardInfo_Create_UsesCardEntityCurrentData()
        {
            var built = new CardTransformationTestBuilder().Build();

            var info = CardInfo.Create(built.Card, built.Context);

            Assert.That(info.Identity, Is.EqualTo(built.Card.Identity));
            Assert.That(info.CardDataID, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(info.OriginCost, Is.EqualTo(2));
            Assert.That(info.OriginPower, Is.EqualTo(3));
        }

        [Test]
        public void CardBuffLayerManager_AddSameDataId_RejectsDuplicateBuff()
        {
            var built = new CardTransformationTestBuilder().Build();
            var firstBuff = BuffTestBuilder.CreateCardBuff(
                built.Context,
                built.Gameplay.ContextManager.CardBuffLibrary,
                CardTransformationTestBuilder.CardBuffId);
            var duplicateBuff = BuffTestBuilder.CreateCardBuff(
                built.Context,
                built.Gameplay.ContextManager.CardBuffLibrary,
                CardTransformationTestBuilder.CardBuffId);
            built.Card.BuffManager.AddBuff(firstBuff);

            var exception = Assert.Throws<Exception>(() => built.Card.BuffManager.AddBuff(duplicateBuff));

            Assert.That(exception.Message, Does.Contain(CardTransformationTestBuilder.CardBuffId));
            Assert.That(built.Card.BuffManager.Buffs, Has.Count.EqualTo(1));
            Assert.That(built.Card.BuffManager.Buffs.Single(), Is.SameAs(firstBuff));
        }

        [Test]
        public void PlayerCardManager_GetCardOrNone_FindsCardByIdentityAcrossZonesAndPlayingCard()
        {
            var built = new CardTransformationTestBuilder().Build();
            var cardManager = built.Gameplay.Ally.CardManager;
            cardManager.HandCard.AddCard(built.Card);

            var foundInHand = cardManager.GetCardOrNone(card => card.Identity == built.Card.Identity);
            var playResult = cardManager.TryPlayCard(built.Card, out _, out _);
            var foundWhilePlaying = cardManager.GetCardOrNone(card => card.Identity == built.Card.Identity);

            Assert.That(foundInHand.ValueOr(CardEntity.DummyCard), Is.SameAs(built.Card));
            Assert.That(playResult.Success, Is.True);
            Assert.That(foundWhilePlaying.ValueOr(CardEntity.DummyCard), Is.SameAs(built.Card));
        }

        [Test]
        public void EffectQueueRunner_ProcessesFifoItemsAndImmediateItemsBeforeQueueTail()
        {
            var runner = new EffectQueueRunner();
            runner.Enqueue(new TestQueueItem(null, 1, enqueueImmediate: true));
            runner.Enqueue(new TestQueueItem(null, 3));

            var result = runner.RunToCompletion();

            Assert.That(
                result.Events.OfType<QueuedTestEvent>().Select(evt => evt.Id).ToArray(),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void TryApplySelfForm_RebuildsCardDataPropertiesAndPreservesRuntimeIdentityAndComponents()
        {
            var built = new CardTransformationTestBuilder().BuildFromInstance(new RecyclePropertyData());
            var buffManager = built.Card.BuffManager;
            var identity = built.Card.Identity;
            var originGuid = built.Card.OriginCardInstanceGuid;

            var result = built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.BattleOnly);

            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(built.Card.BaseCardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(built.Card.Type, Is.EqualTo(CardType.Defense));
            Assert.That(built.Card.OriginCost, Is.EqualTo(5));
            Assert.That(built.Card.OriginPower, Is.EqualTo(8));
            Assert.That(built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Initialize, CardProperty.Recycle }));
            Assert.That(built.Card.Identity, Is.EqualTo(identity));
            Assert.That(built.Card.OriginCardInstanceGuid, Is.EqualTo(originGuid));
            Assert.That(built.Card.BuffManager, Is.SameAs(buffManager));
        }

        [Test]
        public void TryApplySelfForm_TargetAlreadyEffective_ReturnsNoOpWithoutCreatingFormState()
        {
            var built = new CardTransformationTestBuilder().Build();

            var result = built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.BaseCardId,
                CardFormPersistence.Persistent);

            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.NoOp));
            Assert.That(built.Card.SelfFormState.HasValue, Is.False);
        }

        [Test]
        public void TryRevertSelfForm_OnlyRevertsMatchingKeyAndReturnsToBaseForm()
        {
            var built = new CardTransformationTestBuilder().Build();
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);

            var mismatchedResult = built.Card.TryRevertSelfForm("other");
            var revertedResult = built.Card.TryRevertSelfForm("alternate");

            Assert.That(mismatchedResult.Status, Is.EqualTo(CardFormOperationStatus.NoOp));
            Assert.That(revertedResult.Status, Is.EqualTo(CardFormOperationStatus.Reverted));
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Preserved }));
            Assert.That(built.Card.SelfFormState.HasValue, Is.False);
        }

        [Test]
        public void TryApplySelfForm_ReplacingPersistentFormWithBattleOnlyFormKeepsOnlyCurrentFormState()
        {
            var built = new CardTransformationTestBuilder()
                .WithCard(CardTransformationTestBuilder.CreateCardData("t010-battle-only-card", 1, 1))
                .Build();
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);

            built.Card.TryApplySelfForm(
                "alternate",
                "t010-battle-only-card",
                CardFormPersistence.BattleOnly);

            var currentForm = built.Card.SelfFormState.ValueOr((CardFormState)null);
            Assert.That(currentForm.CardDataId, Is.EqualTo("t010-battle-only-card"));
            Assert.That(currentForm.Persistence, Is.EqualTo(CardFormPersistence.BattleOnly));
        }

        [Test]
        public void Clone_UsesCurrentEffectiveFormWithoutCopyingRuntimeState()
        {
            var built = new CardTransformationTestBuilder().BuildFromInstance(new RecyclePropertyData());
            var buff = BuffTestBuilder.CreateCardBuff(
                built.Context,
                built.Gameplay.ContextManager.CardBuffLibrary,
                CardTransformationTestBuilder.CardBuffId);
            built.Card.BuffManager.AddBuff(buff);
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);

            var clone = built.Card.Clone();

            Assert.That(clone.Identity, Is.Not.EqualTo(built.Card.Identity));
            Assert.That(clone.OriginCardInstanceGuid.HasValue, Is.False);
            Assert.That(clone.BaseCardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(clone.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(clone.SelfFormState.HasValue, Is.False);
            Assert.That(clone.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Initialize }));
            Assert.That(clone.BuffManager.Buffs, Is.Empty);
        }

        private static CardInstance _CreateMatchingInstance(ICardEntity card)
        {
            return new CardInstance(
                card.OriginCardInstanceGuid.ValueOr(Guid.Empty),
                card.BaseCardDataId,
                Array.Empty<ICardPropertyData>());
        }

        private sealed record TestQueueItem(
            TriggerContext Context,
            int Id,
            bool enqueueImmediate = false) : EffectQueueItem(Context)
        {
            public override EffectResult Execute(IEffectQueueContext queue)
            {
                if (enqueueImmediate)
                {
                    queue.EnqueueImmediate(new TestQueueItem(Context, 2));
                }

                return new EffectResult(
                    Array.Empty<BaseResultAction>(),
                    new IGameEvent[] { new QueuedTestEvent(Id) });
            }
        }

        private sealed record QueuedTestEvent(int Id) : IGameEvent;
    }
}
