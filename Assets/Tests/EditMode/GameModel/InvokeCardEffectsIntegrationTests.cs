using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class InvokeCardEffectsIntegrationTests
    {
        [TestCase(false, CardCollectionType.HandCard)]
        [TestCase(false, CardCollectionType.Deck)]
        [TestCase(false, CardCollectionType.Graveyard)]
        [TestCase(true, CardCollectionType.HandCard)]
        [TestCase(true, CardCollectionType.Deck)]
        [TestCase(true, CardCollectionType.Graveyard)]
        public void Invoke_ExecutesOnceInPlaceWithoutPaymentOrPlayTimings(bool enemyOwner, CardCollectionType zone)
        {
            var data = _Attack();
            data.Cost = 99;
            data.PropertyDatas.AddRange(new ICardPropertyData[]
            {
                new SealedPropertyData(), new EffectRepeatPropertyData { Value = 3 },
                new RecyclePropertyData(), new ConsumablePropertyData()
            });
            foreach (var timing in new[] { CardTriggeredTiming.Played, CardTriggeredTiming.EffectPlayed })
                data.TriggeredEffects[timing] = new[] { new ConditionalCardEffect { Effect = _Gain() } };
            var observer = new PlayerBuffData
            {
                ID = "play-observer", LifeTimeData = new AlwaysLifeTimePlayerBuffData()
            };
            foreach (var timing in new[] { GameTiming.BeforePlayCardStart, GameTiming.AfterPlayCardStart,
                         GameTiming.BeforePlayCardEnd, GameTiming.AfterPlayCardEnd })
                observer.BuffEffects[timing] = new[] { new ConditionalPlayerBuffEffect { Effect = _Gain() } };
            var built = new GameplayManagerTestBuilder().WithCard(data).WithPlayerBuff(observer).Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff(observer.ID));
            var card = _Card(built, data.ID, enemyOwner ? built.Enemy : built.Ally, zone);

            var result = _Run(built, card);

            // 傷害會附帶雙方的 GeneralUpdateEvent；只應有一次實際效果，且沒有出牌相關事件。
            Assert.That(result.Events.Where(e => e is not GeneralUpdateEvent).Single(), Is.TypeOf<DamageEvent>());
            Assert.That(built.Enemy.Characters.Single().CurrentHealth, Is.EqualTo(98));
            Assert.That((enemyOwner ? (IPlayerEntity)built.Enemy : built.Ally)
                .CardManager.GetCardCollectionZone(zone).Cards, Does.Contain(card));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(0));
            Assert.That(built.Enemy.CurrentEnergy, Is.EqualTo(0));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(built.Enemy.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void AllFourSources_UseCasterAndDeduplicateOnlyWithinOneInvocation(int kind)
        {
            var data = _Attack();
            var cardBuffData = new CardBuffData { ID = "host-buff", LifeTimeData = new AlwaysLifeTimeCardBuffData() };
            var built = new GameplayManagerTestBuilder().WithCard(data).WithCardBuff(cardBuffData).Build();
            var card = _Card(built, data.ID, built.Enemy);
            var hostCard = _Card(built, data.ID, built.Ally);
            _AddBonuses(built.Ally, 7, 0);
            _AddBonuses(built.Enemy, 11, 0);
            var context = _Context(built) with
            {
                Triggered = kind switch
                {
                    0 => new CardTrigger(hostCard),
                    1 => new PlayerBuffTrigger(built.Ally, BuffTestBuilder.CreatePlayerBuff(caster: built.Enemy)),
                    2 => new CharacterBuffTrigger(built.Ally.Characters.Single(), BuffTestBuilder.CreateCharacterBuff(caster: built.Enemy)),
                    _ => new CardBuffTrigger(hostCard, BuffTestBuilder.CreateCardBuff(
                        _Context(built), built.ContextManager.CardBuffLibrary, cardBuffData.ID, built.Enemy))
                }
            };
            var invoke = _Invoke(card, card);
            if (kind == 0)
            {
                var attribute = new CardPlayAttributeEntity();
                attribute.ApplyModify(EffectAttributeAdditionType.NormalDamageAddition, 100);
                context = context with { Action = new CardPlayIntentAction(new CardPlaySource(
                    hostCard, 0, 1, CardPlayReason.Active, Option.None<CardPlayPayment>(), attribute)) };
            }
            EffectQueueItem item = kind switch
            {
                0 => new CardEffectQueueItem(context, invoke),
                1 => new PlayerBuffEffectQueueItem(context, invoke),
                2 => new CharacterBuffEffectQueueItem(context, invoke),
                _ => new CardBuffEffectQueueItem(context, invoke)
            };

            var result = built.Manager.RunEffectBatch(new[] { item, item });

            Assert.That(result.Events.OfType<DamageEvent>().Count(), Is.EqualTo(2));
            Assert.That(built.Enemy.Characters.Single().CurrentHealth, Is.EqualTo(82));
            foreach (var action in result.Actions.OfType<DamageResultAction>())
            {
                var source = (InvokeCardEffectsSource)action.Source;
                Assert.That(source.Card, Is.SameAs(card));
                Assert.That(source.Caster, Is.SameAs(built.Ally));
            }
        }

        [Test]
        public void OpponentAppliedTurnEndBuff_InvokesAsHostAndPreservesOriginalCaster()
        {
            var data = _Attack();
            var buffData = BuffTestBuilder.CreatePlayerBuffData("invoke-at-end", GameTiming.AfterTurnEnd,
                new ConditionalPlayerBuffEffect
                {
                    Effect = new InvokeCardEffects
                    {
                        TargetCards = new CardsOfPlayer { Player = new ReactionOwnerPlayer() }
                    }
                });
            var built = new GameplayManagerTestBuilder().WithCard(data).WithPlayerBuff(buffData)
                .WithPlayerBuff(new PlayerBuffData { ID = "bonuses", LifeTimeData = new AlwaysLifeTimePlayerBuffData() }).Build();
            _Card(built, data.ID, built.Enemy);
            var buff = BuffTestBuilder.CreatePlayerBuff(buffData.ID, built.Ally);
            built.Enemy.BuffManager.AddBuff(buff);
            _AddBonuses(built.Ally, 7, 0);
            _AddBonuses(built.Enemy, 11, 0);

            var events = built.Manager.TriggerTiming(GameTiming.AfterTurnEnd, SystemSource.Instance).ToArray();

            Assert.That(events.OfType<DamageEvent>().Single().Faction, Is.EqualTo(Faction.Ally));
            Assert.That(built.Ally.Characters.Single().CurrentHealth, Is.EqualTo(87));
            Assert.That(buff.Caster.ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
        }

        [Test]
        public void NestedInvocations_KeepOrderCasterOriginAndRestoreEachSelection()
        {
            var a = _Attack("A");
            var b = _Attack("B");
            var c = _Attack("C");
            var d = _Attack("D");
            b.MainSelect.CandidateScope = TargetCandidateScope.ToAlly;
            var built = new GameplayManagerTestBuilder().WithCard(a).WithCard(b).WithCard(c).WithCard(d).Build();
            var cards = new[] { a, b, c, d }.Select(data => _Card(built, data.ID, built.Enemy)).ToArray();
            var seen = new List<string>();
            var root = _Context(built);
            ICardEffect Mark(string name, ICharacterEntity target) => new DamageEffect
            {
                Targets = new SingleCharacterCollection { Target = new SelectedCharacter() },
                Value = new ObservedValue(context =>
                {
                    seen.Add(name);
                    Assert.That(context.Model.ContextManager.Context.SelectedCharacter, Is.EqualTo(target.Identity));
                    Assert.That(context.ReactionOriginAction, Is.SameAs(root.Action));
                    Assert.That(ReactionContextQuery.Caster(context).ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
                    Assert.That(new ActionCard().Eval(context), Is.EqualTo(new TriggeredCard().Eval(context)));
                    return 1;
                })
            };
            a.Effects.Clear();
            a.Effects.Add(_Invoke(cards[1], cards[2]));
            a.Effects.Add(Mark("A", built.Enemy.Characters.Single()));
            b.Effects.Clear();
            b.Effects.Add(Mark("B1", built.Ally.Characters.Single()));
            b.Effects.Add(_Invoke(cards[3]));
            b.Effects.Add(Mark("B2", built.Ally.Characters.Single()));
            c.Effects.Clear();
            c.Effects.Add(Mark("C", built.Enemy.Characters.Single()));
            d.Effects.Clear();
            d.Effects.Add(Mark("D", built.Enemy.Characters.Single()));
            var outer = GameContext.EMPTY with { SelectedCard = cards[2].Identity };

            using (built.ContextManager.SetContext(outer))
            {
                var result = built.Manager.RunEffectBatch(new[] { new CardEffectQueueItem(root, _Invoke(cards[0])) });
                Assert.That(result.Events.OfType<DamageEvent>().Count(), Is.EqualTo(5));
                Assert.That(built.ContextManager.Context, Is.EqualTo(outer));
            }
            Assert.That(seen, Is.EqualTo(new[] { "B1", "D", "B2", "C", "A" }));
        }

        [Test]
        public void SubSelection_UsesCasterCandidatesAndPowerIncludingCardBuff()
        {
            var data = CardTestBuilder.CreateCardData();
            var cardBuff = new CardBuffData
            {
                ID = "power", LifeTimeData = new AlwaysLifeTimeCardBuffData(),
                PropertyDatas = new List<ICardBuffPropertyData> { new PowerCardBuffPropertyData { Value = new ConstInteger { Value = 1 } } }
            };
            data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "discard", CardCandidates = new CardsOfPlayer { Player = new ReactionCasterPlayer() },
                SelectCount = new ObservedValue(context => GameFormula.CardPower(context, ((CardTrigger)context.Triggered).Card).ValueOr(-1)),
                IsMustSelect = new TrueValue()
            });
            data.Effects.Add(new DiscardCardEffect { TargetCards = new SubSelectedCardCollection { SelectionId = "discard" } });
            var built = new GameplayManagerTestBuilder().WithCard(data).WithCardBuff(cardBuff).Build();
            var invoked = CardTestBuilder.CreateCardWithBuff(_Context(built), built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary, data.ID, cardBuff.ID);
            built.Enemy.CardManager.HandCard.AddCard(invoked);
            for (var i = 0; i < 3; i++) _Card(built, data.ID, built.Ally);
            _AddBonuses(built.Ally, 0, 1);
            _AddBonuses(built.Enemy, 0, 10);

            _Run(built, invoked);

            Assert.That(built.Ally.CardManager.Graveyard.Cards.Count, Is.EqualTo(2));
            Assert.That(built.Ally.CardManager.HandCard.Cards.Count, Is.EqualTo(1));
            Assert.That(built.Enemy.CardManager.HandCard.Cards, Is.EqualTo(new[] { invoked }));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void InvokedEffects_CreateBuffWithInvokeCasterButExplicitOwnerStillMeansCardOwner()
        {
            var data = CardTestBuilder.CreateCardData();
            var buffData = new PlayerBuffData { ID = "created", LifeTimeData = new AlwaysLifeTimePlayerBuffData() };
            data.Effects.Add(new AddPlayerBuffEffect
            {
                BuffId = buffData.ID, Level = new ConstInteger { Value = 1 },
                Targets = new SinglePlayerCollection { Target = new CardOwner { Card = new ActionCard() } }
            });
            var built = new GameplayManagerTestBuilder().WithCard(data).WithPlayerBuff(buffData).Build();
            var card = _Card(built, data.ID, built.Enemy);

            _Run(built, card);

            Assert.That(built.Enemy.BuffManager.Buffs.Single().Caster.ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
            Assert.That(built.Ally.BuffManager.Buffs, Is.Empty);
        }

        [Test]
        public void EarlierInvocationMovesLaterCard_StaleRequestIsSkipped()
        {
            var first = CardTestBuilder.CreateCardData("first");
            var later = _Attack("later");
            var built = new GameplayManagerTestBuilder().WithCard(first).WithCard(later).Build();
            var a = _Card(built, first.ID, built.Ally);
            var b = _Card(built, later.ID, built.Enemy);
            first.Effects.Add(new DiscardCardEffect { TargetCards = new FixedCards(b) });

            var result = _Run(built, a, b);

            Assert.That(result.Events.OfType<DamageEvent>(), Is.Empty);
            Assert.That(built.Enemy.CardManager.Graveyard.Cards, Does.Contain(b));
        }

        [Test]
        public void EarlierInvocationTransfersLaterCard_OriginalOwnerRequestIsSkipped()
        {
            var first = CardTestBuilder.CreateCardData("first");
            var later = _Attack("later");
            var built = new GameplayManagerTestBuilder().WithCard(first).WithCard(later).Build();
            var a = _Card(built, first.ID, built.Ally);
            var b = _Card(built, later.ID, built.Enemy);
            var transfer = _Gain();
            transfer.Value = new ObservedValue(_ =>
            {
                built.Enemy.CardManager.HandCard.RemoveCard(b);
                built.Ally.CardManager.HandCard.AddCard(b);
                return 1;
            });
            first.Effects.Add(transfer);

            Assert.That(_Run(built, a, b).Events.OfType<DamageEvent>(), Is.Empty);
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(b));
        }

        [Test]
        public void CardWithoutMainSelection_DoesNotBorrowOuterTarget()
        {
            var outer = _Attack("outer");
            var inner = CardTestBuilder.CreateCardData("inner");
            inner.Effects.Add(_Damage(50));
            inner.Effects.Add(_Gain());
            var built = new GameplayManagerTestBuilder().WithCard(outer).WithCard(inner).Build();
            var a = _Card(built, outer.ID, built.Ally);
            var b = _Card(built, inner.ID, built.Ally);
            outer.Effects.Insert(0, _Invoke(b));

            var result = _Run(built, a);

            Assert.That(result.Events.OfType<DamageEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Enemy.Characters.Single().CurrentHealth, Is.EqualTo(98));
        }

        [Test]
        public void EarlierInvocationTransformsLaterCard_UsesNewSelectionAndEffects()
        {
            var first = CardTestBuilder.CreateCardData("first");
            var later = _Attack("later");
            var replacement = new OverrideCardData
            {
                ID = "replacement", MainSelect = new MainTargetSelectLogic
                {
                    MainSelectable = new CharacterSelectable(), CandidateScope = TargetCandidateScope.ToAlly,
                    AutomaticSelection = AutomaticTargetSelectionStrategy.First
                },
                Effects = new List<ICardEffect> { _Damage(5) }
            };
            var built = new GameplayManagerTestBuilder().WithCard(first).WithCard(later).WithCard(replacement).Build();
            var a = _Card(built, first.ID, built.Ally);
            var b = _Card(built, later.ID, built.Enemy);
            first.Effects.Add(new ApplyCardFormOverrideEffect
            {
                TargetCards = new FixedCards(b), TargetCardDataId = replacement.ID, OverrideKey = "before-invoke"
            });

            var result = _Run(built, a, b);

            Assert.That(result.Events.OfType<DamageEvent>().Single().Faction, Is.EqualTo(Faction.Ally));
            Assert.That(built.Ally.Characters.Single().CurrentHealth, Is.EqualTo(95));
            Assert.That(b.CardDataId, Is.EqualTo(replacement.ID));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingMainTargetOrUnsupportedSubSelection_SkipsWholeInvocation(bool unsupportedSubSelection)
        {
            var data = _Attack();
            data.Effects.Insert(0, _Gain());
            if (unsupportedSubSelection)
                data.SubSelects.Add(new NewCardSelectionGroup { Id = "unsupported" });
            var builder = new GameplayManagerTestBuilder().WithCard(data);
            if (!unsupportedSubSelection) builder.WithEnemyCharacters();
            var built = builder.Build();
            var card = _Card(built, data.ID, built.Ally);

            Assert.That(_Run(built, card).Events, Is.Empty);
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [TestCase(CardCollectionType.ExclusionZone)]
        [TestCase(CardCollectionType.DisposeZone)]
        public void UnsupportedZone_IsSkipped(CardCollectionType zone)
        {
            var data = _Attack();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            Assert.That(_Run(built, _Card(built, data.ID, built.Ally, zone)).Events, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RecursiveInvocation_StopsAtSharedBudgetAndNextRootWorks(bool mutual)
        {
            var a = _Attack("A");
            var b = _Attack("B");
            var built = new GameplayManagerTestBuilder().WithCard(a).WithCard(b).Build();
            var first = _Card(built, a.ID, built.Ally);
            var second = _Card(built, b.ID, built.Enemy);
            a.Effects.Add(_Invoke(mutual ? second : first));
            b.Effects.Add(_Invoke(first));
            built.Manager.CardPlayChainBudget = 35;
            LogAssert.Expect(LogType.Error, new Regex("執行預算已耗盡"));

            var result = _Run(built, first);

            var damageCount = result.Events.OfType<DamageEvent>().Count();
            Assert.That(damageCount, Is.GreaterThan(0));
            Assert.That(built.Enemy.Characters.Single().CurrentHealth, Is.EqualTo(100 - 2 * damageCount));
            Assert.That(result.Actions.OfType<DamageResultAction>().Count(), Is.EqualTo(damageCount));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
            Assert.That(built.Manager.PopAllEvents(), Is.Empty);
            a.Effects.RemoveAt(a.Effects.Count - 1);
            Assert.That(_Run(built, first).Events.OfType<DamageEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void InvokeAndPlayCard_MaintainImmediateEffectsThenFifoFullPlay()
        {
            var invoked = _Attack("invoke");
            var played = _Attack("play");
            var built = new GameplayManagerTestBuilder().WithCard(invoked).WithCard(played).Build();
            var a = _Card(built, invoked.ID, built.Ally);
            var b = _Card(built, played.ID, built.Ally);
            var order = new List<string>();
            ((DamageEffect)invoked.Effects[0]).Value = new ObservedValue(context =>
            {
                order.Add("invoke");
                Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
                Assert.That(context.Action.Source, Is.TypeOf<InvokeCardEffectsSource>());
                return 2;
            });
            ((DamageEffect)played.Effects[0]).Value = new ObservedValue(context =>
            {
                order.Add("play");
                Assert.That(built.Ally.CardManager.PlayingCard.ValueOr((ICardEntity)null), Is.SameAs(b));
                Assert.That(context.Action.Source, Is.TypeOf<CardPlaySource>());
                return 2;
            });
            invoked.Effects.Insert(0, new PlayCardEffect { TargetCards = new FixedCards(b) });

            var result = _Run(built, a);

            Assert.That(result.Events.Where(e => e is DamageEvent or UsedCardEvent).Select(e => e.GetType()),
                Is.EqualTo(new[] { typeof(DamageEvent), typeof(DamageEvent), typeof(UsedCardEvent) }));
            Assert.That(order, Is.EqualTo(new[] { "invoke", "play" }));
            Assert.That(result.Events.OfType<UsedCardEvent>().Single().UsedCardIdentity, Is.EqualTo(b.Identity));
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(a));
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Does.Contain(b));
        }

        [Test]
        public void AlternatingInvokeAndFullPlay_ShareBudgetAcrossRunners()
        {
            var invokeData = _Attack("invoke");
            var playData = _Attack("play");
            playData.PropertyDatas.Add(new RecyclePropertyData());
            var built = new GameplayManagerTestBuilder().WithCard(invokeData).WithCard(playData).Build();
            var a = _Card(built, invokeData.ID, built.Ally);
            var b = _Card(built, playData.ID, built.Ally);
            invokeData.Effects.Add(new PlayCardEffect { TargetCards = new FixedCards(b) });
            playData.Effects.Add(_Invoke(a));
            built.Manager.CardPlayChainBudget = 80;
            LogAssert.Expect(LogType.Error, new Regex("執行預算已耗盡"));

            var result = _Run(built, a);

            Assert.That(result.Events.OfType<UsedCardEvent>().Any(), Is.True);
            Assert.That(built.Enemy.Characters.Single().CurrentHealth,
                Is.EqualTo(100 - 2 * result.Events.OfType<DamageEvent>().Count()));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
            invokeData.Effects.RemoveAt(1);
            Assert.That(_Run(built, a).Events.OfType<DamageEvent>().Count(), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExceptionOrCancellation_RestoresContextAndKeepsCommittedEventOnce(bool cancel)
        {
            using var cancellation = new CancellationTokenSource();
            var data = _Attack();
            data.Effects.Add(new DamageEffect
            {
                Targets = new SingleCharacterCollection { Target = new SelectedCharacter() },
                Value = new ObservedValue(_ =>
                {
                    if (!cancel) throw new InvalidOperationException("測試用例外");
                    cancellation.Cancel();
                    return 2;
                })
            });
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = _Card(built, data.ID, built.Enemy);
            var events = new List<IGameEvent>();
            var runner = new EffectQueueRunner(built.ContextManager, new EffectQueueExecutionScope(100),
                emitted => events.AddRange(emitted), cancellation.Token);
            runner.Enqueue(new CardEffectQueueItem(_Context(built), _Invoke(card)));
            var outer = GameContext.EMPTY with { SelectedCard = card.Identity };
            using (built.ContextManager.SetContext(outer))
            {
                if (cancel) Assert.Throws<OperationCanceledException>(() => runner.RunToCompletion());
                else Assert.Throws<InvalidOperationException>(() => runner.RunToCompletion());
                Assert.That(built.ContextManager.Context, Is.EqualTo(outer));
            }
            Assert.That(events.OfType<DamageEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Enemy.Characters.Single().CurrentHealth, Is.EqualTo(98));
            data.Effects.RemoveAt(1);
            Assert.That(_Run(built, card).Events.OfType<DamageEvent>().Count(), Is.EqualTo(1));
        }

        private static StandardCardData _Attack(string id = CardTestBuilder.CardId)
        {
            var data = CardTestBuilder.CreateCardData(id);
            data.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CharacterSelectable(), CandidateScope = TargetCandidateScope.ToEnemy,
                AutomaticSelection = AutomaticTargetSelectionStrategy.First
            };
            data.Effects.Add(_Damage(2));
            return data;
        }

        private static DamageEffect _Damage(int point) => new()
        {
            Targets = new SingleCharacterCollection { Target = new SelectedCharacter() },
            Value = new ConstInteger { Value = point }
        };

        private static GainEnergyEffect _Gain() => new()
        {
            Targets = new SinglePlayerCollection { Target = new PlayerByFaction { Faction = Faction.Ally } },
            Value = new ConstInteger { Value = 1 }
        };

        private static ICardEntity _Card(BuiltGameplay built, string id, IPlayerEntity owner,
            CardCollectionType zone = CardCollectionType.HandCard)
        {
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, id);
            owner.CardManager.GetCardCollectionZone(zone).AddCard(card);
            return card;
        }

        private static void _AddBonuses(IPlayerEntity player, int damage, int power)
            => player.BuffManager.AddBuff(new PlayerBuffEntity("bonuses", Guid.NewGuid(), 1, 1, player.Some(),
                new IPlayerBuffPropertyEntity[]
                {
                    new NormalDamageAdditionPlayerBuffPropertyEntity(new ConstInteger { Value = damage }),
                    new AllCardPowerPlayerBuffPropertyEntity(new ConstInteger { Value = power })
                }, new AlwaysLifeTimePlayerBuffEntity(), new Dictionary<string, IReactionSessionEntity>()));

        private static TriggerContext _Context(BuiltGameplay built)
            => new(built.Manager, new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));

        private static InvokeCardEffects _Invoke(params ICardEntity[] cards) => new() { TargetCards = new FixedCards(cards) };

        private static EffectResult _Run(BuiltGameplay built, params ICardEntity[] cards)
            => built.Manager.RunEffectBatch(new[] { new CardEffectQueueItem(_Context(built), _Invoke(cards)) });

        private sealed class FixedCards : ITargetCardCollectionValue
        {
            private readonly ICardEntity[] _cards;
            public FixedCards(params ICardEntity[] cards) => _cards = cards;
            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext context) => _cards;
        }

        private sealed class ObservedValue : IIntegerValue
        {
            private readonly Func<TriggerContext, int> _read;
            public ObservedValue(Func<TriggerContext, int> read) => _read = read;
            public Option<int> Eval(TriggerContext context) => _read(context).Some();
        }
    }
}
