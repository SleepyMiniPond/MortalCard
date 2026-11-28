using System;
using MortalGame.Exceptions;
using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests.CardTransformation
{
    public sealed class CardInstanceChangeSetTests
    {
        private const string OverrideCardId = "change-set-override";

        [Test]
        public void Collect_PersistentSelfForm_SetsCurrentFormAndClearsOverride()
        {
            var built = _Build();
            var (instance, card) = _CreateCardFromInstance(built);
            built.Gameplay.Ally.CardManager.DisposeZone.AddCard(card);
            card.TryApplySelfForm(
                "persistent-form",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);
            card.TryApplyOverrideForm(
                "temporary-override",
                OverrideCardId,
                SystemSource.Instance,
                Array.Empty<CardFormOverrideReleaseRule>(),
                new Dictionary<string, IReactionSessionEntity>());

            var changes = CardInstanceChangeSetCollector.Collect(
                new[] { instance },
                built.Gameplay.Ally.CardManager);

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].InstanceGuid, Is.EqualTo(instance.InstanceGuid));
            Assert.That(changes[0].PersistentFormState.TryGetValue(out var form), Is.True);
            Assert.That(form.TransformKey, Is.EqualTo("persistent-form"));
            Assert.That(form.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(card.OverrideFormState.HasValue, Is.False);
            Assert.That(card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
        }

        [Test]
        public void Collect_BattleOnlySelfForm_ClearsPreviousPersistentForm()
        {
            var built = _Build();
            var previousForm = new PersistentCardFormState(
                "previous-persistent",
                CardTransformationTestBuilder.AlternateCardId);
            var (instance, card) = _CreateCardFromInstance(built, previousForm.Some());
            built.Gameplay.Ally.CardManager.HandCard.AddCard(card);
            card.TryApplySelfForm(
                "battle-only-form",
                CardTransformationTestBuilder.BaseCardId,
                CardFormPersistence.BattleOnly);

            var changes = CardInstanceChangeSetCollector.Collect(
                new[] { instance },
                built.Gameplay.Ally.CardManager);

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].PersistentFormState.HasValue, Is.False);
        }

        [Test]
        public void Collect_RuntimeCardAndClone_DoNotProduceAdditionalChanges()
        {
            var built = _Build();
            var (instance, card) = _CreateCardFromInstance(built);
            built.Gameplay.Ally.CardManager.Deck.AddCard(card);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            built.Gameplay.Ally.CardManager.Graveyard.AddCard(built.Card.Clone());

            var changes = CardInstanceChangeSetCollector.Collect(
                new[] { instance },
                built.Gameplay.Ally.CardManager);

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].InstanceGuid, Is.EqualTo(instance.InstanceGuid));
        }

        [Test]
        public void CollectForBattleResult_Lose_ReturnsEmptyWithoutMutatingCard()
        {
            var built = _Build();
            var (instance, card) = _CreateCardFromInstance(built);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(card);
            card.TryApplySelfForm(
                "persistent-form",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);
            card.TryApplyOverrideForm(
                "temporary-override",
                OverrideCardId,
                SystemSource.Instance,
                Array.Empty<CardFormOverrideReleaseRule>(),
                new Dictionary<string, IReactionSessionEntity>());

            var changes = CardInstanceChangeSetCollector.CollectForBattleResult(
                false,
                new[] { instance },
                built.Gameplay.Ally.CardManager);

            Assert.That(changes.Count, Is.EqualTo(0));
            Assert.That(card.OverrideFormState.HasValue, Is.True);
            Assert.That(card.CardDataId, Is.EqualTo(OverrideCardId));
        }

        [Test]
        public void Collect_MissingOriginalCardReportsIdentityWithoutChangingOtherCards()
        {
            var built = _Build();
            var (instance, card) = _CreateCardFromInstance(built);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(card);
            var missing = instance with { InstanceGuid = Guid.NewGuid() };

            var error = Assert.Throws<MissingBattleCardEntityException>(() =>
                CardInstanceChangeSetCollector.Collect(new[] { missing }, built.Gameplay.Ally.CardManager));

            Assert.That(error.InstanceGuid, Is.EqualTo(missing.InstanceGuid));
            Assert.That(card.OriginCardInstanceGuid.ValueOr(Guid.Empty), Is.EqualTo(instance.InstanceGuid));
            Assert.That(card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        [Test]
        public void Collect_InconsistentOverrideLayerPreservesStructuredRejectionAndStopsOutput()
        {
            var built = _Build();
            var (instance, card) = _CreateCardFromInstance(built);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(card);
            card.TryApplyOverrideForm("override", OverrideCardId, SystemSource.Instance,
                Array.Empty<CardFormOverrideReleaseRule>(), new Dictionary<string, IReactionSessionEntity>());
            Assert.That(card.OverrideFormState.TryGetValue(out var state), Is.True);
            Assert.That(card.BuffManager.TryRemoveOverrideLayer(state.BuffLayerHandle), Is.True);

            var error = Assert.Throws<CardOverrideRemovalFailedException>(() =>
                CardInstanceChangeSetCollector.Collect(new[] { instance }, built.Gameplay.Ally.CardManager));

            Assert.That(error.CardIdentity, Is.EqualTo(card.Identity));
            Assert.That(error.Rejection, Is.TypeOf<CardFormRejection.OverrideLayerMismatch>());
            Assert.That(((CardFormRejection.OverrideLayerMismatch)error.Rejection).StateIdentity, Is.EqualTo(state.Identity));
            Assert.That(error.Message, Does.Contain(state.Identity.ToString()));
            Assert.That(card.OverrideFormState.TryGetValue(out var unchanged), Is.True);
            Assert.That(unchanged, Is.SameAs(state));
            Assert.That(card.CardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(instance.PersistentFormState.HasValue, Is.False);
        }

        private static BuiltCardTransformationTest _Build()
        {
            return new CardTransformationTestBuilder()
                .WithCard(new OverrideCardData
                {
                    ID = OverrideCardId,
                    Cost = 7,
                    Power = 11
                })
                .Build();
        }

        private static (CardInstance Instance, ICardEntity Card) _CreateCardFromInstance(
            BuiltCardTransformationTest built,
            Option<PersistentCardFormState> persistentFormState = default)
        {
            var instance = new CardInstance(
                Guid.NewGuid(),
                CardTransformationTestBuilder.BaseCardId,
                Array.Empty<ICardPropertyData>(),
                persistentFormState);
            var card = CardEntity.CreateFromInstance(
                instance,
                built.Gameplay.ContextManager.CardLibrary,
                built.Gameplay.ContextManager.CardPropertyEntityFactory);
            return (instance, card);
        }
    }
}
