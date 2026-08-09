using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests.T010
{
    /// <summary>
    /// 驗證 External Override 單一 Slot 的狀態、取代與安全移除語意。
    /// </summary>
    public class CardFormOverrideStateTests
    {
        private const string FirstOverrideId = "t010-first-override";
        private const string SecondOverrideId = "t010-second-override";

        [Test]
        public void ApplyOverride_CreatesStateAndActivatesMatchingBuffLayer()
        {
            var built = _BuildWithOverrides();

            var result = _Apply(built, "first", FirstOverrideId);

            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(result.BeforeCardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(result.AfterCardDataId, Is.EqualTo(FirstOverrideId));
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var state), Is.True);
            Assert.That(state.Identity, Is.Not.EqualTo(Guid.Empty));
            Assert.That(state.OverrideKey, Is.EqualTo("first"));
            Assert.That(state.CardDataId, Is.EqualTo(FirstOverrideId));
            Assert.That(state.Source, Is.SameAs(SystemSource.Instance));
            Assert.That(state.BuffLayerHandle, Is.SameAs(built.Card.BuffManager.ActiveLayerHandle));
            Assert.That(built.Card.BuffManager.Buffs, Is.Empty);
        }

        [Test]
        public void ApplyOverride_WithSameKeyAndCardData_IsNoOpAndPreservesState()
        {
            var built = _BuildWithOverrides();
            _Apply(built, "first", FirstOverrideId);
            built.Card.OverrideFormState.TryGetValue(out var originalState);

            var result = _Apply(built, "first", FirstOverrideId);

            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.NoOp));
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var currentState), Is.True);
            Assert.That(currentState, Is.SameAs(originalState));
            Assert.That(currentState.Identity, Is.EqualTo(originalState.Identity));
            Assert.That(currentState.BuffLayerHandle, Is.SameAs(originalState.BuffLayerHandle));
        }

        [Test]
        public void ApplyOverride_WithDifferentOverride_ReplacesOldStateAndLayer()
        {
            var built = _BuildWithOverrides();
            _Apply(built, "first", FirstOverrideId);
            built.Card.OverrideFormState.TryGetValue(out var firstState);
            var oldLayerBuff = BuffTestBuilder.CreateCardBuff(
                built.Context,
                built.Gameplay.ContextManager.CardBuffLibrary,
                CardTransformationTestBuilder.CardBuffId);
            built.Card.BuffManager.AddBuff(oldLayerBuff);

            var replaceResult = _Apply(built, "second", SecondOverrideId);

            Assert.That(replaceResult.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(replaceResult.BeforeCardDataId, Is.EqualTo(FirstOverrideId));
            Assert.That(replaceResult.AfterCardDataId, Is.EqualTo(SecondOverrideId));
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var secondState), Is.True);
            Assert.That(secondState.Identity, Is.Not.EqualTo(firstState.Identity));
            Assert.That(secondState.BuffLayerHandle, Is.Not.SameAs(firstState.BuffLayerHandle));
            Assert.That(built.Card.BuffManager.Buffs, Is.Empty);

            var staleRemoveResult = built.Card.TryRemoveOverrideForm(firstState.Identity);

            Assert.That(staleRemoveResult.Status, Is.EqualTo(CardFormOperationStatus.NoOp));
            Assert.That(built.Card.CardDataId, Is.EqualTo(SecondOverrideId));
            Assert.That(
                built.Card.OverrideFormState.ValueOr((CardFormOverrideState)null),
                Is.SameAs(secondState));
        }

        [Test]
        public void RemoveCurrentOverride_AfterReplacement_ReturnsToSelfFormNotOldOverride()
        {
            var built = _BuildWithOverrides();
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.BattleOnly);
            _Apply(built, "first", FirstOverrideId);
            _Apply(built, "second", SecondOverrideId);
            built.Card.OverrideFormState.TryGetValue(out var secondState);

            var removeResult = built.Card.TryRemoveOverrideForm(secondState.Identity);

            Assert.That(removeResult.Status, Is.EqualTo(CardFormOperationStatus.Reverted));
            Assert.That(removeResult.BeforeCardDataId, Is.EqualTo(SecondOverrideId));
            Assert.That(removeResult.AfterCardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(built.Card.OverrideFormState.HasValue, Is.False);
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
        }

        [Test]
        public void ApplyOverride_WithDifferentKeyAndSameCardData_ReplacesStateWithoutChangingEffectiveId()
        {
            var built = _BuildWithOverrides();
            _Apply(built, "first", FirstOverrideId);
            built.Card.OverrideFormState.TryGetValue(out var firstState);

            var result = _Apply(built, "second", FirstOverrideId);

            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(result.BeforeCardDataId, Is.EqualTo(FirstOverrideId));
            Assert.That(result.AfterCardDataId, Is.EqualTo(FirstOverrideId));
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var secondState), Is.True);
            Assert.That(secondState.Identity, Is.Not.EqualTo(firstState.Identity));
            Assert.That(secondState.BuffLayerHandle, Is.Not.SameAs(firstState.BuffLayerHandle));
        }

        [Test]
        public void OverrideForm_SuppressesInstancePropertiesAndRemoveRestoresUnderlyingProperties()
        {
            var overrideData = _CreateOverride(FirstOverrideId, cost: 7, power: 11);
            overrideData.PropertyDatas.Add(new SealedPropertyData());
            var built = new CardTransformationTestBuilder()
                .WithCard(overrideData)
                .BuildFromInstance(new RecyclePropertyData());

            _Apply(built, "first", FirstOverrideId);

            Assert.That(
                built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Sealed }));
            built.Card.OverrideFormState.TryGetValue(out var state);

            built.Card.TryRemoveOverrideForm(state.Identity);

            Assert.That(
                built.Card.Properties.Select(property => property.Property),
                Is.EquivalentTo(new[] { CardProperty.Preserved, CardProperty.Recycle }));
        }

        private static BuiltCardTransformationTest _BuildWithOverrides()
        {
            return new CardTransformationTestBuilder()
                .WithCard(_CreateOverride(FirstOverrideId, cost: 7, power: 11))
                .WithCard(_CreateOverride(SecondOverrideId, cost: 9, power: 13))
                .Build();
        }

        private static CardFormOperationResult _Apply(
            BuiltCardTransformationTest built,
            string overrideKey,
            string cardDataId)
        {
            return built.Card.TryApplyOverrideForm(
                overrideKey,
                cardDataId,
                SystemSource.Instance,
                Array.Empty<CardFormOverrideReleaseRule>(),
                new Dictionary<string, IReactionSessionEntity>());
        }

        private static OverrideCardData _CreateOverride(string cardDataId, int cost, int power)
        {
            return new OverrideCardData
            {
                ID = cardDataId,
                Cost = cost,
                Power = power
            };
        }
    }
}
