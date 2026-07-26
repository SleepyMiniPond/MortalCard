using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests.T010
{
    public class CardTransformationCharacterizationTests
    {
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
        public void CardBuffManager_AddSameDataId_RejectsDuplicateBuff()
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
