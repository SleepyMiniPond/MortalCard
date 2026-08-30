using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class CardIdentityConditionTests
    {
        [Test]
        public void CardCollectionContains_WhenTriggeredCardIsInOwnersZone_ReturnsTrue()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = _CreateContext(built, card);
            var condition = _CreateTriggeredCardInOwnerZoneCondition(
                CardCollectionType.HandCard);

            var result = condition.Eval(context);

            Assert.That(result, Is.True);
        }

        [Test]
        public void CardCollectionContains_WhenTriggeredCardIsInDifferentZone_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.Graveyard.AddCard(card);
            var context = _CreateContext(built, card);
            var condition = _CreateTriggeredCardInOwnerZoneCondition(
                CardCollectionType.HandCard);

            var result = condition.Eval(context);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CardCollectionContains_WhenCardTargetIsMissing_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary));
            var condition = new CardCollectionContainsCondition
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = new TriggeredPlayer(),
                    Zone = CardCollectionType.HandCard
                },
                Card = new NoneCard()
            };

            var result = condition.Eval(context);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CardCollectionContains_WhenCollectionIsEmpty_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var context = _CreateContext(built, card);
            var condition = _CreateTriggeredCardInOwnerZoneCondition(
                CardCollectionType.HandCard);

            var result = condition.Eval(context);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CardCollectionContains_WhenPlayerTargetIsMissing_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var context = _CreateContext(built, card);
            var condition = new CardCollectionContainsCondition
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = new NonePlayer(),
                    Zone = CardCollectionType.HandCard
                },
                Card = new TriggeredCard()
            };

            var result = condition.Eval(context);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CardIdentityComparison_WhenTriggeredCardIsPlayingByOwner_ReturnsTrue()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var playResult = built.Ally.CardManager.TryPlayCard(card, out _, out _);
            Assert.That(playResult.Success, Is.True);
            using var playingScope = playResult.PlayCardDisposable;
            var context = _CreateContext(built, card);
            var condition = _CreateTriggeredCardIsPlayingByOwnerCondition();

            var result = condition.Eval(context);

            Assert.That(result, Is.True);
        }

        [Test]
        public void CardIdentityComparison_WhenOwnerHasNoPlayingCard_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = _CreateContext(built, card);
            var condition = _CreateTriggeredCardIsPlayingByOwnerCondition();

            var result = condition.Eval(context);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CardIdentityComparison_WhenOwnerIsPlayingDifferentCard_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var triggeredCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var playingCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(triggeredCard);
            built.Ally.CardManager.HandCard.AddCard(playingCard);
            var playResult = built.Ally.CardManager.TryPlayCard(playingCard, out _, out _);
            Assert.That(playResult.Success, Is.True);
            using var playingScope = playResult.PlayCardDisposable;
            var context = _CreateContext(built, triggeredCard);
            var condition = _CreateTriggeredCardIsPlayingByOwnerCondition();

            var result = condition.Eval(context);

            Assert.That(result, Is.False);
        }

        private static CardCollectionContainsCondition _CreateTriggeredCardInOwnerZoneCondition(
            CardCollectionType zone)
        {
            return new CardCollectionContainsCondition
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = new CardOwner { Card = new TriggeredCard() },
                    Zone = zone
                },
                Card = new TriggeredCard()
            };
        }

        private static CardCondition _CreateTriggeredCardIsPlayingByOwnerCondition()
        {
            return new CardCondition
            {
                Card = new PlayingCardOfPlayer
                {
                    Player = new CardOwner { Card = new TriggeredCard() }
                },
                Conditions =
                {
                    new CardIdentityCondition { CompareCard = new TriggeredCard() }
                }
            };
        }

        private static TriggerContext _CreateContext(BuiltGameplay built, ICardEntity card)
        {
            return new TriggerContext(
                built.Manager,
                new CardTrigger(card),
                new TestAction(SystemSource.Instance));
        }

        private sealed record TestAction(IActionSource Source) : IActionUnit
        {
            public GameTiming Timing => GameTiming.None;
        }
    }
}
