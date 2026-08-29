using System;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class CardTargetValueTests
    {
        [Test]
        public void CardOwner_WhenCardExists_ReturnsOwningPlayer()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = _CreateContext(
                built,
                new CardTrigger(card),
                SystemSource.Instance);
            var target = new CardOwner { Card = new TriggeredCard() };

            var result = target.Eval(context);

            Assert.That(result.TryGetValue(out var owner), Is.True);
            Assert.That(owner, Is.SameAs(built.Ally));
        }

        [Test]
        public void CardOwner_WhenCardTargetIsMissing_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);

            var result = new CardOwner { Card = new NoneCard() }.Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void ActionCard_WhenSourceIsCardPlay_ReturnsSourceCard()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var cardPlaySource = _CreateCardPlaySource(card, built.Ally);
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                cardPlaySource);

            var result = new ActionCard().Eval(context);

            Assert.That(result.TryGetValue(out var actionCard), Is.True);
            Assert.That(actionCard, Is.SameAs(card));
        }

        [Test]
        public void ActionCard_WhenSourceIsCardPlayResult_ReturnsOriginalSourceCard()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var resultSource = _CreateCardPlaySource(card, built.Ally)
                .CreateResultSource(Array.Empty<IEffectResultAction>());
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                resultSource);

            var result = new ActionCard().Eval(context);

            Assert.That(result.TryGetValue(out var actionCard), Is.True);
            Assert.That(actionCard, Is.SameAs(card));
        }

        [Test]
        public void ActionCard_WhenSourceIsNotCardPlay_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);

            var result = new ActionCard().Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void PlayingCardOfPlayer_WhenPlayerIsPlayingCard_ReturnsTransientCard()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var playResult = built.Ally.CardManager.TryPlayCard(card, out _, out _);
            Assert.That(playResult.Success, Is.True);
            using var playingScope = playResult.PlayCardDisposable;
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);
            var target = new PlayingCardOfPlayer
            {
                Player = new TriggeredPlayer()
            };

            var result = target.Eval(context);

            Assert.That(result.TryGetValue(out var playingCard), Is.True);
            Assert.That(playingCard, Is.SameAs(card));
        }

        [Test]
        public void PlayingCardOfPlayer_WhenPlayerHasNoPlayingCard_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);
            var target = new PlayingCardOfPlayer
            {
                Player = new TriggeredPlayer()
            };

            var result = target.Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void PlayingCardOfPlayer_WhenPlayerTargetIsMissing_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);
            var target = new PlayingCardOfPlayer { Player = new NonePlayer() };

            var result = target.Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [TestCase(CardCollectionType.Deck)]
        [TestCase(CardCollectionType.HandCard)]
        [TestCase(CardCollectionType.Graveyard)]
        [TestCase(CardCollectionType.ExclusionZone)]
        [TestCase(CardCollectionType.DisposeZone)]
        public void CardsOfPlayer_WhenZoneExists_ReturnsCardsInExistingOrder(
            CardCollectionType zone)
        {
            var built = new GameplayManagerTestBuilder().Build();
            var first = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var second = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.GetCardCollectionZone(zone).AddCard(first);
            built.Ally.CardManager.GetCardCollectionZone(zone).AddCard(second);
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);
            var target = new CardsOfPlayer
            {
                Player = new TriggeredPlayer(),
                Zone = zone
            };

            var result = target.Eval(context);

            Assert.That(result.ToArray(), Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void CardsOfPlayer_WhenPlayerTargetIsMissing_ReturnsEmptyCollection()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);
            var target = new CardsOfPlayer
            {
                Player = new NonePlayer(),
                Zone = CardCollectionType.HandCard
            };

            var result = target.Eval(context);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CardsOfPlayer_WhenCardIsPlaying_DoesNotTreatItAsHandCard()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var playResult = built.Ally.CardManager.TryPlayCard(card, out _, out _);
            Assert.That(playResult.Success, Is.True);
            using var playingScope = playResult.PlayCardDisposable;
            var context = _CreateContext(
                built,
                new PlayerTrigger(built.Ally),
                SystemSource.Instance);

            var handCards = new CardsOfPlayer
            {
                Player = new TriggeredPlayer(),
                Zone = CardCollectionType.HandCard
            }.Eval(context);
            var playingCard = new PlayingCardOfPlayer
            {
                Player = new TriggeredPlayer()
            }.Eval(context);

            Assert.That(handCards, Is.Empty);
            Assert.That(playingCard.TryGetValue(out var actualPlayingCard), Is.True);
            Assert.That(actualPlayingCard, Is.SameAs(card));
            Assert.That(Enum.GetNames(typeof(CardCollectionType)),
                Does.Not.Contain("PlayingCard"));
        }

        private static TriggerContext _CreateContext(
            BuiltGameplay built,
            ITriggeredSource triggered,
            IActionSource source)
        {
            return new TriggerContext(
                built.Manager,
                triggered,
                new TestAction(source));
        }

        private static CardPlaySource _CreateCardPlaySource(
            ICardEntity card,
            IPlayerEntity player)
        {
            return new CardPlaySource(
                card,
                0,
                1,
                new LoseEnergyEffectCommand(player, 0),
                new CardPlayAttributeEntity());
        }

        private sealed record TestAction(IActionSource Source) : IActionUnit
        {
            public GameTiming Timing => GameTiming.None;
        }
    }
}
