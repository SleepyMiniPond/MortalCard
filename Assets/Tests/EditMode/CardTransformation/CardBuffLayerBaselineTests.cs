using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests.T010
{
    /// <summary>
    /// 鎖定 CardBuff 分層導入前的單層行為，作為後續 Facade 重構的回歸基準。
    /// </summary>
    public class CardBuffLayerBaselineTests
    {
        [Test]
        public void SingleLayer_AddModifyRemove_ReturnsExpectedResultsAndCollectionState()
        {
            var built = new CardTransformationTestBuilder().Build();
            var buff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);

            var addResult = built.Card.BuffManager.AddBuff(buff);
            var modifyResult = built.Card.BuffManager.ModifyBuffLevel(buff.CardBuffDataID, 2);
            var removeResult = built.Card.BuffManager.RemoveBuff(buff);

            Assert.That(addResult.CardBuff, Is.SameAs(buff));
            Assert.That(modifyResult.CardBuff, Is.SameAs(buff));
            Assert.That(modifyResult.DeltaLevel, Is.EqualTo(2));
            Assert.That(modifyResult.NewLevel, Is.EqualTo(3));
            Assert.That(removeResult.CardBuff, Is.SameAs(buff));
            Assert.That(built.Card.BuffManager.Buffs, Is.Empty);
        }

        [Test]
        public void SingleLayer_AddDuplicateDataId_ThrowsAndKeepsOriginalBuff()
        {
            var built = new CardTransformationTestBuilder().Build();
            var originalBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var duplicateBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            built.Card.BuffManager.AddBuff(originalBuff);

            var exception = Assert.Throws<Exception>(
                () => built.Card.BuffManager.AddBuff(duplicateBuff));

            Assert.That(exception.Message, Does.Contain(CardTransformationTestBuilder.CardBuffId));
            Assert.That(built.Card.BuffManager.Buffs, Has.Count.EqualTo(1));
            Assert.That(built.Card.BuffManager.Buffs.Single(), Is.SameAs(originalBuff));
        }

        [Test]
        public void SingleLayer_Update_UpdatesSessionAndLifeTimeThenRemovesExpiredBuff()
        {
            const string expiringBuffId = "t010-expiring-layer-baseline-buff";
            var buffData = _CreateExpiringBuffData(expiringBuffId);
            var built = new CardTransformationTestBuilder()
                .WithCardBuff(buffData)
                .Build();
            var buff = _CreateBuff(built, expiringBuffId);
            var session = buff.ReactionSessions["whole-turn"];
            built.Card.BuffManager.AddBuff(buff);
            var updateContext = built.Context with
            {
                Action = new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance)
            };

            var isUpdated = built.Card.BuffManager.Update(updateContext, built.Card);

            Assert.That(isUpdated, Is.True);
            Assert.That(session.BooleanValue.HasValue, Is.False);
            Assert.That(buff.IsExpired(), Is.True);
            Assert.That(built.Card.BuffManager.Buffs, Is.Empty);
        }

        [Test]
        public void SelfTransform_PreservesBuffManagerInstanceAndExistingBuffIdentity()
        {
            var built = new CardTransformationTestBuilder().Build();
            var managerBeforeTransform = built.Card.BuffManager;
            var buff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            managerBeforeTransform.AddBuff(buff);

            var result = built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.BattleOnly);

            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(built.Card.BuffManager, Is.SameAs(managerBeforeTransform));
            Assert.That(
                built.Card.BuffManager.ActiveLayerHandle,
                Is.SameAs(managerBeforeTransform.ActiveLayerHandle));
            Assert.That(built.Card.BuffManager.Buffs, Has.Count.EqualTo(1));
            Assert.That(built.Card.BuffManager.Buffs.Single(), Is.SameAs(buff));
            Assert.That(built.Card.BuffManager.Buffs.Single().Identity, Is.EqualTo(buff.Identity));
        }

        [Test]
        public void CardEntity_AllCreationPaths_ExposeCardBuffLayerManagerFacade()
        {
            var runtimeCard = new CardTransformationTestBuilder().Build().Card;
            var instanceCard = new CardTransformationTestBuilder().BuildFromInstance().Card;
            var clonedCard = runtimeCard.Clone();

            Assert.That(runtimeCard.BuffManager, Is.TypeOf<CardBuffLayerManager>());
            Assert.That(instanceCard.BuffManager, Is.TypeOf<CardBuffLayerManager>());
            Assert.That(clonedCard.BuffManager, Is.TypeOf<CardBuffLayerManager>());
            Assert.That(CardEntity.DummyCard.BuffManager, Is.TypeOf<CardBuffLayerManager>());
        }

        [Test]
        public void BaseFacade_ActiveLayerHandle_IsStableAndCannotBeConstructedExternally()
        {
            ICardBuffManager facade = new CardBuffLayerManager(Array.Empty<ICardBuffEntity>());

            var firstHandle = facade.ActiveLayerHandle;
            var secondHandle = facade.ActiveLayerHandle;

            Assert.That(firstHandle, Is.SameAs(secondHandle));
            Assert.That(firstHandle.ToString(), Is.Not.Empty);
            Assert.That(typeof(CardBuffLayerHandle).GetConstructors(), Is.Empty);
        }

        [Test]
        public void BaseFacade_SeparateManagersHaveDifferentLayerHandles()
        {
            ICardBuffManager firstFacade = new CardBuffLayerManager(Array.Empty<ICardBuffEntity>());
            ICardBuffManager secondFacade = new CardBuffLayerManager(Array.Empty<ICardBuffEntity>());

            Assert.That(firstFacade.ActiveLayerHandle, Is.Not.SameAs(secondFacade.ActiveLayerHandle));
        }

        [Test]
        public void CardBuffLayer_DoesNotImplementFacadeContract()
        {
            var singleLayer = new CardBuffLayer(Array.Empty<ICardBuffEntity>());

            Assert.That(singleLayer, Is.Not.InstanceOf<ICardBuffManager>());
        }

        [Test]
        public void BaseFacade_LayerScopedMutations_WithActiveHandleReturnResults()
        {
            var built = new CardTransformationTestBuilder().Build();
            var buff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            ICardBuffManager facade = new CardBuffLayerManager(Array.Empty<ICardBuffEntity>());
            var handle = facade.ActiveLayerHandle;

            var addResult = facade.TryAddBuff(handle, buff);
            var modifyResult = facade.TryModifyBuffLevel(handle, buff.CardBuffDataID, 2);
            var removeResult = facade.TryRemoveBuff(handle, buff);

            Assert.That(addResult.HasValue, Is.True);
            Assert.That(addResult.ValueOr((AddCardBuffResult)null).CardBuff, Is.SameAs(buff));
            Assert.That(modifyResult.HasValue, Is.True);
            Assert.That(
                modifyResult.ValueOr((ModifyCardBuffLevelResult)null).NewLevel,
                Is.EqualTo(3));
            Assert.That(removeResult.HasValue, Is.True);
            Assert.That(removeResult.ValueOr((RemoveCardBuffResult)null).CardBuff, Is.SameAs(buff));
            Assert.That(facade.Buffs, Is.Empty);
        }

        [Test]
        public void BaseFacade_LayerScopedMutations_WithForeignHandleReturnNoneWithoutMutation()
        {
            var built = new CardTransformationTestBuilder().Build();
            var originalBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var duplicateBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            ICardBuffManager facade = new CardBuffLayerManager(new[] { originalBuff });
            var foreignHandle = new CardBuffLayerManager(Array.Empty<ICardBuffEntity>())
                .ActiveLayerHandle;

            var addResult = facade.TryAddBuff(foreignHandle, duplicateBuff);
            var modifyResult = facade.TryModifyBuffLevel(
                foreignHandle,
                originalBuff.CardBuffDataID,
                2);
            var removeResult = facade.TryRemoveBuff(foreignHandle, originalBuff);

            Assert.That(addResult.HasValue, Is.False);
            Assert.That(modifyResult.HasValue, Is.False);
            Assert.That(removeResult.HasValue, Is.False);
            Assert.That(originalBuff.Level, Is.EqualTo(1));
            Assert.That(facade.Buffs.Single(), Is.SameAs(originalBuff));
        }

        [Test]
        public void BaseFacade_LayerScopedMutation_WithActiveHandlePreservesDataErrors()
        {
            var built = new CardTransformationTestBuilder().Build();
            var originalBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var duplicateBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            ICardBuffManager facade = new CardBuffLayerManager(new[] { originalBuff });

            var exception = Assert.Throws<Exception>(
                () => facade.TryAddBuff(facade.ActiveLayerHandle, duplicateBuff));

            Assert.That(exception.Message, Does.Contain(CardTransformationTestBuilder.CardBuffId));
            Assert.That(facade.Buffs.Single(), Is.SameAs(originalBuff));
        }

        [Test]
        public void BaseFacade_ForwardsBuffCollectionAndMutationOperationsToBaseLayer()
        {
            var built = new CardTransformationTestBuilder().Build();
            var buff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            ICardBuffManager facade = new CardBuffLayerManager(Array.Empty<ICardBuffEntity>());

            var addResult = facade.AddBuff(buff);
            var modifyResult = facade.ModifyBuffLevel(buff.CardBuffDataID, 2);

            Assert.That(addResult.CardBuff, Is.SameAs(buff));
            Assert.That(modifyResult.CardBuff, Is.SameAs(buff));
            Assert.That(modifyResult.NewLevel, Is.EqualTo(3));
            Assert.That(facade.Buffs.Single(), Is.SameAs(buff));

            var removeResult = facade.RemoveBuff(buff);

            Assert.That(removeResult.CardBuff, Is.SameAs(buff));
            Assert.That(facade.Buffs, Is.Empty);
        }

        [Test]
        public void BaseFacade_Update_ForwardsSessionAndLifeTimeUpdateToBaseLayer()
        {
            const string expiringBuffId = "t010-expiring-base-facade-buff";
            var buffData = _CreateExpiringBuffData(expiringBuffId);
            var built = new CardTransformationTestBuilder()
                .WithCardBuff(buffData)
                .Build();
            var buff = _CreateBuff(built, expiringBuffId);
            var session = buff.ReactionSessions["whole-turn"];
            ICardBuffManager facade = new CardBuffLayerManager(new[] { buff });
            var updateContext = built.Context with
            {
                Action = new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance)
            };

            var isUpdated = facade.Update(updateContext, built.Card);

            Assert.That(isUpdated, Is.True);
            Assert.That(session.BooleanValue.HasValue, Is.False);
            Assert.That(buff.IsExpired(), Is.True);
            Assert.That(facade.Buffs, Is.Empty);
        }

        [Test]
        public void OverrideLayer_ReplaceActivatesEmptyLayerAndRemoveRestoresBaseLayer()
        {
            var built = new CardTransformationTestBuilder().Build();
            var baseBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var manager = built.Card.BuffManager;
            manager.AddBuff(baseBuff);
            var baseHandle = manager.ActiveLayerHandle;

            var overrideHandle = manager.ReplaceOverrideLayer();

            Assert.That(overrideHandle, Is.Not.SameAs(baseHandle));
            Assert.That(manager.ActiveLayerHandle, Is.SameAs(overrideHandle));
            Assert.That(manager.Buffs, Is.Empty);

            var isRemoved = manager.TryRemoveOverrideLayer(overrideHandle);

            Assert.That(isRemoved, Is.True);
            Assert.That(manager.ActiveLayerHandle, Is.SameAs(baseHandle));
            Assert.That(manager.Buffs.Single(), Is.SameAs(baseBuff));
        }

        [Test]
        public void OverrideLayer_BaseLayerRemainsFrozenUntilRemove()
        {
            const string expiringBuffId = "t010-frozen-base-layer-buff";
            var buffData = _CreateExpiringBuffData(expiringBuffId);
            var built = new CardTransformationTestBuilder()
                .WithCardBuff(buffData)
                .Build();
            var baseBuff = _CreateBuff(built, expiringBuffId);
            var session = baseBuff.ReactionSessions["whole-turn"];
            built.Card.BuffManager.AddBuff(baseBuff);
            var overrideHandle = built.Card.BuffManager.ReplaceOverrideLayer();
            var updateContext = built.Context with
            {
                Action = new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance)
            };

            var isOverrideUpdated = built.Card.BuffManager.Update(updateContext, built.Card);

            Assert.That(isOverrideUpdated, Is.False);
            Assert.That(session.BooleanValue.ValueOr(false), Is.True);
            Assert.That(baseBuff.IsExpired(), Is.False);

            built.Card.BuffManager.TryRemoveOverrideLayer(overrideHandle);
            var isBaseUpdated = built.Card.BuffManager.Update(updateContext, built.Card);

            Assert.That(isBaseUpdated, Is.True);
            Assert.That(session.BooleanValue.HasValue, Is.False);
            Assert.That(baseBuff.IsExpired(), Is.True);
            Assert.That(built.Card.BuffManager.Buffs, Is.Empty);
        }

        [Test]
        public void OverrideLayer_AllowsSameBuffIdAndDiscardsOverrideBuffOnRemove()
        {
            var built = new CardTransformationTestBuilder().Build();
            var baseBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var overrideBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            built.Card.BuffManager.AddBuff(baseBuff);
            var overrideHandle = built.Card.BuffManager.ReplaceOverrideLayer();

            built.Card.BuffManager.AddBuff(overrideBuff);
            built.Card.BuffManager.ModifyBuffLevel(overrideBuff.CardBuffDataID, 2);

            Assert.That(built.Card.BuffManager.Buffs.Single(), Is.SameAs(overrideBuff));
            Assert.That(overrideBuff.Level, Is.EqualTo(3));
            Assert.That(baseBuff.Level, Is.EqualTo(1));

            var isRemoved = built.Card.BuffManager.TryRemoveOverrideLayer(overrideHandle);

            Assert.That(isRemoved, Is.True);
            Assert.That(built.Card.BuffManager.Buffs.Single(), Is.SameAs(baseBuff));
            Assert.That(baseBuff.Level, Is.EqualTo(1));
        }

        [Test]
        public void OverrideLayer_SecondReplacementDiscardsFirstAndOldHandleCannotRemoveCurrent()
        {
            ICardBuffManager manager = new CardBuffLayerManager(Array.Empty<ICardBuffEntity>());
            var built = new CardTransformationTestBuilder().Build();
            var firstHandle = manager.ReplaceOverrideLayer();
            var firstOverrideBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            manager.AddBuff(firstOverrideBuff);

            var secondHandle = manager.ReplaceOverrideLayer();
            var isOldLayerRemoved = manager.TryRemoveOverrideLayer(firstHandle);

            Assert.That(secondHandle, Is.Not.SameAs(firstHandle));
            Assert.That(manager.ActiveLayerHandle, Is.SameAs(secondHandle));
            Assert.That(manager.Buffs, Is.Empty);
            Assert.That(isOldLayerRemoved, Is.False);
            Assert.That(manager.ActiveLayerHandle, Is.SameAs(secondHandle));
        }

        [Test]
        public void CardBuffCommands_CarryCapturedLayerHandle()
        {
            var built = new CardTransformationTestBuilder().Build();
            var buff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var layerHandle = built.Card.BuffManager.ActiveLayerHandle;

            var addCommand = new AddCardBuffEffectCommand(built.Card, layerHandle, buff);
            var removeCommand = new RemoveCardBuffEffectCommand(built.Card, layerHandle, buff);
            var modifyCommand = new ModifyCardBuffLevelEffectCommand(
                built.Card,
                layerHandle,
                buff.CardBuffDataID,
                1);

            Assert.That(addCommand.LayerHandle, Is.SameAs(layerHandle));
            Assert.That(removeCommand.LayerHandle, Is.SameAs(layerHandle));
            Assert.That(modifyCommand.LayerHandle, Is.SameAs(layerHandle));
        }

        [Test]
        public void CardBuffCommandHandler_WithReplacedLayerHandle_ReturnsNoOpWithoutMutation()
        {
            var built = new CardTransformationTestBuilder().Build();
            var overrideBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var newBuff = _CreateBuff(built, CardTransformationTestBuilder.CardBuffId);
            var replacedHandle = built.Card.BuffManager.ReplaceOverrideLayer();
            built.Card.BuffManager.AddBuff(overrideBuff);
            built.Card.BuffManager.ReplaceOverrideLayer();
            var handler = new CardBuffEffectCommandHandler();

            var addResult = handler.Handle(
                built.Context,
                new AddCardBuffEffectCommand(built.Card, replacedHandle, newBuff));
            var modifyResult = handler.Handle(
                built.Context,
                new ModifyCardBuffLevelEffectCommand(
                    built.Card,
                    replacedHandle,
                    overrideBuff.CardBuffDataID,
                    2));
            var removeResult = handler.Handle(
                built.Context,
                new RemoveCardBuffEffectCommand(built.Card, replacedHandle, overrideBuff));

            Assert.That(addResult.Actions, Is.Empty);
            Assert.That(addResult.Events, Is.Empty);
            Assert.That(modifyResult.Actions, Is.Empty);
            Assert.That(modifyResult.Events, Is.Empty);
            Assert.That(removeResult.Actions, Is.Empty);
            Assert.That(removeResult.Events, Is.Empty);
            Assert.That(overrideBuff.Level, Is.EqualTo(1));
            Assert.That(built.Card.BuffManager.Buffs, Is.Empty);
        }

        private static ICardBuffEntity _CreateBuff(
            BuiltCardTransformationTest built,
            string buffId)
        {
            return BuffTestBuilder.CreateCardBuff(
                built.Context,
                built.Gameplay.ContextManager.CardBuffLibrary,
                buffId);
        }

        private static CardBuffData _CreateExpiringBuffData(string buffId)
        {
            return new CardBuffData
            {
                ID = buffId,
                Sessions = new Dictionary<string, IReactionSessionData>
                {
                    ["whole-turn"] = new SessionBoolean
                    {
                        InitialValue = true,
                        LifeTime = SessionLifeTime.WholeTurn
                    }
                },
                LifeTimeData = new TurnLifeTimeCardBuffData
                {
                    Turn = new ConstInteger { Value = 1 }
                }
            };
        }
    }
}
