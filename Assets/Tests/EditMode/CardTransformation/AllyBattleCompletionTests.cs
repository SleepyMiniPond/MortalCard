using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests.CardTransformation
{
    public sealed class AllyBattleCompletionTests
    {
        [Test]
        public void Apply_UpdatesResourcesAndFormsInOriginalDeckOrderWithoutMutatingSource()
        {
            var source = _CreateAlly();
            var originalCards = source.Deck.ToArray();
            var newForm = new PersistentCardFormState("new-form", "new-target");
            var completion = new AllyBattleCompletion(source.Identity, 17, 6, new[]
            {
                new CardInstanceChangeSet(source.Deck[1].InstanceGuid, default),
                new CardInstanceChangeSet(source.Deck[0].InstanceGuid, newForm.Some())
            });

            var updated = source.ApplyBattleCompletion(completion);

            Assert.That(updated, Is.Not.SameAs(source));
            Assert.That(updated.Identity, Is.EqualTo(source.Identity));
            Assert.That(updated.NameKey, Is.EqualTo(source.NameKey));
            Assert.That(updated.MaxHealth, Is.EqualTo(source.MaxHealth));
            Assert.That(updated.MaxEnergy, Is.EqualTo(source.MaxEnergy));
            Assert.That(updated.HandCardMaxCount, Is.EqualTo(source.HandCardMaxCount));
            Assert.That(updated.CurrentHealth, Is.EqualTo(17));
            Assert.That(updated.CurrentDisposition, Is.EqualTo(6));
            Assert.That(updated.CurrentEnergy, Is.EqualTo(source.MaxEnergy));
            Assert.That(updated.Deck, Is.Not.SameAs(source.Deck));
            Assert.That(updated.Deck.Select(card => card.InstanceGuid),
                Is.EqualTo(source.Deck.Select(card => card.InstanceGuid)));
            Assert.That(updated.Deck[0].PersistentFormState, Is.EqualTo(newForm.Some()));
            Assert.That(updated.Deck[1].PersistentFormState.HasValue, Is.False, "None 必須清除舊持久形態");
            for (var i = 0; i < originalCards.Length; i++)
            {
                Assert.That(updated.Deck[i].CardDataId, Is.EqualTo(originalCards[i].CardDataId));
                Assert.That(updated.Deck[i].AdditionPropertyDatas, Is.SameAs(originalCards[i].AdditionPropertyDatas));
                Assert.That(source.Deck[i], Is.SameAs(originalCards[i]));
            }
            Assert.That(source.CurrentHealth, Is.EqualTo(30));
            Assert.That(source.CurrentDisposition, Is.EqualTo(3));
            Assert.That(source.CurrentEnergy, Is.EqualTo(1));
            Assert.That(source.Deck[1].PersistentFormState.HasValue, Is.True);
            updated.Deck.Clear();
            Assert.That(source.Deck, Has.Count.EqualTo(2), "新牌組集合不得與原牌組共用");
        }

        [Test]
        public void Apply_UnchangedFormsRemainIntact()
        {
            var source = _CreateAlly();
            var updated = source.ApplyBattleCompletion(_Completion(source));
            Assert.That(updated.Deck, Is.EqualTo(source.Deck));
            Assert.That(updated.Deck, Is.Not.SameAs(source.Deck));
        }

        [TestCase("persistent", true)]
        [TestCase("battle-only", false)]
        [TestCase("reverted", false)]
        [TestCase("no-self", false)]
        public void Apply_CollectedFormsPreserveExistingPersistenceBoundaries(string formCase, bool retainsForm)
        {
            var built = new CardTransformationTestBuilder().WithCard(new OverrideCardData
            {
                ID = "completion-override", Cost = 9, Power = 9
            }).WithCard(CardTransformationTestBuilder.CreateCardData("completion-target", 2, 3)).Build();
            var instance = new CardInstance(Guid.NewGuid(), CardTransformationTestBuilder.BaseCardId,
                Array.Empty<ICardPropertyData>(),
                new PersistentCardFormState("old", CardTransformationTestBuilder.AlternateCardId).Some());
            var source = _CreateAlly() with { Deck = new List<CardInstance> { instance } };
            var card = CardEntity.CreateFromInstance(instance, built.Gameplay.ContextManager.CardLibrary,
                built.Gameplay.ContextManager.CardPropertyEntityFactory);
            built.Gameplay.Ally.CardManager.DisposeZone.AddCard(card);
            if (formCase == "no-self")
                card.TryRevertSelfForm("old");
            else
            {
                card.TryApplySelfForm("current", "completion-target",
                    formCase == "battle-only" ? CardFormPersistence.BattleOnly : CardFormPersistence.Persistent);
                if (formCase == "reverted") card.TryRevertSelfForm("current");
            }
            card.TryApplyOverrideForm("override", "completion-override", SystemSource.Instance,
                Array.Empty<CardFormOverrideReleaseRule>(), new Dictionary<string, IReactionSessionEntity>());
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            built.Gameplay.Ally.CardManager.Graveyard.AddCard(card.Clone());

            var changes = CardInstanceChangeSetCollector.Collect(source.Deck, built.Gameplay.Ally.CardManager);
            var updated = source.ApplyBattleCompletion(new AllyBattleCompletion(source.Identity, 17, 6, changes));

            Assert.That(updated.Deck, Has.Count.EqualTo(1));
            Assert.That(updated.Deck[0].InstanceGuid, Is.EqualTo(instance.InstanceGuid));
            Assert.That(updated.Deck[0].CardDataId, Is.EqualTo(instance.CardDataId));
            Assert.That(updated.Deck[0].PersistentFormState.HasValue, Is.EqualTo(retainsForm));
            if (retainsForm)
                Assert.That(updated.Deck[0].PersistentFormState,
                    Is.EqualTo(new PersistentCardFormState("current", "completion-target").Some()));
            Assert.That(source.Deck[0].PersistentFormState, Is.EqualTo(instance.PersistentFormState));
        }

        [Test]
        public void Apply_EmptyDeckAcceptsEmptyChanges()
        {
            var source = _CreateAlly() with { Deck = new List<CardInstance>() };
            var updated = source.ApplyBattleCompletion(_Completion(source));
            Assert.That(updated.Deck, Is.Empty);
            Assert.That(updated.Deck, Is.Not.SameAs(source.Deck));
            Assert.That(updated.CurrentEnergy, Is.EqualTo(source.MaxEnergy));
        }

        [TestCase("player")]
        [TestCase("missing")]
        [TestCase("extra")]
        [TestCase("unknown")]
        [TestCase("duplicate")]
        [TestCase("source-duplicate")]
        public void Apply_InvalidIdentitySetThrowsWithoutChangingSource(string invalidCase)
        {
            var source = _CreateAlly();
            var completion = _Completion(source);
            var changes = completion.CardInstanceChanges.ToList();
            switch (invalidCase)
            {
                case "player":
                    completion = completion with { OriginAllyInstanceGuid = Guid.NewGuid() };
                    break;
                case "missing": changes.RemoveAt(1); break;
                case "extra": changes.Add(new CardInstanceChangeSet(Guid.NewGuid(), default)); break;
                case "unknown": changes[1] = new CardInstanceChangeSet(Guid.NewGuid(), default); break;
                case "duplicate": changes[1] = changes[0]; break;
                case "source-duplicate": source.Deck[1] = source.Deck[0]; break;
            }
            completion = completion with { CardInstanceChanges = changes };
            var originalDeck = source.Deck;
            var originalCards = source.Deck.ToArray();

            Assert.Throws<InvalidOperationException>(() => source.ApplyBattleCompletion(completion));

            Assert.That(source.Deck, Is.SameAs(originalDeck));
            Assert.That(source.Deck, Is.EqualTo(originalCards));
            Assert.That(source.CurrentHealth, Is.EqualTo(30));
            Assert.That(source.CurrentDisposition, Is.EqualTo(3));
            Assert.That(source.CurrentEnergy, Is.EqualTo(1));
        }

        private static AllyBattleCompletion _Completion(AllyInstance source)
            => new(source.Identity, 17, 6, source.Deck.Select(card =>
                new CardInstanceChangeSet(card.InstanceGuid, card.PersistentFormState)).ToArray());

        private static AllyInstance _CreateAlly()
            => new(Guid.NewGuid(), "ally", 3, 30, 40, 1, 5,
                new List<CardInstance>
                {
                    new(Guid.NewGuid(), "base-a", new ICardPropertyData[] { new InitialPriorityPropertyData() }),
                    new(Guid.NewGuid(), "base-b", Array.Empty<ICardPropertyData>(),
                        new PersistentCardFormState("old-form", "old-target").Some())
                }, 7);
    }
}
