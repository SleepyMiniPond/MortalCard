using System.Collections.Generic;
using MortalGame.GameModel;
using System.Linq;
using NUnit.Framework;
using MortalGame.GameData;

namespace MortalGame.Tests
{

    public class GameRandomTests
    {
        [Test]
        public void ShuffleInPlace_WithSameSeed_ProducesSameOrder()
        {
            var first = Enumerable.Range(1, 10).ToList();
            var second = Enumerable.Range(1, 10).ToList();
            var firstRandom = new GameRandom(seed: 12345);
            var secondRandom = new GameRandom(seed: 12345);

            firstRandom.ShuffleInPlace(first);
            secondRandom.ShuffleInPlace(second);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void ShuffleInPlace_WithDifferentSeed_CanProduceDifferentOrder()
        {
            var first = Enumerable.Range(1, 10).ToList();
            var second = Enumerable.Range(1, 10).ToList();
            var firstRandom = new GameRandom(seed: 12345);
            var secondRandom = new GameRandom(seed: 67890);

            firstRandom.ShuffleInPlace(first);
            secondRandom.ShuffleInPlace(second);

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void EnumerableShuffle_WithSameSeed_ProducesSameOrder()
        {
            var firstRandom = new GameRandom(seed: 12345);
            var secondRandom = new GameRandom(seed: 12345);

            var first = Enumerable.Range(1, 10).Shuffle(firstRandom).ToArray();
            var second = Enumerable.Range(1, 10).Shuffle(secondRandom).ToArray();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void DeckShuffle_WithSameSeed_ProducesSameCardOrder()
        {
            var firstDeck = new DeckEntity(new GameRandom(seed: 12345));
            var secondDeck = new DeckEntity(new GameRandom(seed: 12345));
            var cards = CreateCards();

            firstDeck.EnqueueCardsThenShuffle(cards);
            secondDeck.EnqueueCardsThenShuffle(cards);

            Assert.That(
                firstDeck.Cards.Select(card => card.CardDataId).ToArray(),
                Is.EqualTo(secondDeck.Cards.Select(card => card.CardDataId).ToArray()));
        }

        [Test]
        public void GameplayManager_DoesNotReplaceContextRandom()
        {
            var contextManager = GameContextTestBuilder.CreateContextManager(randomSeed: 1);
            var stageSetting = new GameStageSetting(
                StageID: "test-stage",
                RandomSeed: 12345,
                Ally: null,
                Enemy: null);
            var expectedRandom = new GameRandom(seed: 1);

            _ = new GameplayManager(stageSetting, contextManager);

            Assert.That(contextManager.GameRandom.Range(0, 100), Is.EqualTo(expectedRandom.Range(0, 100)));
        }

        private static IReadOnlyList<ICardEntity> CreateCards()
        {
            var cardDatas = Enumerable.Range(1, 10)
                .Select(index => CardTestBuilder.CreateCardData($"test-card-{index}"))
                .ToDictionary(card => card.ID);
            var cardLibrary = new CardLibrary(cardDatas);

            return cardDatas.Keys
                .Select(cardId => CardTestBuilder.CreateCard(cardLibrary, cardId))
                .ToArray();
        }
    }
}
