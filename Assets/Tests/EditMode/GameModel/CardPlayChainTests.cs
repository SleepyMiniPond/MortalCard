using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class CardPlayChainTests
    {
        [Test]
        public void Chain_UsesFifoAndCompletesExitRecycleAndEndTimingBeforeNextCard()
        {
            var data = new[] { "A", "B", "C", "D" }.Select(CardTestBuilder.CreateCardData).ToArray();
            data[0].PropertyDatas.Add(new RecyclePropertyData());
            var completed = new List<Guid>();
            var observer = BuffTestBuilder.CreatePlayerBuffData("end-observer", GameTiming.AfterPlayCardEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new CallbackCondition(context =>
                    {
                        Assert.That(context.Model.GameStatus.Ally.CardManager.PlayingCard.HasValue, Is.False);
                        Assert.That(context.Model.GameStatus.Enemy.CardManager.PlayingCard.HasValue, Is.False);
                        var source = (CardPlayResultSource)context.Action.Source;
                        completed.Add(source.CardPlaySource.Card.Identity);
                    }) },
                    Effect = new GainEnergyEffect()
                });
            var builder = new GameplayManagerTestBuilder().WithPlayerBuff(observer);
            foreach (var item in data) builder.WithCard(item);
            var built = builder.Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff("end-observer"));
            var cards = data.Select(item => CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, item.ID)).ToArray();
            built.Ally.CardManager.HandCard.AddCard(cards[0]);
            built.Enemy.CardManager.HandCard.AddCard(cards[1]);
            built.Ally.CardManager.HandCard.AddCard(cards[2]);
            built.Enemy.CardManager.HandCard.AddCard(cards[3]);
            _OnTiming(data[0], CardTriggeredTiming.Played, context =>
            {
                _Enqueue(context, cards[1], built.Enemy);
                _Enqueue(context, cards[2], built.Ally);
            });
            _OnTiming(data[1], CardTriggeredTiming.EffectPlayed, context =>
            {
                Assert.That(completed, Is.EqualTo(new[] { cards[0].Identity }));
                Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(cards[0]));
                Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
                _Enqueue(context, cards[3], built.Enemy);
            });

            _UseCard(built, cards[0]);

            var events = built.Manager.PopAllEvents();
            Assert.That(events.OfType<UsedCardEvent>().Select(item => item.UsedCardIdentity),
                Is.EqualTo(cards.Select(card => card.Identity)));
            Assert.That(completed, Is.EqualTo(cards.Select(card => card.Identity)));
            Assert.That(events.OfType<LoseEnergyEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
            Assert.That(built.Status.CurrentPlayer.Value.HasValue, Is.False);
        }

        [Test]
        public void DrawRoot_CompletesWholeBatchBeforePlayingRequestedCards()
        {
            var data = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            _OnTiming(data, CardTriggeredTiming.Drawed, context =>
            {
                var card = ((CardTrigger)context.Triggered).Card;
                _Enqueue(context, card, built.Ally);
            });
            var cards = Enumerable.Range(0, 2).Select(_ => CardTestBuilder.CreateCard(built.ContextManager.CardLibrary)).ToArray();
            built.Ally.CardManager.Deck.AddCards(cards);

            var events = EffectManager.DrawCards(built.Manager, SystemSource.Instance, built.Ally, 2).Events;

            Assert.That(events.Where(item => item is DrawCardEvent or UsedCardEvent).Select(item => item.GetType()),
                Is.EqualTo(new[] { typeof(DrawCardEvent), typeof(DrawCardEvent), typeof(UsedCardEvent), typeof(UsedCardEvent) }));
        }

        [Test]
        public void InvalidRequestsConsumeSharedBudgetAndNextRootStartsFresh()
        {
            var built = new GameplayManagerTestBuilder().Build();
            built.Manager.CardPlayChainBudget = 2;
            LogAssert.Expect(LogType.Error, new Regex("執行預算已耗盡"));
            built.Manager.RunEffectBatch(new[] { new CallbackItem(_Context(built), () =>
            {
                for (var i = 0; i < 3; i++)
                    built.Manager.EnqueueCardPlay(new CardPlayRequest(Guid.NewGuid(), built.Ally.Identity, SystemSource.Instance));
            }) });
            var ran = false;
            built.Manager.RunEffectBatch(new[] { new CallbackItem(_Context(built), () => ran = true) });
            Assert.That(ran, Is.True);
            Assert.That(built.Manager.PopAllEvents(), Is.Empty);
        }

        [Test]
        public void RepeatBudgetAbortPreservesCommittedEventsAndCleansPlayingAndSelection()
        {
            var data = CardTestBuilder.CreateCardData();
            data.PropertyDatas.Add(new EffectRepeatPropertyData { Value = 100 });
            data.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            built.Manager.CardPlayChainBudget = 30;
            LogAssert.Expect(LogType.Error, new Regex("執行預算已耗盡"));
            using (built.Status.SetCurrentPlayer(built.Ally))
                _UseCard(built, card);

            var events = built.Manager.PopAllEvents();
            Assert.That(events.OfType<GainEnergyEvent>(), Is.Not.Empty);
            Assert.That(events.OfType<LoseEnergyEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<UsedCardEvent>(), Is.Empty);
            Assert.That(events.OfType<MoveCardEvent>().Single().Destination, Is.EqualTo(CardCollectionType.Graveyard));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Does.Contain(card));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void ExceptionClearsQueuedRequestsAndPreservesEventsForNextRoot()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var marker = new PlayerExecuteEndEvent(built.Ally.Faction, built.Ally.CardManager.ToInfo());
            Assert.Throws<InvalidOperationException>(() => built.Manager.RunEffectBatch(new EffectQueueItem[]
            {
                new CallbackItem(_Context(built), () => built.Manager.EnqueueCardPlay(
                    new CardPlayRequest(card.Identity, built.Ally.Identity, SystemSource.Instance)), marker),
                new CallbackItem(_Context(built), () => throw new InvalidOperationException("測試例外"))
            }));
            Assert.That(built.Manager.PopAllEvents(), Is.EqualTo(new[] { marker }));
            built.Manager.RunEffectBatch(Array.Empty<EffectQueueItem>());
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(card));
        }

        [Test]
        public void CancellationClearsPendingRequestsWithoutStartingCards()
        {
            using var cancellation = new CancellationTokenSource();
            var chain = new CardPlayChain(10, cancellation.Token);
            chain.Enqueue(new CardPlayRequest(Guid.NewGuid(), Guid.NewGuid(), SystemSource.Instance));
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => chain.TryDequeue(out _));
            Assert.That(chain.PendingCount, Is.EqualTo(1));
            chain.Clear();
            Assert.That(chain.PendingCount, Is.Zero);
        }

        [Test]
        public void GameEndWaitsForEndTimingAndDiscardsRemainingRequests()
        {
            var data = CardTestBuilder.CreateCardData();
            var reachedEnd = false;
            var observer = BuffTestBuilder.CreatePlayerBuffData("end-check", GameTiming.AfterPlayCardEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new CallbackCondition(_ => reachedEnd = true) },
                    Effect = new GainEnergyEffect()
                });
            var built = new GameplayManagerTestBuilder().WithCard(data).WithPlayerBuff(observer).Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff("end-check"));
            var first = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var pending = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(first);
            built.Ally.CardManager.HandCard.AddCard(pending);
            _OnTiming(data, CardTriggeredTiming.EffectPlayed, _ =>
                built.Enemy.MainCharacter.HealthManager.TakeDamage(100, GameContext.EMPTY, DamageType.Normal));

            Assert.Throws<GameplayManager.GameEndException>(() => built.Manager.RunEffectBatch(new[]
            {
                new CallbackItem(_Context(built), () =>
                {
                    _Enqueue(_Context(built), first, built.Ally);
                    _Enqueue(_Context(built), pending, built.Ally);
                })
            }));

            Assert.That(reachedEnd, Is.True);
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Does.Contain(first));
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(pending));
            Assert.That(built.Manager.PopAllEvents().OfType<UsedCardEvent>().Select(e => e.UsedCardIdentity),
                Is.EqualTo(new[] { first.Identity }));
            built.Enemy.MainCharacter.HealthManager.GetHeal(100, GameContext.EMPTY);
            built.Manager.RunEffectBatch(Array.Empty<EffectQueueItem>());
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(pending));
        }

        [Test]
        public void PublicTimingDrainsRequestsBeforeReturningAndKeepsChildActionsSeparate()
        {
            var observer = BuffTestBuilder.CreatePlayerBuffData("root-timing", GameTiming.AfterTurnStart,
                new ConditionalPlayerBuffEffect { Effect = new GainEnergyEffect() });
            var built = new GameplayManagerTestBuilder().WithPlayerBuff(observer).Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff("root-timing"));
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            observer.BuffEffects[GameTiming.AfterTurnStart][0].Conditions.Add(new CallbackCondition(context =>
                _Enqueue(context, card, built.Ally)));

            var events = built.Manager.TriggerTiming(GameTiming.AfterTurnStart, SystemSource.Instance).ToArray();

            Assert.That(events.OfType<UsedCardEvent>().Single().UsedCardIdentity, Is.EqualTo(card.Identity));
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Does.Contain(card));
            Assert.That(built.Manager.PopAllEvents(), Is.Empty);
        }

        [Test]
        public void ExceptionDuringCardReleasesPlayingCardAndRestoresOuterSelection()
        {
            var data = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var first = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var pending = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(first);
            built.Ally.CardManager.HandCard.AddCard(pending);
            _OnTiming(data, CardTriggeredTiming.EffectPlayed, context =>
            {
                _Enqueue(context, pending, built.Ally);
                throw new InvalidOperationException("出牌中的測試例外");
            });
            var outer = GameContext.EMPTY with { SelectedCard = pending.Identity };
            using (built.ContextManager.SetContext(outer))
            {
                Assert.Throws<InvalidOperationException>(() => built.Manager.RunEffectBatch(new[]
                {
                    new CallbackItem(_Context(built), () => _Enqueue(_Context(built), first, built.Ally))
                }));
                Assert.That(built.ContextManager.Context, Is.EqualTo(outer));
            }
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Does.Contain(first));
            Assert.That(built.Manager.PopAllEvents().OfType<MoveCardEvent>().Single().CardIdentity, Is.EqualTo(first.Identity));
            data.TriggeredEffects.Clear();
            built.Manager.RunEffectBatch(Array.Empty<EffectQueueItem>());
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(pending));
        }

        [Test]
        public void ChildBudgetAbortDoesNotReturnChildActionsAsRootActions()
        {
            var data = CardTestBuilder.CreateCardData();
            data.PropertyDatas.Add(new EffectRepeatPropertyData { Value = 100 });
            data.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            built.Manager.CardPlayChainBudget = 30;
            LogAssert.Expect(LogType.Error, new Regex("執行預算已耗盡"));
            using var currentPlayer = built.Status.SetCurrentPlayer(built.Ally);
            var result = built.Manager.RunEffectBatch(new[]
            {
                new CallbackItem(_Context(built), () => _Enqueue(_Context(built), card, built.Ally))
            });
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events.OfType<GainEnergyEvent>(), Is.Not.Empty);
        }

        [Test]
        public void InitializeRequestedCardCanEndBattleInsideStartBattleResultBoundary()
        {
            var data = CardTestBuilder.CreateCardData();
            _OnTiming(data, CardTriggeredTiming.Initialize, context =>
            {
                var card = ((CardTrigger)context.Triggered).Card;
                var owner = context.Model.GameStatus.Ally;
                owner.CardManager.MoveCard(card, CardCollectionType.Deck, CardCollectionType.HandCard);
                _Enqueue(context, card, owner);
            });
            _OnTiming(data, CardTriggeredTiming.EffectPlayed, context =>
                context.Model.GameStatus.Enemy.MainCharacter.HealthManager.TakeDamage(100, GameContext.EMPTY, DamageType.Normal));
            var deck = ScriptableObject.CreateInstance<DeckScriptable>();
            try
            {
                var context = GameContextTestBuilder.CreateContextManager(
                    cardLibrary: new CardLibrary(new Dictionary<string, CardData> { [data.ID] = data }));
                var stage = new GameStageSetting("chain-initialize", 1,
                    new AllyInstance(Guid.NewGuid(), "ally", 0, 100, 100, 0, 3,
                        new List<CardInstance> { CardInstance.Create(data) }, 5),
                    new EnemyData
                    {
                        EnemyID = "enemy", SelectedCardMaxCount = 0,
                        PlayerData = new PlayerData
                        {
                            ID = "enemy-player", NameKey = "enemy", MaxHealth = 100, InitialHealth = 100,
                            MaxEnergy = 3, InitialEnergy = 0, HandCardMaxCount = 5, Deck = deck
                        }
                    });
                var manager = new GameplayManager(stage, context);

                var result = manager.StartBattle(CancellationToken.None).GetAwaiter().GetResult();

                Assert.That(result.HasValue, Is.True);
                Assert.That(manager.PopAllEvents().OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
                Assert.That(context.Context, Is.EqualTo(GameContext.EMPTY));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(deck);
            }
        }

        [Test]
        public void TurnHandClearTimingDrainsRequestsWithoutNextPlayerAction()
        {
            var discarded = CardTestBuilder.CreateCardData("discarded");
            var preserved = CardTestBuilder.CreateCardData("preserved");
            preserved.PropertyDatas.Add(new PreservedPropertyData());
            var built = new GameplayManagerTestBuilder().WithCard(discarded).WithCard(preserved).Build();
            var discardedCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, discarded.ID);
            var preservedCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, preserved.ID);
            built.Ally.CardManager.HandCard.AddCard(discardedCard);
            built.Ally.CardManager.HandCard.AddCard(preservedCard);
            _OnTiming(discarded, CardTriggeredTiming.Discarded, context => _Enqueue(context, preservedCard, built.Ally));

            typeof(GameplayManager).GetMethod("_TurnEnd", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(built.Manager, null);

            Assert.That(built.Manager.PopAllEvents().OfType<UsedCardEvent>().Single().UsedCardIdentity,
                Is.EqualTo(preservedCard.Identity));
        }

        [Test]
        public void ActiveRootExceptionStoresEventsOnceAndReleasesChain()
        {
            var data = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            _OnTiming(data, CardTriggeredTiming.Played, _ => throw new InvalidOperationException("主動出牌測試例外"));

            var error = Assert.Throws<TargetInvocationException>(() => _UseCard(built, card));

            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            var events = built.Manager.PopAllEvents();
            Assert.That(events.OfType<LoseEnergyEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<MoveCardEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
            Assert.Throws<InvalidOperationException>(() => _Enqueue(_Context(built), card, built.Ally));
            data.TriggeredEffects.Clear();
            var next = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(next);
            _UseCard(built, next);
            Assert.That(built.Manager.PopAllEvents().OfType<UsedCardEvent>().Single().UsedCardIdentity, Is.EqualTo(next.Identity));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RootEventsAreDeliveredExactlyOnceToTheirOwner(bool active)
        {
            var built = new GameplayManagerTestBuilder().Build();
            if (active)
            {
                var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
                built.Ally.CardManager.HandCard.AddCard(card);
                _UseCard(built, card);
                Assert.That(built.Manager.PopAllEvents().OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
            }
            else
            {
                var marker = new PlayerExecuteEndEvent(built.Ally.Faction, built.Ally.CardManager.ToInfo());
                var result = built.Manager.RunEffectBatch(new[] { new CallbackItem(_Context(built), () => { }, marker) });
                Assert.That(result.Events, Is.EqualTo(new[] { marker }));
            }
            Assert.That(built.Manager.PopAllEvents(), Is.Empty);
        }

        private static void _Enqueue(TriggerContext context, ICardEntity card, IPlayerEntity owner)
            => context.Model.EnqueueCardPlay(new CardPlayRequest(card.Identity, owner.Identity, context.Action.Source));

        private static TriggerContext _Context(BuiltGameplay built)
            => new(built.Manager, new PlayerTrigger(built.Ally), new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));

        private static void _OnTiming(StandardCardData data, CardTriggeredTiming timing, Action<TriggerContext> callback)
        {
            data.TriggeredEffects[timing] = new[] { new ConditionalCardEffect
            {
                Conditions = { new CallbackCondition(callback) },
                Effect = new GainEnergyEffect()
            } };
        }

        private static void _UseCard(BuiltGameplay built, ICardEntity card)
        {
            typeof(GameplayManager).GetMethod("_UseCard", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(built.Manager, new object[] { built.Ally, new UseCardAction(card.Identity,
                    MainSelectionAction.Empty, new Dictionary<string, ISubSelectionAction>()) });
        }

        // 測試探針只觀察派送邊界／提交請求，不新增尚未實作的正式 Effect。
        private sealed class CallbackCondition : ICondition
        {
            private readonly Action<TriggerContext> _callback;
            public CallbackCondition(Action<TriggerContext> callback) => _callback = callback;
            public bool Eval(TriggerContext context)
            {
                _callback(context);
                return false;
            }
        }

        private sealed record CallbackItem(TriggerContext Context, Action Callback, IGameEvent Event = null) : EffectQueueItem(Context)
        {
            public override EffectResult Execute(IEffectQueueContext queue)
            {
                Callback();
                return new EffectResult(Array.Empty<BaseResultAction>(), Event == null ? Array.Empty<IGameEvent>() : new[] { Event });
            }
        }
    }
}
