using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public sealed class CardPlayFlowCharacterizationTests
    {
        [Test]
        public void UseCard_PlayedExecutesCardDataBeforeCardBuffThenLeavesPlayingCard()
        {
            const string cardId = "card-play-card-data-before-card-buff";
            const string sourceCardBuffId = "card-play-source-card-buff";
            const string addedCardBuffId = "card-play-added-card-buff";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.TriggeredEffects[CardTriggeredTiming.Played] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 1 }
                    }
                }
            };
            var sourceCardBuffData = new CardBuffData
            {
                ID = sourceCardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData(),
                Effects = new Dictionary<CardTriggeredTiming, ConditionalCardBuffEffect[]>
                {
                    [CardTriggeredTiming.Played] = new[]
                    {
                        new ConditionalCardBuffEffect
                        {
                            Conditions = { new ConstCondition { Value = true } },
                            Effect = new AddCardBuffEffect
                            {
                                TargetCards = new SingleCardCollection
                                {
                                    TargetCard = new PlayingCardOfPlayer
                                    {
                                        Player = new CurrentPlayer()
                                    }
                                },
                                AddCardBuffDatas = new List<AddCardBuffData>
                                {
                                    new()
                                    {
                                        CardBuffId = addedCardBuffId,
                                        Level = new ConstInteger { Value = 1 }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var addedCardBuffData = new CardBuffData
            {
                ID = addedCardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(sourceCardBuffData)
                .WithCardBuff(addedCardBuffData)
                .Build();
            var setupContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new CardLookIntentAction(CardEntity.DummyCard));
            var card = CardTestBuilder.CreateCardWithBuff(
                setupContext,
                built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary,
                cardId,
                sourceCardBuffId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents().ToArray();
            Assert.That(
                events
                    .Where(gameEvent => gameEvent is UsedCardEvent or GainEnergyEvent or AddCardBuffEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(UsedCardEvent),
                    typeof(GainEnergyEvent),
                    typeof(AddCardBuffEvent)
                }));
            Assert.That(
                card.BuffManager.Buffs.Select(buff => buff.CardBuffDataID),
                Does.Contain(addedCardBuffId));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(
                built.Ally.CardManager.Graveyard.Cards.Any(item => item.Identity == card.Identity),
                Is.True);
        }

        [TestCase(Faction.Ally)]
        [TestCase(Faction.Enemy)]
        public void UseCard_ActivePlayerDispatchesPlayedOnceAndDoesNotDispatchEffectPlayed(
            Faction faction)
        {
            const string cardId = "card-play-active-player-timing";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.PropertyDatas.Add(new EffectRepeatPropertyData { Value = 3 });
            cardData.TriggeredEffects[CardTriggeredTiming.Played] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 1 }
                    }
                }
            };
            cardData.TriggeredEffects[CardTriggeredTiming.EffectPlayed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 2 }
                    }
                }
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            IPlayerEntity player = faction == Faction.Ally
                ? built.Ally
                : built.Enemy;
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            player.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(player))
            {
                _InvokeUseCard(built.Manager, player, card.Identity);
            }

            var events = built.Manager.PopAllEvents();
            Assert.That(player.CurrentEnergy, Is.EqualTo(1));
            Assert.That(events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UseCard_PlayedRunsOnceAfterNormalEffectsWhileCardIsPlaying()
        {
            const string cardId = "card-play-played-timing";
            const string cardBuffId = "card-play-played-timing-buff";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.PropertyDatas.Add(new EffectRepeatPropertyData { Value = 2 });
            cardData.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            cardData.TriggeredEffects[CardTriggeredTiming.Played] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new AddCardBuffEffect
                    {
                        TargetCards = new SingleCardCollection
                        {
                            TargetCard = new PlayingCardOfPlayer
                            {
                                Player = new CurrentPlayer()
                            }
                        },
                        AddCardBuffDatas = new List<AddCardBuffData>
                        {
                            new()
                            {
                                CardBuffId = cardBuffId,
                                Level = new ConstInteger { Value = 1 }
                            }
                        }
                    }
                }
            };
            var cardBuffData = new CardBuffData
            {
                ID = cardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents().ToArray();
            Assert.That(
                events
                    .Where(gameEvent => gameEvent is
                        LoseEnergyEvent or
                        GainEnergyEvent or
                        UsedCardEvent or
                        AddCardBuffEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(LoseEnergyEvent),
                    typeof(GainEnergyEvent),
                    typeof(GainEnergyEvent),
                    typeof(UsedCardEvent),
                    typeof(AddCardBuffEvent)
                }));
            Assert.That(
                card.BuffManager.Buffs.Count(buff => buff.CardBuffDataID == cardBuffId),
                Is.EqualTo(1));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(
                built.Ally.CardManager.Graveyard.Cards.Any(item => item.Identity == card.Identity),
                Is.True);
        }

        [Test]
        public void UseCard_PlayedResultIsIncludedInCardPlayResultSource()
        {
            const string cardId = "card-play-played-result";
            const string cardBuffId = "card-play-played-result-card-buff";
            const string playerBuffId = "card-play-played-result-observer";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.TriggeredEffects[CardTriggeredTiming.Played] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new AddCardBuffEffect
                    {
                        TargetCards = new SingleCardCollection
                        {
                            TargetCard = new PlayingCardOfPlayer
                            {
                                Player = new CurrentPlayer()
                            }
                        },
                        AddCardBuffDatas = new List<AddCardBuffData>
                        {
                            new()
                            {
                                CardBuffId = cardBuffId,
                                Level = new ConstInteger { Value = 1 }
                            }
                        }
                    }
                }
            };
            var cardBuffData = new CardBuffData
            {
                ID = cardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var observerBuffData = BuffTestBuilder.CreatePlayerBuffData(
                playerBuffId,
                GameTiming.BeforePlayCardEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions =
                    {
                        new CardPlayResultCondition
                        {
                            Conditions = { new ContainsAddCardBuffResultCondition() }
                        }
                    },
                    Effect = new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 1 }
                    }
                });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .WithPlayerBuff(observerBuffData)
                .Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff(playerBuffId));
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents().ToArray();
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(
                events
                    .Where(gameEvent => gameEvent is UsedCardEvent or AddCardBuffEvent or GainEnergyEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(UsedCardEvent),
                    typeof(AddCardBuffEvent),
                    typeof(GainEnergyEvent)
                }));
        }

        [Test]
        public void UseCard_EffectRepeatProperty_RepeatsNormalEffectsButEmitsOneUsedEvent()
        {
            const string cardId = "card-play-effect-repeat";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.PropertyDatas.Add(new EffectRepeatPropertyData { Value = 2 });
            cardData.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents();
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(2));
            Assert.That(events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UseCard_EffectRepeatCardBuffProperty_RepeatsNormalEffects()
        {
            const string cardId = "card-play-buff-effect-repeat";
            const string cardBuffId = "card-play-effect-repeat-buff";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            var cardBuffData = new CardBuffData
            {
                ID = cardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData(),
                PropertyDatas = new List<ICardBuffPropertyData>
                {
                    new EffectRepeatCardBuffPropertyData
                    {
                        Value = new ConstInteger { Value = 2 }
                    }
                }
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .Build();
            var setupContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new CardLookIntentAction(CardEntity.DummyCard));
            var card = CardTestBuilder.CreateCardWithBuff(
                setupContext,
                built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary,
                cardId,
                cardBuffId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents();
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(2));
            Assert.That(events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UseCard_NormalEffectRunsWhileCardIsPlayingThenUsedEventPrecedesGraveyardExit()
        {
            const string cardId = "card-play-flow";
            const string cardBuffId = "card-play-flow-buff";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            cardData.Effects.Add(new AddCardBuffEffect
            {
                TargetCards = new SingleCardCollection
                {
                    TargetCard = new PlayingCardOfPlayer
                    {
                        Player = new CurrentPlayer()
                    }
                },
                AddCardBuffDatas = new List<AddCardBuffData>
                {
                    new()
                    {
                        CardBuffId = cardBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                }
            });
            var cardBuffData = new CardBuffData
            {
                ID = cardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents().ToArray();
            Assert.That(
                events
                    .Where(gameEvent => gameEvent is
                        LoseEnergyEvent or
                        GainEnergyEvent or
                        AddCardBuffEvent or
                        UsedCardEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(LoseEnergyEvent),
                    typeof(GainEnergyEvent),
                    typeof(AddCardBuffEvent),
                    typeof(UsedCardEvent)
                }));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(
                card.BuffManager.Buffs.Select(buff => buff.CardBuffDataID),
                Does.Contain(cardBuffId));
            Assert.That(
                built.Ally.CardManager.HandCard.Cards.Any(item => item.Identity == card.Identity),
                Is.False);
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(
                built.Ally.CardManager.Graveyard.Cards.Any(item => item.Identity == card.Identity),
                Is.True);
        }

        [Test]
        public void UseCard_DisposePropertyMovesCardToExclusionZoneAfterUsedEvent()
        {
            const string cardId = "card-play-dispose-flow";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.PropertyDatas.Add(new DisposePropertyData());
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            Assert.That(
                built.Manager.PopAllEvents()
                    .Where(gameEvent => gameEvent is LoseEnergyEvent or UsedCardEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(LoseEnergyEvent),
                    typeof(UsedCardEvent)
                }));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(
                built.Ally.CardManager.Graveyard.Cards.Any(item => item.Identity == card.Identity),
                Is.False);
            Assert.That(
                built.Ally.CardManager.ExclusionZone.Cards.Any(item => item.Identity == card.Identity),
                Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CardFormula_UsesOwnerBuffWithDifferentOrMissingCurrentPlayer(bool hasCurrentPlayer)
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(card);
            built.Enemy.BuffManager.AddBuff(new PlayerBuffEntity(
                "owner-addition", System.Guid.NewGuid(), 1, 1, Option.None<IPlayerEntity>(),
                new IPlayerBuffPropertyEntity[]
                {
                    new NormalDamageAdditionPlayerBuffPropertyEntity(new ConstInteger { Value = 7 }),
                    new NormalDamageRatioPlayerBuffPropertyEntity(0.5f)
                },
                new AlwaysLifeTimePlayerBuffEntity(), new Dictionary<string, IReactionSessionEntity>()));
            var source = new CardPlaySource(card, 0, 1, CardPlayReason.Active,
                Option.None<CardPlayPayment>(), new CardPlayAttributeEntity());
            var context = new TriggerContext(built.Manager, new CardPlayTrigger(source), new CardPlayIntentAction(source));
            using var currentPlayerScope = hasCurrentPlayer ? built.Status.SetCurrentPlayer(built.Ally) : null;
            var before = built.Status.CurrentPlayer.Value;

            Assert.That(GameFormula.NormalDamagePoint(context, 3).ValueOr(-1), Is.EqualTo(10));
            var ratio = typeof(GameFormula).GetMethod("_GetAttributeRatio", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { context, EffectAttributeRatioType.NormalDamageRatio, PlayerBuffProperty.NormalDamageRatio });
            Assert.That(ratio, Is.EqualTo(0.5f));
            Assert.That(built.Status.CurrentPlayer.Value, Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UseCard_InvalidOrUnaffordableCardDoesNotPayOrNotify(bool sealedCard)
        {
            var data = CardTestBuilder.CreateCardData();
            data.Cost = sealedCard ? 0 : 1;
            if (sealedCard) data.PropertyDatas.Add(new SealedPropertyData());
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);
            var notified = false;
            built.Manager.OnUseCard += () => notified = true;

            _InvokeUseCard(built.Manager, built.Ally, card.Identity);

            Assert.That(built.Manager.PopAllEvents(), Is.Empty);
            Assert.That(built.Ally.CurrentEnergy, Is.Zero);
            Assert.That(notified, Is.False);
            Assert.That(built.Ally.CardManager.HandCard.Cards, Does.Contain(card));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void UseCard_ClearsInheritedSelectionAndRestoresItAfterCompletion()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);
            var outer = GameContext.EMPTY with { SelectedCard = System.Guid.NewGuid() };
            GameContext observed = null;
            built.Manager.OnUseCard += () => observed = built.ContextManager.Context;
            using (built.ContextManager.SetContext(outer))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
                Assert.That(built.ContextManager.Context, Is.EqualTo(outer));
            }
            Assert.That(observed, Is.EqualTo(GameContext.EMPTY));
            Assert.That(built.Manager.PopAllEvents().OfType<UsedCardEvent>().Single().Reason,
                Is.EqualTo(CardPlayReason.Active));
        }

        private static void _InitializeEventBuffer(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetField("_gameEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, new List<IGameEvent>());
        }

        private static void _InvokeUseCard(
            GameplayManager manager,
            IPlayerEntity player,
            System.Guid cardIdentity)
        {
            typeof(GameplayManager)
                .GetMethod("_UseCard", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(manager, new object[] { player, new UseCardAction(
                    cardIdentity, MainSelectionAction.Empty, new Dictionary<string, ISubSelectionAction>()) });
        }

        private sealed class ContainsAddCardBuffResultCondition : ICardPlayResultValueCondition
        {
            public bool Eval(
                TriggerContext triggerContext,
                CardPlayResultSource cardPlayResultSource)
            {
                return cardPlayResultSource.EffectResults.Any(
                    effectResult => effectResult is AddCardBuffResultAction);
            }
        }
    }
}
