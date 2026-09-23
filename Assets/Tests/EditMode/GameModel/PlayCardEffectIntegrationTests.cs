using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using NUnit.Framework;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class PlayCardEffectIntegrationTests
    {
        [Test]
        public void EnemyActionQueryPreservesSelectionAndSkipsAttemptedCards()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var first = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var second = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCards(new[] { first, second });
            built.Enemy.SelectedCards.TryAddCard(first);
            built.Enemy.SelectedCards.TryAddCard(second);
            var attempted = new HashSet<Guid>();

            Assert.That(built.Enemy.TryGetNextUseCardAction(built.Manager, attempted, out var action), Is.True);
            Assert.That(action.CardIndentity, Is.EqualTo(first.Identity));
            Assert.That(built.Enemy.SelectedCards.Cards, Is.EqualTo(new[] { first, second }));
            attempted.Add(first.Identity);
            Assert.That(built.Enemy.TryGetNextUseCardAction(built.Manager, attempted, out action), Is.True);
            Assert.That(action.CardIndentity, Is.EqualTo(second.Identity));
            attempted.Add(second.Identity);
            Assert.That(built.Enemy.TryGetNextUseCardAction(built.Manager, attempted, out _), Is.False);
            Assert.That(built.Enemy.SelectedCards.Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void BeginEnemyCardPlayClearsSelectionOnlyAfterEnteringPlayingCard()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.SelectedCards.TryAddCard(card);
            IPlayerEntity player = built.Enemy;

            var failed = player.TryBeginCardPlay(card);
            Assert.That(failed.HasValue, Is.False);
            Assert.That(built.Enemy.SelectedCards.Cards, Does.Contain(card));
            built.Enemy.CardManager.HandCard.AddCard(card);
            var started = player.TryBeginCardPlay(card);
            Assert.That(started.TryGetValue(out var play), Is.True);
            using (play)
            {
                Assert.That(player.CardManager.PlayingCard.HasValue, Is.True);
                Assert.That(built.Enemy.SelectedCards.Cards, Is.Empty);
            }
            Assert.That(player.CardManager.Graveyard.Cards, Does.Contain(card));
        }

        [Test]
        public void EnemyExecuteContinuesAfterFailedCardAndClearsRemainingSelectionAtEnd()
        {
            var sealedData = CardTestBuilder.CreateCardData("sealed");
            sealedData.PropertyDatas.Add(new SealedPropertyData());
            var built = new GameplayManagerTestBuilder().WithCard(sealedData).Build();
            var failed = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, "sealed");
            var playable = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCards(new[] { failed, playable });
            built.Enemy.SelectedCards.TryAddCard(failed);
            built.Enemy.SelectedCards.TryAddCard(playable);
            // 此測試直接進入敵方階段，補上正式 StartBattle 會建立的輸入佇列。
            typeof(GameplayManager).GetField("_gameActions", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(built.Manager, new UniTaskAwaitableQueue<IGameAction>());

            typeof(GameplayManager).GetMethod("_EnemyExecute", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(built.Manager, Array.Empty<object>());

            var events = built.Manager.PopAllEvents().ToArray();
            Assert.That(events.OfType<UsedCardEvent>().Single().UsedCardIdentity, Is.EqualTo(playable.Identity));
            Assert.That(events.OfType<UsedCardEvent>().Single().Reason, Is.EqualTo(CardPlayReason.Active));
            Assert.That(events.OfType<EnemyUnselectedCardEvent>().Single().UnselectedCards,
                Is.EqualTo(new[] { failed.Identity }));
            Assert.That(built.Enemy.CardManager.HandCard.Cards, Does.Contain(failed));
            Assert.That(built.Enemy.SelectedCards.Cards, Is.Empty);

            // 每次執行階段都使用新的嘗試集合。
            built.Enemy.CardManager.Graveyard.RemoveCard(playable);
            built.Enemy.CardManager.HandCard.AddCard(playable);
            built.Enemy.SelectedCards.TryAddCard(playable);
            typeof(GameplayManager).GetMethod("_EnemyExecute", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(built.Manager, Array.Empty<object>());
            Assert.That(built.Manager.PopAllEvents().OfType<UsedCardEvent>().Single().UsedCardIdentity,
                Is.EqualTo(playable.Identity));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void AllSources_PlayForFreeAndDeduplicateWithinResolution(int source)
        {
            var data = CardTestBuilder.CreateCardData();
            data.Cost = 99;
            data.PropertyDatas.Add(new RecyclePropertyData());
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = _Context(built);
            var effect = new PlayCardEffect { TargetCards = new FixedCards(card, card) };
            EffectQueueItem item = source switch
            {
                0 => new CardEffectQueueItem(context, effect),
                1 => new PlayerBuffEffectQueueItem(context, effect),
                2 => new CharacterBuffEffectQueueItem(context, effect),
                _ => new CardBuffEffectQueueItem(context, effect)
            };

            var result = built.Manager.RunEffectBatch(new[] { item });

            Assert.That(result.Events.OfType<UsedCardEvent>().Single().Reason, Is.EqualTo(CardPlayReason.Effect));
            Assert.That(result.Events.OfType<LoseEnergyEvent>(), Is.Empty);
            Assert.That(result.Actions, Is.Empty);
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(card));
            Assert.That(built.Status.CurrentPlayer.Value.HasValue, Is.False);
        }

        [Test]
        public void RepeatRepeatsEffectsButEffectPlayedRunsOnceAndPlayedNeverRuns()
        {
            var data = CardTestBuilder.CreateCardData();
            data.PropertyDatas.Add(new EffectRepeatPropertyData { Value = 3 });
            data.Effects.Add(_Gain());
            data.TriggeredEffects[CardTriggeredTiming.EffectPlayed] = new[] { new ConditionalCardEffect { Effect = _Gain() } };
            data.TriggeredEffects[CardTriggeredTiming.Played] = new[] { new ConditionalCardEffect { Effect = _Gain() } };
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);

            var result = _Run(built, card);

            Assert.That(result.Events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(4));
            Assert.That(result.Events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnemyCardsPlayWithOrWithoutPreselectionWithoutChangingCurrentPlayer(bool selected)
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(card);
            if (selected) built.Enemy.SelectedCards.TryAddCard(card);
            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                var used = _Run(built, card).Events.OfType<UsedCardEvent>().Single();
                Assert.That(used.Reason, Is.EqualTo(CardPlayReason.Effect));
                Assert.That(used.CardManagerInfo.CardZoneInfos[CardCollectionType.HandCard].Contains(card.Identity),
                    Is.False);
                Assert.That(built.Status.CurrentPlayer.Value.TryGetValue(out var current), Is.True);
                Assert.That(current, Is.SameAs(built.Ally));
            }
            Assert.That(built.Enemy.SelectedCards.Cards.Contains(card), Is.False);
            Assert.That(built.Enemy.CardManager.Graveyard.Cards, Does.Contain(card));
        }

        [Test]
        public void UndisplayedEnemyCardCanReceiveUsedEventWithoutASelectedCardView()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(card);
            var used = _Run(built, card).Events.OfType<UsedCardEvent>().Single();
            var viewObject = new GameObject("UndisplayedEnemyCardViewTest");
            try
            {
                var view = viewObject.AddComponent<EnemySelectedCardView>();
                Assert.DoesNotThrow(() => view.RemoveCardView(used));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        [Test]
        public void SealedEnemyRequestDoesNotRemovePreselection()
        {
            var data = CardTestBuilder.CreateCardData();
            data.PropertyDatas.Add(new SealedPropertyData());
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(card);
            built.Enemy.SelectedCards.TryAddCard(card);
            Assert.That(_Run(built, card).Events.OfType<UsedCardEvent>(), Is.Empty);
            Assert.That(built.Enemy.SelectedCards.Cards, Does.Contain(card));
            Assert.That(built.Enemy.CardManager.HandCard.Cards, Does.Contain(card));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void SeparateRequestsRevalidateHandAndAllowRecycledCard(bool recycle, int count)
        {
            var data = CardTestBuilder.CreateCardData();
            if (recycle) data.PropertyDatas.Add(new RecyclePropertyData());
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var item = new CardEffectQueueItem(_Context(built), new PlayCardEffect { TargetCards = new FixedCards(card) });
            var result = built.Manager.RunEffectBatch(new[] { item, item });
            Assert.That(result.Events.OfType<UsedCardEvent>().Count(), Is.EqualTo(count));
        }

        [Test]
        public void DrawTimingCanRequestFullPlayFromSystemRoot()
        {
            var data = CardTestBuilder.CreateCardData();
            data.TriggeredEffects[CardTriggeredTiming.Drawed] = new[] { new ConditionalCardEffect
            {
                Effect = new PlayCardEffect { TargetCards = new SingleCardCollection { TargetCard = new TriggeredCard() } }
            } };
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.Deck.AddCards(new[] { card });
            var result = EffectManager.DrawCards(built.Manager, SystemSource.Instance, built.Ally, 1);
            Assert.That(result.Events.OfType<UsedCardEvent>().Single().UsedCardIdentity, Is.EqualTo(card.Identity));
            Assert.That(result.Events.OfType<LoseEnergyEvent>(), Is.Empty);
        }

        [Test]
        public void CardDataBeforeCardBuffAndNestedRequestsPreserveFifo()
        {
            var data = new[] { "A", "B", "C", "D" }.Select(CardTestBuilder.CreateCardData).ToArray();
            var buff = new CardBuffData { ID = "play-chain", LifeTimeData = new AlwaysLifeTimeCardBuffData() };
            var builder = new GameplayManagerTestBuilder().WithCardBuff(buff);
            foreach (var item in data) builder.WithCard(item);
            var built = builder.Build();
            var cards = data.Select(item => CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, item.ID)).ToArray();
            cards[0] = CardTestBuilder.CreateCardWithBuff(_Context(built), built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary, "A", "play-chain");
            built.Ally.CardManager.HandCard.AddCards(cards);
            data[0].TriggeredEffects[CardTriggeredTiming.EffectPlayed] = new[] { new ConditionalCardEffect
            {
                Effect = new PlayCardEffect { TargetCards = new FixedCards(cards[1]) }
            } };
            buff.Effects[CardTriggeredTiming.EffectPlayed] = new[] { new ConditionalCardBuffEffect
            {
                Effect = new PlayCardEffect { TargetCards = new FixedCards(cards[2]) }
            } };
            data[1].Effects.Add(new PlayCardEffect { TargetCards = new FixedCards(cards[3]) });

            var result = _Run(built, cards[0]);

            Assert.That(result.Events.OfType<UsedCardEvent>().Select(item => item.UsedCardIdentity),
                Is.EqualTo(cards.Select(card => card.Identity)));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        private static GainEnergyEffect _Gain() => new()
        {
            Targets = new SinglePlayerCollection { Target = new PlayerByFaction { Faction = Faction.Ally } },
            Value = new ConstInteger { Value = 1 }
        };

        private static TriggerContext _Context(BuiltGameplay built)
            => new(built.Manager, new PlayerTrigger(built.Ally), new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));

        private static EffectResult _Run(BuiltGameplay built, ICardEntity card)
            => built.Manager.RunEffectBatch(new[] { new CardEffectQueueItem(_Context(built),
                new PlayCardEffect { TargetCards = new FixedCards(card) }) });

        private sealed class FixedCards : ITargetCardCollectionValue
        {
            private readonly ICardEntity[] _cards;
            public FixedCards(params ICardEntity[] cards) => _cards = cards;
            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext context) => _cards;
        }
    }
}
