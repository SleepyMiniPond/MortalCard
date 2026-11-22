using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presenter;
using NUnit.Framework;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class InitialDeckPriorityTests
    {
        private readonly List<UnityEngine.Object> _assets = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _assets)
                UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void ApplyInitialPriority_PreservesRelativeOrderAndEveryCard(int priorityCount)
        {
            var cards = _Cards(4, priorityCount);
            var deck = new DeckEntity(new GameRandom(7));
            deck.AddCards(cards);

            deck.ApplyInitialPriority();

            var expected = cards.Where(card => card.HasProperty(CardProperty.InitialPriority))
                .Concat(cards.Where(card => !card.HasProperty(CardProperty.InitialPriority)));
            Assert.That(deck.Cards, Is.EqualTo(expected));
            Assert.That(deck.Cards, Is.EquivalentTo(cards));
        }

        [Test]
        public void ApplyInitialPriority_EmptyDeck_RemainsEmpty()
        {
            var deck = new DeckEntity(new GameRandom(7));
            deck.ApplyInitialPriority();
            Assert.That(deck.Cards, Is.Empty);
        }

        [TestCase(7)]
        [TestCase(145)]
        public void ApplyInitialPriority_PreservesShuffledOrderAndRandomState(int seed)
        {
            var cards = _Cards(8, 4);
            var random = new GameRandom(seed);
            var baseline = new GameRandom(seed);
            var deck = new DeckEntity(random);
            deck.EnqueueCardsThenShuffle(cards);
            var expected = cards.ToList();
            baseline.ShuffleInPlace(expected);

            deck.ApplyInitialPriority();

            Assert.That(deck.Cards, Is.EqualTo(expected.OrderByDescending(card => card.HasProperty(CardProperty.InitialPriority))));
            Assert.That(random.Range(0, int.MaxValue), Is.EqualTo(baseline.Range(0, int.MaxValue)));
        }

        [Test]
        public void LaterShuffle_DoesNotReapplyPriority()
        {
            var cards = _Cards(4, 2);
            var added = _Cards(1, 0).Single();
            var random = new GameRandom(145);
            var baseline = new GameRandom(145);
            var deck = new DeckEntity(random);
            deck.AddCards(cards);
            deck.ApplyInitialPriority();
            var expected = deck.Cards.ToList();
            expected.Add(added);
            baseline.ShuffleInPlace(expected);

            deck.EnqueueCardsThenShuffle(new[] { added });

            Assert.That(deck.Cards, Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(4)]
        public void DrawCards_UsesOrderedDeckWithoutIncreasingDrawCount(int drawCount)
        {
            var built = new GameplayManagerTestBuilder().Build();
            var cards = _Cards(5, 3);
            built.Ally.CardManager.Deck.AddCards(cards);
            built.Ally.CardManager.Deck.ApplyInitialPriority();
            var expected = built.Ally.CardManager.Deck.Cards.ToArray();

            var result = EffectManager.DrawCards(built.Manager, SystemSource.Instance, built.Ally, drawCount);

            Assert.That(result.Events.OfType<DrawCardEvent>().Select(item => item.NewCardInfo.Identity),
                Is.EqualTo(expected.Take(drawCount).Select(card => card.Identity)));
            Assert.That(built.Ally.CardManager.Deck.Cards, Is.EqualTo(expected.Skip(drawCount)));
        }

        [Test]
        public void GameStart_OrdersBothDecksAfterInitialize_AndPublishesSnapshots()
        {
            var cards = Enumerable.Range(0, 4).Select(i => _Data("initial-" + i, i % 2 == 1)).ToArray();
            var observedInitializeOrder = new List<Guid>();
            Guid[] initialOrder = null;
            foreach (var data in cards)
            {
                _ObserveInitialize(data, context =>
                {
                    initialOrder ??= context.Model.GameStatus.Ally.CardManager.Deck.Cards
                        .Concat(context.Model.GameStatus.Enemy.CardManager.Deck.Cards).Select(card => card.Identity).ToArray();
                    observedInitializeOrder.Add(((CardTrigger)context.Triggered).Card.Identity);
                });
            }
            var manager = _Manager(cards, cards, cards);

            _Start(manager);

            Assert.That(observedInitializeOrder, Is.EqualTo(initialOrder));
            var snapshots = manager.PopAllEvents().OfType<InitialDeckOrderedEvent>().ToArray();
            Assert.That(snapshots.Select(item => item.Faction), Is.EqualTo(new[] { Faction.Ally, Faction.Enemy }));
            var status = ((IGameplayModel)manager).GameStatus;
            foreach (var player in new IPlayerEntity[] { status.Ally, status.Enemy })
            {
                Assert.That(player.CardManager.Deck.Cards.Select(card => card.HasProperty(CardProperty.InitialPriority)),
                    Is.EqualTo(new[] { true, true, false, false }));
                Assert.That(snapshots.Single(item => item.Faction == player.Faction).CardManagerInfo.CardZoneInfos[CardCollectionType.Deck],
                    Is.EqualTo(player.CardManager.Deck.Cards.Select(card => card.Identity)));
            }
        }

        [Test]
        public void GameStart_InitializeCreatedPriorityCard_ParticipatesWithoutInitializingAgain()
        {
            var initial = _Data("initial", false);
            var created = _Data("created", true);
            var createdInitializeCount = 0;
            _ObserveInitialize(created, _ => createdInitializeCount++);
            initial.TriggeredEffects[CardTriggeredTiming.Initialize] = new[]
            {
                new ConditionalCardEffect
                {
                    Effect = new CreateCardEffect
                    {
                        Target = new PlayerByFaction { Faction = Faction.Ally },
                        CardDataIds = new List<string> { created.ID },
                        CreateDestination = CardCollectionType.Deck
                    }
                }
            };
            var manager = _Manager(new[] { initial, created }, new[] { initial });

            _Start(manager);

            var deck = ((IGameplayModel)manager).GameStatus.Ally.CardManager.Deck;
            Assert.That(deck.Cards.Select(card => card.CardDataId), Is.EqualTo(new[] { created.ID, initial.ID }));
            Assert.That(createdInitializeCount, Is.Zero);
        }

        [Test]
        public void GameStart_InitializeDraw_IsNotDelayedOrGivenPriority_AndDrawnCardStaysInHand()
        {
            var initial = _Data("initial", false);
            var priority = _Data("priority", true);
            Guid expectedDraw = default;
            _ObserveInitialize(initial, context => expectedDraw = context.Model.GameStatus.Ally.CardManager.Deck.Cards.First().Identity);
            initial.TriggeredEffects[CardTriggeredTiming.Initialize] = initial.TriggeredEffects[CardTriggeredTiming.Initialize]
                .Append(new ConditionalCardEffect { Effect = _Draw() }).ToArray();
            var manager = _Manager(new[] { initial, priority }, new[] { initial, priority });

            _Start(manager);

            var status = ((IGameplayModel)manager).GameStatus;
            Assert.That(status.Ally.CardManager.HandCard.Cards.Single().Identity, Is.EqualTo(expectedDraw));
            var events = manager.PopAllEvents().ToArray();
            Assert.That(Array.FindIndex(events, item => item is DrawCardEvent),
                Is.LessThan(Array.FindIndex(events, item => item is InitialDeckOrderedEvent)));
            Assert.That(status.Ally.CardManager.Deck.Cards, Has.Count.EqualTo(1));
        }

        [Test]
        public void GameStart_AfterGameStartBuffDraw_UsesPriorityAndFollowsSnapshot()
        {
            var initial = _Data("initial", false);
            var priority = _Data("priority", true);
            var buff = BuffTestBuilder.CreateCardBuffData("opening-draw", GameTiming.AfterGameStart,
                new ConditionalCardBuffEffect { Effect = _Draw() });
            _ObserveInitialize(initial, context =>
            {
                var card = ((CardTrigger)context.Triggered).Card;
                card.BuffManager.AddBuff(BuffTestBuilder.CreateCardBuff(context, context.Model.ContextManager.CardBuffLibrary, buff.ID));
            });
            var manager = _Manager(new[] { initial, priority }, new[] { initial, priority }, buff: buff);

            _Start(manager);

            var events = manager.PopAllEvents().ToArray();
            var drawn = events.OfType<DrawCardEvent>().Single();
            Assert.That(drawn.NewCardInfo.CardDataID, Is.EqualTo(priority.ID));
            Assert.That(Array.FindLastIndex(events, item => item is InitialDeckOrderedEvent),
                Is.LessThan(Array.FindIndex(events, item => item is DrawCardEvent)));
        }

        [Test]
        public void GameStart_InitializeChangesForm_UsesEffectivePropertyAtOrderingTime()
        {
            var initial = _Data("initial", false);
            var other = _Data("other", false);
            var priorityForm = _Data("priority-form", true);
            _ObserveInitialize(initial, context => ((CardTrigger)context.Triggered).Card
                .TryApplySelfForm("opening-form", priorityForm.ID, CardFormPersistence.Persistent));
            var manager = _Manager(new[] { initial, other, priorityForm }, new[] { other, initial });

            _Start(manager);

            Assert.That(((IGameplayModel)manager).GameStatus.Ally.CardManager.Deck.Cards
                .Select(card => card.CardDataId), Is.EqualTo(new[] { priorityForm.ID, other.ID }));
        }

        [Test]
        public void GameStart_InitializeMovesPriorityCardOutOfDeck_DoesNotPullItBack()
        {
            var initial = _Data("initial", false);
            var priority = _Data("priority", true);
            _ObserveInitialize(priority, context =>
            {
                var card = ((CardTrigger)context.Triggered).Card;
                context.Model.GameStatus.Ally.CardManager.MoveCard(card,
                    CardCollectionType.Deck, CardCollectionType.Graveyard);
            });
            var manager = _Manager(new[] { initial, priority }, new[] { initial, priority });

            _Start(manager);

            var cards = ((IGameplayModel)manager).GameStatus.Ally.CardManager;
            Assert.That(cards.Deck.Cards.Single().CardDataId, Is.EqualTo(initial.ID));
            Assert.That(cards.Graveyard.Cards.Single().CardDataId, Is.EqualTo(priority.ID));
        }

        [Test]
        public void DrawCards_RecyclesGraveyardWithNormalShuffle_WithoutPrioritySorting()
        {
            const int seed = 145;
            var built = new GameplayManagerTestBuilder().WithRandomSeed(seed).Build();
            var cards = _Cards(6, 3);
            built.Ally.CardManager.Graveyard.AddCards(cards);
            var expected = cards.ToList();
            new GameRandom(seed).ShuffleInPlace(expected);

            var result = EffectManager.DrawCards(built.Manager, SystemSource.Instance, built.Ally, 1);

            Assert.That(result.Events.OfType<RecycleGraveyardToDeckEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Ally.CardManager.HandCard.Cards.Single(), Is.SameAs(expected[0]));
            Assert.That(built.Ally.CardManager.Deck.Cards, Is.EqualTo(expected.Skip(1)));
        }

        [Test]
        public void GameplayView_InitialDeckOrderedEvent_UpdatesDisplayedDeckOrder()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var cards = _Cards(4, 2);
            built.Ally.CardManager.Deck.AddCards(cards);
            var viewModel = new GameViewModel();
            foreach (var card in cards) viewModel.UpdateCardInfo(card.ToInfo(built.Manager));
            viewModel.UpdateCardManagerInfo(Faction.Ally, built.Ally.CardManager.ToInfo());
            built.Ally.CardManager.Deck.ApplyInitialPriority();
            var host = new GameObject("初始牌堆同步測試");
            _assets.Add(host);
            var view = host.AddComponent<GameplayView>();
            typeof(GameplayView).GetField("_gameViewModel", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, viewModel);

            view.Render(new IGameEvent[]
            {
                new InitialDeckOrderedEvent(Faction.Ally, built.Ally.CardManager.ToInfo())
            }, null);

            var displayed = viewModel.ObservableCardCollectionInfo(Faction.Ally, CardCollectionType.Deck)
                .Value.CardInfos.OrderBy(pair => pair.Value).Select(pair => pair.Key.Identity);
            Assert.That(displayed, Is.EqualTo(built.Ally.CardManager.Deck.Cards.Select(card => card.Identity)));
        }

        private GameplayManager _Manager(StandardCardData[] catalog, StandardCardData[] allyCards,
            StandardCardData[] enemyCards = null, CardBuffData buff = null)
        {
            var deck = ScriptableObject.CreateInstance<DeckScriptable>();
            _assets.Add(deck);
            deck.Cards = (enemyCards ?? Array.Empty<StandardCardData>()).Select(data =>
            {
                var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
                asset.Data = data;
                _assets.Add(asset);
                return asset;
            }).ToArray();
            var context = GameContextTestBuilder.CreateContextManager(
                cardLibrary: new CardLibrary(catalog.ToDictionary(data => data.ID, data => (CardData)data)),
                cardBuffLibrary: new CardBuffLibrary(buff == null ? new Dictionary<string, CardBuffData>()
                    : new Dictionary<string, CardBuffData> { [buff.ID] = buff }));
            var stage = new GameStageSetting("initial-priority", 1,
                new AllyInstance(Guid.NewGuid(), "ally", 0, 100, 100, 0, 3,
                    allyCards.Select(CardInstance.Create).ToList(), 10),
                new EnemyData
                {
                    EnemyID = "enemy", SelectedCardMaxCount = 0,
                    PlayerData = new PlayerData
                    {
                        ID = "enemy-player", NameKey = "enemy", MaxHealth = 100, InitialHealth = 100,
                        MaxEnergy = 3, InitialEnergy = 0, HandCardMaxCount = 10, Deck = deck
                    }
                });
            return new GameplayManager(stage, context);
        }

        private static void _Start(GameplayManager manager) => typeof(GameplayManager)
            .GetMethod("_GameStart", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);

        private static StandardCardData _Data(string id, bool priority)
        {
            var data = CardTestBuilder.CreateCardData(id);
            if (priority) data.PropertyDatas.Add(new InitialPriorityPropertyData());
            return data;
        }

        private static ICardEntity[] _Cards(int count, int priorityCount)
        {
            var data = Enumerable.Range(0, count).Select(i => _Data("card-" + i, i >= count - priorityCount)).ToArray();
            var library = new CardLibrary(data.ToDictionary(card => card.ID, card => (CardData)card));
            return data.Select(card => CardTestBuilder.CreateCard(library, card.ID)).ToArray();
        }

        private static DrawCardEffect _Draw() => new()
        {
            Targets = new SinglePlayerCollection { Target = new PlayerByFaction { Faction = Faction.Ally } },
            Value = new ConstInteger { Value = 1 }
        };

        // 探針只觀察時序與配置測試反應，不新增正式效果型別。
        private static void _ObserveInitialize(StandardCardData data, Action<TriggerContext> observe) =>
            data.TriggeredEffects[CardTriggeredTiming.Initialize] = new[]
            {
                new ConditionalCardEffect { Conditions = { new ObserveCondition(observe) }, Effect = new GainEnergyEffect() }
            };

        private sealed class ObserveCondition : ICondition
        {
            private readonly Action<TriggerContext> _observe;
            public ObserveCondition(Action<TriggerContext> observe) => _observe = observe;
            public bool Eval(TriggerContext context)
            {
                _observe(context);
                return false;
            }
        }
    }
}
