using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class HandClearFlowCharacterizationTests
    {
        [Test]
        public void ClearHandOnTurnEnd_PreservesCardsAndMovesOtherCardsByCurrentProperties()
        {
            const string normalCardId = "hand-clear-normal";
            const string autoDisposeCardId = "hand-clear-auto-dispose";
            const string preservedCardId = "hand-clear-preserved";
            const string preservedAutoDisposeCardId = "hand-clear-preserved-auto-dispose";
            var normalCardData = CardTestBuilder.CreateCardData(normalCardId);
            var autoDisposeCardData = CardTestBuilder.CreateCardData(autoDisposeCardId);
            autoDisposeCardData.PropertyDatas.Add(new AutoDisposePropertyData());
            var preservedCardData = CardTestBuilder.CreateCardData(preservedCardId);
            preservedCardData.PropertyDatas.Add(new PreservedPropertyData());
            var preservedAutoDisposeCardData = CardTestBuilder.CreateCardData(preservedAutoDisposeCardId);
            preservedAutoDisposeCardData.PropertyDatas.Add(new PreservedPropertyData());
            preservedAutoDisposeCardData.PropertyDatas.Add(new AutoDisposePropertyData());
            var built = new GameplayManagerTestBuilder()
                .WithCard(normalCardData)
                .WithCard(autoDisposeCardData)
                .WithCard(preservedCardData)
                .WithCard(preservedAutoDisposeCardData)
                .Build();
            var normalCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                normalCardId);
            var autoDisposeCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                autoDisposeCardId);
            var preservedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                preservedCardId);
            var preservedAutoDisposeCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                preservedAutoDisposeCardId);
            built.Ally.CardManager.HandCard.AddCards(new[]
            {
                normalCard,
                preservedCard,
                autoDisposeCard,
                preservedAutoDisposeCard
            });

            var result = built.Ally.CardManager.ClearHandOnTurnEnd(built.Manager);
            var gameEvent = result.Events
                .OfType<DiscardHandCardEvent>()
                .Single();

            Assert.That(
                result.Cards,
                Is.EqualTo(new[]
                {
                    new HandClearCardResult(
                        normalCard,
                        CardCollectionType.Graveyard,
                        CardTriggeredTiming.Discarded),
                    new HandClearCardResult(
                        preservedCard,
                        CardCollectionType.HandCard,
                        CardTriggeredTiming.Preserved),
                    new HandClearCardResult(
                        autoDisposeCard,
                        CardCollectionType.ExclusionZone,
                        CardTriggeredTiming.Discarded),
                    new HandClearCardResult(
                        preservedAutoDisposeCard,
                        CardCollectionType.HandCard,
                        CardTriggeredTiming.Preserved)
                }));
            Assert.That(
                built.Ally.CardManager.HandCard.Cards,
                Is.EqualTo(new[] { preservedCard, preservedAutoDisposeCard }));
            Assert.That(
                built.Ally.CardManager.Graveyard.Cards,
                Is.EqualTo(new[] { normalCard }));
            Assert.That(
                built.Ally.CardManager.ExclusionZone.Cards,
                Is.EqualTo(new[] { autoDisposeCard }));
            Assert.That(gameEvent.Faction, Is.EqualTo(Faction.Ally));
            Assert.That(
                gameEvent.DiscardedCardIdentities,
                Is.EqualTo(new[] { normalCard.Identity }));
            Assert.That(
                gameEvent.ExcludedCardIdentities,
                Is.EqualTo(new[] { autoDisposeCard.Identity }));
        }

        [Test]
        public void TurnEnd_ClearsAllyHandBeforeEnemyHand()
        {
            const string allyCardId = "turn-end-ally-hand-card";
            const string enemyCardId = "turn-end-enemy-hand-card";
            var built = new GameplayManagerTestBuilder()
                .WithCard(CardTestBuilder.CreateCardData(allyCardId))
                .WithCard(CardTestBuilder.CreateCardData(enemyCardId))
                .Build();
            var allyCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                allyCardId);
            var enemyCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                enemyCardId);
            built.Ally.CardManager.HandCard.AddCard(allyCard);
            built.Enemy.CardManager.HandCard.AddCard(enemyCard);
            _InitializeEventBuffer(built.Manager);

            typeof(GameplayManager)
                .GetMethod("_TurnEnd", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(built.Manager, null);

            Assert.That(
                built.Manager.PopAllEvents()
                    .OfType<DiscardHandCardEvent>()
                    .Select(gameEvent => gameEvent.Faction),
                Is.EqualTo(new[] { Faction.Ally, Faction.Enemy }));
            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.Empty);
            Assert.That(built.Enemy.CardManager.HandCard.Cards, Is.Empty);
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Is.EqualTo(new[] { allyCard }));
            Assert.That(built.Enemy.CardManager.Graveyard.Cards, Is.EqualTo(new[] { enemyCard }));
        }

        [Test]
        public void TurnEnd_RunsEachPlayersPreservedEffectsAfterTheirDiscardEvent()
        {
            const string allyCardId = "turn-end-ally-preserved-card";
            const string enemyCardId = "turn-end-enemy-preserved-card";
            var allyCardData = _CreatePreservedEnergyCardData(allyCardId);
            var enemyCardData = _CreatePreservedEnergyCardData(enemyCardId);
            var built = new GameplayManagerTestBuilder()
                .WithCard(allyCardData)
                .WithCard(enemyCardData)
                .Build();
            var allyCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                allyCardId);
            var enemyCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                enemyCardId);
            built.Ally.CardManager.HandCard.AddCard(allyCard);
            built.Enemy.CardManager.HandCard.AddCard(enemyCard);
            _InitializeEventBuffer(built.Manager);

            typeof(GameplayManager)
                .GetMethod("_TurnEnd", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(built.Manager, null);

            var lifecycleEvents = built.Manager.PopAllEvents()
                .Where(gameEvent => gameEvent is DiscardHandCardEvent or GainEnergyEvent)
                .ToArray();

            Assert.That(
                lifecycleEvents.Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DiscardHandCardEvent),
                    typeof(GainEnergyEvent),
                    typeof(DiscardHandCardEvent),
                    typeof(GainEnergyEvent)
                }));
            Assert.That(
                lifecycleEvents.Select(_GetFaction),
                Is.EqualTo(new[]
                {
                    Faction.Ally,
                    Faction.Ally,
                    Faction.Enemy,
                    Faction.Enemy
                }));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(built.Enemy.CurrentEnergy, Is.EqualTo(1));
            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(new[] { allyCard }));
            Assert.That(built.Enemy.CardManager.HandCard.Cards, Is.EqualTo(new[] { enemyCard }));
        }

        [Test]
        public void TurnEnd_RunsPreservedBeforeDiscardedAndKeepsOriginalOrderWithinEachTiming()
        {
            const string normalCardId = "turn-end-discarded-normal";
            const string preservedCardId = "turn-end-discarded-preserved";
            const string autoDisposeCardId = "turn-end-discarded-auto-dispose";
            var normalCardData = CardTestBuilder.CreateCardData(normalCardId);
            normalCardData.TriggeredEffects[CardTriggeredTiming.Discarded] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _GainEnergy(1)
                }
            };
            var preservedCardData = _CreatePreservedEnergyCardData(preservedCardId, 4);
            preservedCardData.TriggeredEffects[CardTriggeredTiming.Discarded] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _GainEnergy(100)
                }
            };
            var autoDisposeCardData = CardTestBuilder.CreateCardData(autoDisposeCardId);
            autoDisposeCardData.PropertyDatas.Add(new AutoDisposePropertyData());
            autoDisposeCardData.TriggeredEffects[CardTriggeredTiming.Discarded] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _GainEnergy(2)
                }
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(normalCardData)
                .WithCard(preservedCardData)
                .WithCard(autoDisposeCardData)
                .Build();
            var normalCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                normalCardId);
            var preservedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                preservedCardId);
            var autoDisposeCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                autoDisposeCardId);
            built.Ally.CardManager.HandCard.AddCards(
                new[] { normalCard, preservedCard, autoDisposeCard });
            _InitializeEventBuffer(built.Manager);

            _InvokeTurnEnd(built.Manager);

            var lifecycleEvents = built.Manager.PopAllEvents()
                .Where(gameEvent => gameEvent is DiscardHandCardEvent or GainEnergyEvent)
                .ToArray();
            Assert.That(
                lifecycleEvents.Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DiscardHandCardEvent),
                    typeof(GainEnergyEvent),
                    typeof(GainEnergyEvent),
                    typeof(GainEnergyEvent),
                    typeof(DiscardHandCardEvent)
                }));
            Assert.That(
                lifecycleEvents
                    .OfType<GainEnergyEvent>()
                    .Select(gameEvent => gameEvent.GainEnergyResult.EnergyPoint),
                Is.EqualTo(new[] { 4, 1, 2 }));
            Assert.That(
                lifecycleEvents
                    .OfType<GainEnergyEvent>()
                    .Select(gameEvent => gameEvent.Faction),
                Is.EqualTo(new[] { Faction.Ally, Faction.Ally, Faction.Ally }));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(built.Ally.MaxEnergy));
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Is.EqualTo(new[] { normalCard }));
            Assert.That(
                built.Ally.CardManager.ExclusionZone.Cards,
                Is.EqualTo(new[] { autoDisposeCard }));
            Assert.That(
                built.Ally.CardManager.HandCard.Cards,
                Is.EqualTo(new[] { preservedCard }));
        }

        [Test]
        public void TurnEnd_PreservedRunsCardDataBeforeCardBuff()
        {
            const string cardId = "turn-end-preserved-order-card";
            const string buffId = "turn-end-preserved-order-buff";
            var cardData = _CreatePreservedEnergyCardData(cardId, 1);
            var cardBuffData = new CardBuffData
            {
                ID = buffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData(),
                Effects = new Dictionary<CardTriggeredTiming, ConditionalCardBuffEffect[]>
                {
                    [CardTriggeredTiming.Preserved] = new[]
                    {
                        new ConditionalCardBuffEffect
                        {
                            Conditions = { new ConstCondition { Value = true } },
                            Effect = _GainEnergy(2)
                        }
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
                buffId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            _InvokeTurnEnd(built.Manager);

            Assert.That(
                built.Manager.PopAllEvents()
                    .OfType<GainEnergyEvent>()
                    .Select(gameEvent => gameEvent.GainEnergyResult.EnergyPoint),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(3));
        }

        [Test]
        public void TurnEnd_PreservedSnapshotSurvivesLaterCardFormChange()
        {
            const string firstCardId = "turn-end-preserved-snapshot-first";
            const string secondCardId = "turn-end-preserved-snapshot-second";
            const string overrideCardId = "turn-end-preserved-snapshot-override";
            var firstCardData = CardTestBuilder.CreateCardData(firstCardId);
            firstCardData.PropertyDatas.Add(new PreservedPropertyData());
            var secondCardData = _CreatePreservedEnergyCardData(secondCardId, 1);
            var overrideCardData = new OverrideCardData
            {
                ID = overrideCardId,
                Cost = 0,
                Power = 0
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(firstCardData)
                .WithCard(secondCardData)
                .WithCard(overrideCardData)
                .Build();
            var firstCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                firstCardId);
            var secondCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                secondCardId);
            firstCardData.TriggeredEffects[CardTriggeredTiming.Preserved] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new ApplyCardFormOverrideEffect
                    {
                        TargetCards = new FixedCardCollection(secondCard),
                        OverrideKey = "preserved-snapshot",
                        TargetCardDataId = overrideCardId
                    }
                }
            };
            built.Ally.CardManager.HandCard.AddCards(new[] { firstCard, secondCard });
            _InitializeEventBuffer(built.Manager);

            _InvokeTurnEnd(built.Manager);

            Assert.That(secondCard.CardDataId, Is.EqualTo(overrideCardId));
            Assert.That(secondCard.HasProperty(CardProperty.Preserved), Is.False);
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(
                built.Ally.CardManager.HandCard.Cards,
                Is.EqualTo(new[] { firstCard, secondCard }));
        }

        [Test]
        public void TurnEnd_PreservedEffectSeesOnlyCardsRemainingInHand()
        {
            const string preservedCardId = "turn-end-preserved-hand-state";
            const string normalCardId = "turn-end-normal-hand-state";
            const string autoDisposeCardId = "turn-end-auto-dispose-hand-state";
            const string markerBuffId = "turn-end-preserved-hand-marker";
            var preservedCardData = CardTestBuilder.CreateCardData(preservedCardId);
            preservedCardData.PropertyDatas.Add(new PreservedPropertyData());
            preservedCardData.TriggeredEffects[CardTriggeredTiming.Preserved] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new AddCardBuffEffect
                    {
                        TargetCards = new CardsOfPlayer
                        {
                            Player = new CardOwner { Card = new TriggeredCard() },
                            Zone = CardCollectionType.HandCard
                        },
                        AddCardBuffDatas = new List<AddCardBuffData>
                        {
                            new()
                            {
                                CardBuffId = markerBuffId,
                                Level = new ConstInteger { Value = 1 }
                            }
                        }
                    }
                }
            };
            var normalCardData = CardTestBuilder.CreateCardData(normalCardId);
            var autoDisposeCardData = CardTestBuilder.CreateCardData(autoDisposeCardId);
            autoDisposeCardData.PropertyDatas.Add(new AutoDisposePropertyData());
            var markerBuffData = new CardBuffData
            {
                ID = markerBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(preservedCardData)
                .WithCard(normalCardData)
                .WithCard(autoDisposeCardData)
                .WithCardBuff(markerBuffData)
                .Build();
            var preservedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                preservedCardId);
            var normalCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                normalCardId);
            var autoDisposeCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                autoDisposeCardId);
            built.Ally.CardManager.HandCard.AddCards(
                new[] { normalCard, preservedCard, autoDisposeCard });
            _InitializeEventBuffer(built.Manager);

            _InvokeTurnEnd(built.Manager);

            Assert.That(
                preservedCard.BuffManager.Buffs.Select(buff => buff.CardBuffDataID),
                Does.Contain(markerBuffId));
            Assert.That(normalCard.BuffManager.Buffs, Is.Empty);
            Assert.That(autoDisposeCard.BuffManager.Buffs, Is.Empty);
            Assert.That(built.Ally.CardManager.Graveyard.Cards, Is.EqualTo(new[] { normalCard }));
            Assert.That(
                built.Ally.CardManager.ExclusionZone.Cards,
                Is.EqualTo(new[] { autoDisposeCard }));
        }

        [Test]
        public void TurnEnd_PreservedWinsAutoDisposeAndDoesNotDispatchDiscarded()
        {
            const string cardId = "turn-end-preserved-auto-dispose-discarded";
            var cardData = _CreatePreservedEnergyCardData(cardId, 1);
            cardData.PropertyDatas.Add(new AutoDisposePropertyData());
            cardData.TriggeredEffects[CardTriggeredTiming.Discarded] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _GainEnergy(100)
                }
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            _InvokeTurnEnd(built.Manager);

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(new[] { card }));
            Assert.That(built.Ally.CardManager.ExclusionZone.Cards, Is.Empty);
            Assert.That(
                built.Manager.PopAllEvents().OfType<GainEnergyEvent>().Count(),
                Is.EqualTo(1));
        }

        private static StandardCardData _CreatePreservedEnergyCardData(
            string cardId,
            int energy = 1)
        {
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.PropertyDatas.Add(new PreservedPropertyData());
            cardData.TriggeredEffects[CardTriggeredTiming.Preserved] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _GainEnergy(energy)
                }
            };
            return cardData;
        }

        private static GainEnergyEffect _GainEnergy(int value)
        {
            return new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection
                {
                    Target = new CardOwner { Card = new TriggeredCard() }
                },
                Value = new ConstInteger { Value = value }
            };
        }

        private static Faction _GetFaction(IGameEvent gameEvent)
        {
            return gameEvent switch
            {
                DiscardHandCardEvent discardEvent => discardEvent.Faction,
                GainEnergyEvent gainEnergyEvent => gainEnergyEvent.Faction,
                _ => Faction.None
            };
        }

        private static void _InitializeEventBuffer(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetField("_gameEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, new List<IGameEvent>());
        }

        private static void _InvokeTurnEnd(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetMethod("_TurnEnd", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(manager, null);
        }

        private sealed class FixedCardCollection : ITargetCardCollectionValue
        {
            private readonly ICardEntity _card;

            public FixedCardCollection(ICardEntity card)
            {
                _card = card;
            }

            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext triggerContext)
            {
                return new[] { _card };
            }
        }
    }
}
