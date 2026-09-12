using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class GameStartInitializePipelineTests
    {
        private const string InitialCardId = "initial-card";
        private const string CreatedCardId = "created-card";

        [Test]
        public void GameStart_InitialCardsInitializeBeforeFirstDraw_AndCreatedCardsDoNotReinitialize()
        {
            var initialCard = CardTestBuilder.CreateCardData(InitialCardId);
            var createdCard = CardTestBuilder.CreateCardData(CreatedCardId);
            initialCard.TriggeredEffects[CardTriggeredTiming.Initialize] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _GainEnergy(1)
                },
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new CreateCardEffect
                    {
                        Target = new PlayerByFaction { Faction = Faction.Ally },
                        CardDataIds = new List<string> { CreatedCardId },
                        CreateDestination = CardCollectionType.Deck
                    }
                }
            };
            createdCard.TriggeredEffects[CardTriggeredTiming.Initialize] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _GainEnergy(100)
                }
            };

            var enemyDeck = ScriptableObject.CreateInstance<DeckScriptable>();
            try
            {
                var contextManager = GameContextTestBuilder.CreateContextManager(
                    cardLibrary: new CardLibrary(new Dictionary<string, CardData>
                    {
                        [InitialCardId] = initialCard,
                        [CreatedCardId] = createdCard
                    }));
                var stage = new GameStageSetting(
                    StageID: "initialize-test-stage",
                    RandomSeed: 1,
                    Ally: new AllyInstance(
                        Identity: Guid.NewGuid(),
                        NameKey: "ally",
                        CurrentDisposition: 0,
                        CurrentHealth: 100,
                        MaxHealth: 100,
                        CurrentEnergy: 0,
                        MaxEnergy: 10,
                        Deck: new List<CardInstance>
                        {
                            CardInstance.Create(initialCard)
                        },
                        HandCardMaxCount: 5),
                    Enemy: new EnemyData
                    {
                        EnemyID = "initialize-test-enemy",
                        Level = 1,
                        SelectedCardMaxCount = 0,
                        TurnStartDrawCardCount = 0,
                        EnergyRecoverPoint = 0,
                        PlayerData = new PlayerData
                        {
                            ID = "initialize-test-enemy-player",
                            MaxHealth = 100,
                            InitialHealth = 100,
                            MaxEnergy = 10,
                            InitialEnergy = 0,
                            HandCardMaxCount = 5,
                            Deck = enemyDeck,
                            NameKey = "enemy"
                        }
                    });
                var manager = new GameplayManager(stage, contextManager);
                _SetGameEvents(manager);

                _InvokeGameStart(manager);

                var status = ((IGameplayModel)manager).GameStatus;
                Assert.That(status.Ally.CurrentEnergy, Is.EqualTo(1));
                Assert.That(status.Ally.CardManager.Deck.Cards, Has.Count.EqualTo(2));
                Assert.That(
                    status.Ally.CardManager.Deck.Cards.Count(card => card.CardDataId == CreatedCardId),
                    Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyDeck);
            }
        }

        private static GainEnergyEffect _GainEnergy(int value)
        {
            return new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection
                {
                    Target = new CardOwner
                    {
                        Card = new TriggeredCard()
                    }
                },
                Value = new ConstInteger { Value = value }
            };
        }

        private static void _SetGameEvents(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetField("_gameEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, new List<IGameEvent>());
        }

        private static void _InvokeGameStart(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetMethod("_GameStart", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(manager, null);
        }
    }
}
