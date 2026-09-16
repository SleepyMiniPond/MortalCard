using System;
using MortalGame.GameModel;
using MortalGame.GameData;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{

    public class EffectQueueRunnerTests
    {
        [Test]
        public void DrawCardCommandHandler_ExpandsDrawCountIntoImmediateSingleCardItems()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DrawCardIntentAction(SystemSource.Instance));
            var runner = new EffectQueueRunner();
            var handler = new DrawCardEffectCommandHandler();

            var result = handler.Handle(
                context,
                new DrawCardEffectCommand(built.Ally, 3, true),
                runner);

            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
            Assert.That(runner.PendingItemCount, Is.EqualTo(3));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RunToCompletion_WithDrawCommand_ProcessesCardsAsSeparateQueueItemsInDeckOrder(
            bool isSystemInitiated)
        {
            const string firstCardId = "draw-first";
            const string secondCardId = "draw-second";
            const string thirdCardId = "draw-third";
            var built = new GameplayManagerTestBuilder()
                .WithCard(CardTestBuilder.CreateCardData(firstCardId))
                .WithCard(CardTestBuilder.CreateCardData(secondCardId))
                .WithCard(CardTestBuilder.CreateCardData(thirdCardId))
                .Build();
            var cards = new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, firstCardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, secondCardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, thirdCardId)
            };
            built.Ally.CardManager.Deck.AddCards(cards);
            IActionUnit originAction = isSystemInitiated
                ? new DrawCardIntentTargetAction(SystemSource.Instance, new PlayerTarget(built.Ally))
                : new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                originAction);
            var commands = new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, cards.Length, isSystemInitiated)
            });
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, commands);
            var result = runner.RunToCompletion();

            Assert.That(runner.ProcessedItemCount, Is.EqualTo(cards.Length + 1));
            Assert.That(built.Ally.CardManager.Deck.Cards, Is.Empty);
            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(cards));
            Assert.That(
                result.Actions.OfType<DrawCardResultAction>().Select(action => action.Card),
                Is.EqualTo(cards));
            Assert.That(
                result.Events.OfType<DrawCardEvent>().Select(gameEvent => gameEvent.NewCardInfo.Identity),
                Is.EqualTo(cards.Select(card => card.Identity)));
        }

        [Test]
        public void RunToCompletion_WithMultipleDrawCommands_PreservesCommandAndCardOrder()
        {
            const string allyFirstCardId = "ally-draw-first";
            const string allySecondCardId = "ally-draw-second";
            const string enemyCardId = "enemy-draw";
            var built = new GameplayManagerTestBuilder()
                .WithCard(CardTestBuilder.CreateCardData(allyFirstCardId))
                .WithCard(CardTestBuilder.CreateCardData(allySecondCardId))
                .WithCard(CardTestBuilder.CreateCardData(enemyCardId))
                .Build();
            var allyCards = new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, allyFirstCardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, allySecondCardId)
            };
            var enemyCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, enemyCardId);
            built.Ally.CardManager.Deck.AddCards(allyCards);
            built.Enemy.CardManager.Deck.AddCard(enemyCard);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DrawCardIntentAction(SystemSource.Instance));
            var commands = new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, allyCards.Length, true),
                new DrawCardEffectCommand(built.Enemy, 1, true)
            });
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, commands);
            var result = runner.RunToCompletion();

            Assert.That(runner.ProcessedItemCount, Is.EqualTo(5));
            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(allyCards));
            Assert.That(built.Enemy.CardManager.HandCard.Cards, Is.EqualTo(new[] { enemyCard }));
            Assert.That(
                result.Actions.OfType<DrawCardResultAction>().Select(action => action.Card),
                Is.EqualTo(allyCards.Concat(new[] { enemyCard })));
        }

        [Test]
        public void RunToCompletion_WithSystemDraw_DispatchesEachDrawedBeforeNextCard()
        {
            const string firstCardId = "drawed-first";
            const string secondCardId = "drawed-second";
            var drawedEffect = new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            };
            var firstCardData = CardTestBuilder.CreateCardData(firstCardId);
            firstCardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = drawedEffect
                }
            };
            var secondCardData = CardTestBuilder.CreateCardData(secondCardId);
            secondCardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = drawedEffect
                }
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(firstCardData)
                .WithCard(secondCardData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var cards = new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, firstCardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, secondCardId)
            };
            built.Ally.CardManager.Deck.AddCards(cards);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DrawCardIntentTargetAction(SystemSource.Instance, new PlayerTarget(built.Ally)));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, cards.Length, true)
            }));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(
                result.Actions.Select(action => action.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DrawCardResultAction),
                    typeof(GainEnergyResultAction),
                    typeof(DrawCardResultAction),
                    typeof(GainEnergyResultAction)
                }));
            Assert.That(
                result.Events
                    .Where(gameEvent => gameEvent is DrawCardEvent or GainEnergyEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DrawCardEvent),
                    typeof(GainEnergyEvent),
                    typeof(DrawCardEvent),
                    typeof(GainEnergyEvent)
                }));
        }

        [Test]
        public void RunToCompletion_WithNonSystemDraw_DispatchesEffectDrawedInsteadOfDrawed()
        {
            const string cardId = "effect-draw-boundary";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 100 }
                    }
                }
            };
            cardData.TriggeredEffects[CardTriggeredTiming.EffectDrawed] = new[]
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
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.Deck.AddCard(card);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, 1, false)
            }));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(result.Actions.OfType<DrawCardResultAction>().Count(), Is.EqualTo(1));
            Assert.That(result.Actions.OfType<GainEnergyResultAction>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<DrawCardEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunToCompletion_WithMultipleEffectDraws_DispatchesEachEffectDrawedBeforeNextCard()
        {
            const string firstCardId = "effect-drawed-first";
            const string secondCardId = "effect-drawed-second";
            var built = new GameplayManagerTestBuilder()
                .WithCard(_CreateTriggeredGainEnergyCardData(
                    firstCardId,
                    CardTriggeredTiming.EffectDrawed))
                .WithCard(_CreateTriggeredGainEnergyCardData(
                    secondCardId,
                    CardTriggeredTiming.EffectDrawed))
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var cards = new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, firstCardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, secondCardId)
            };
            built.Ally.CardManager.Deck.AddCards(cards);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, cards.Length, false)
            }));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(result.Actions.Select(action => action.GetType()), Is.EqualTo(new[]
            {
                typeof(DrawCardResultAction),
                typeof(GainEnergyResultAction),
                typeof(DrawCardResultAction),
                typeof(GainEnergyResultAction)
            }));
            Assert.That(
                result.Events
                    .Where(gameEvent => gameEvent is DrawCardEvent or GainEnergyEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(DrawCardEvent),
                    typeof(GainEnergyEvent),
                    typeof(DrawCardEvent),
                    typeof(GainEnergyEvent)
                }));
        }

        [Test]
        public void RunToCompletion_WhenEffectDrawedEffectDrawsAgain_NestedDrawStillDispatchesEffectDrawed()
        {
            const string chainStarterCardId = "effect-draw-chain-starter";
            const string nestedDrawCardId = "effect-draw-chain-result";
            var chainStarterCardData = CardTestBuilder.CreateCardData(chainStarterCardId);
            chainStarterCardData.TriggeredEffects[CardTriggeredTiming.EffectDrawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new DrawCardEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 1 }
                    }
                }
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(chainStarterCardData)
                .WithCard(_CreateTriggeredGainEnergyCardData(
                    nestedDrawCardId,
                    CardTriggeredTiming.EffectDrawed))
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var cards = new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, chainStarterCardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, nestedDrawCardId)
            };
            built.Ally.CardManager.Deck.AddCards(cards);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, 1, false)
            }));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(cards));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(result.Actions.Select(action => action.GetType()), Is.EqualTo(new[]
            {
                typeof(DrawCardResultAction),
                typeof(DrawCardResultAction),
                typeof(GainEnergyResultAction)
            }));
        }

        [Test]
        public void RunToCompletion_WhenSystemDrawedEffectDrawsAgain_NestedDrawStillDispatchesDrawed()
        {
            const string chainStarterCardId = "system-draw-chain-starter";
            const string nestedDrawCardId = "system-draw-chain-result";
            var chainStarterCardData = CardTestBuilder.CreateCardData(chainStarterCardId);
            chainStarterCardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new DrawCardEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 1 }
                    }
                }
            };
            var nestedDrawCardData = CardTestBuilder.CreateCardData(nestedDrawCardId);
            nestedDrawCardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
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
            var built = new GameplayManagerTestBuilder()
                .WithCard(chainStarterCardData)
                .WithCard(nestedDrawCardData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var cards = new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, chainStarterCardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, nestedDrawCardId)
            };
            built.Ally.CardManager.Deck.AddCards(cards);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DrawCardIntentTargetAction(SystemSource.Instance, new PlayerTarget(built.Ally)));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, 1, true)
            }));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(cards));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(result.Actions.Select(action => action.GetType()), Is.EqualTo(new[]
            {
                typeof(DrawCardResultAction),
                typeof(DrawCardResultAction),
                typeof(GainEnergyResultAction)
            }));
        }

        [Test]
        public void RunToCompletion_WithEmptyDeckAndGraveyard_DoesNotDispatchDrawed()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DrawCardIntentTargetAction(SystemSource.Instance, new PlayerTarget(built.Ally)));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new DrawCardEffectCommand(built.Ally, 1, true)
            }));
            var result = runner.RunToCompletion();

            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithCreateAndCloneIntoHand_DoesNotDispatchDrawed()
        {
            const string cardId = "create-clone-not-draw";
            var cardData = _CreateDrawedGainEnergyCardData(cardId);
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var originCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            var createdCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            var clonedCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.Deck.AddCard(originCard);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DrawCardIntentTargetAction(SystemSource.Instance, new PlayerTarget(built.Ally)));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new CreateCardEffectCommand(built.Ally, createdCard, CardCollectionType.HandCard),
                new CloneCardEffectCommand(
                    built.Ally,
                    originCard,
                    clonedCard,
                    CardCollectionType.HandCard)
            }));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(new[] { createdCard, clonedCard }));
            Assert.That(built.Ally.CurrentEnergy, Is.Zero);
            Assert.That(result.Actions.OfType<CreateCardResultAction>().Count(), Is.EqualTo(1));
            Assert.That(result.Actions.OfType<CloneCardResultAction>().Count(), Is.EqualTo(1));
            Assert.That(result.Actions.OfType<GainEnergyResultAction>(), Is.Empty);
        }

        [TestCase(CardCollectionType.Graveyard)]
        [TestCase(CardCollectionType.ExclusionZone)]
        public void RunToCompletion_WithMoveFromNonDeckZoneIntoHand_DoesNotDispatchDrawed(
            CardCollectionType sourceZone)
        {
            const string cardId = "move-to-hand-not-draw";
            var cardData = _CreateDrawedGainEnergyCardData(cardId);
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.GetCardCollectionZone(sourceZone).AddCard(card);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DrawCardIntentTargetAction(SystemSource.Instance, new PlayerTarget(built.Ally)));
            var runner = new EffectQueueRunner();

            runner.EnqueueCommands(context, new EffectCommandSet(new IEffectCommand[]
            {
                new MoveCardEffectCommand(
                    built.Ally,
                    card,
                    sourceZone,
                    CardCollectionType.HandCard,
                    MoveCardType.Recycle)
            }));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(new[] { card }));
            Assert.That(built.Ally.CurrentEnergy, Is.Zero);
            Assert.That(result.Actions.OfType<MoveCardResultAction>().Count(), Is.EqualTo(1));
            Assert.That(result.Actions.OfType<GainEnergyResultAction>(), Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithCardEffects_AppliesCommandsAndCollectsResultsInOrder()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new GainEnergyIntentAction(SystemSource.Instance));
            var runner = new EffectQueueRunner();
            var firstEffect = new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            };
            var secondEffect = new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 2 }
            };

            runner.Enqueue(new CardEffectQueueItem(context, firstEffect));
            runner.Enqueue(new CardEffectQueueItem(context, secondEffect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(3));
            Assert.That(result.Actions.Select(action => action.GetType()).ToArray(), Is.EqualTo(new[]
            {
            typeof(GainEnergyResultAction),
            typeof(GainEnergyResultAction)
        }));
            Assert.That(result.Events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void RunToCompletion_WithNegativeDamage_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var initialHealth = built.Ally.MainCharacter.CurrentHealth;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DamageIntentAction(SystemSource.Instance, DamageType.Normal));
            var effect = new DamageEffect
            {
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
                },
                Value = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(initialHealth));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithNegativeHeal_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            built.Ally.MainCharacter.HealthManager.TakeDamage(
                10,
                built.ContextManager.Context,
                DamageType.Penetrate);
            var initialHealth = built.Ally.MainCharacter.CurrentHealth;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new HealIntentAction(SystemSource.Instance));
            var effect = new HealEffect
            {
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
                },
                Value = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(initialHealth));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithNegativeShield_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            built.Ally.MainCharacter.HealthManager.GetShield(
                10,
                built.ContextManager.Context);
            var initialShield = built.Ally.MainCharacter.CurrentShield;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new ShieldIntentAction(SystemSource.Instance));
            var effect = new ShieldEffect
            {
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
                },
                Value = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.MainCharacter.CurrentShield, Is.EqualTo(initialShield));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithNegativeEnergyGain_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            built.Ally.EnergyManager.GainEnergy(1);
            var initialEnergy = built.Ally.CurrentEnergy;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new GainEnergyIntentAction(SystemSource.Instance));
            var effect = new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(initialEnergy));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithNegativeEnergyLoss_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            built.Ally.EnergyManager.GainEnergy(1);
            var initialEnergy = built.Ally.CurrentEnergy;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new LoseEnergyIntentAction(SystemSource.Instance));
            var effect = new LoseEnegyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(initialEnergy));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithNegativePlayerBuffAddition_DoesNotModifyExistingBuff()
        {
            var buffData = new PlayerBuffData
            {
                ID = BuffTestBuilder.PlayerBuffId,
                MaxLevel = 99,
                LifeTimeData = new AlwaysLifeTimePlayerBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(buffData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var buff = BuffTestBuilder.CreatePlayerBuff();
            built.Ally.BuffManager.AddBuff(buff);
            var initialLevel = buff.Level;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new AddPlayerBuffIntentAction(SystemSource.Instance));
            var effect = new AddPlayerBuffEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                BuffId = BuffTestBuilder.PlayerBuffId,
                Level = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(buff.Level, Is.EqualTo(initialLevel));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WhenPlayerBuffAdditionExceedsMaximum_CreatesBuffAtMaximumLevel()
        {
            var buffData = new PlayerBuffData
            {
                ID = BuffTestBuilder.PlayerBuffId,
                MaxLevel = 3,
                LifeTimeData = new AlwaysLifeTimePlayerBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(buffData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new AddPlayerBuffIntentAction(SystemSource.Instance));
            var effect = new AddPlayerBuffEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                BuffId = BuffTestBuilder.PlayerBuffId,
                Level = new ConstInteger { Value = 5 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.BuffManager.Buffs.Single().Level, Is.EqualTo(3));
            Assert.That(result.Actions.Single(), Is.TypeOf<AddPlayerBuffResultAction>());
            Assert.That(result.Events.OfType<AddPlayerBuffEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunToCompletion_WithNegativePlayerBuffLevelDelta_ClampsExistingBuffToZero()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var buff = BuffTestBuilder.CreatePlayerBuff();
            built.Ally.BuffManager.AddBuff(buff);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new ModifyPlayerBuffLevelIntentAction(SystemSource.Instance));
            var effect = new ModifyPlayerBuffLevelEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                BuffId = BuffTestBuilder.PlayerBuffId,
                DeltaLevel = new ConstInteger { Value = -2 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(buff.Level, Is.Zero);
            Assert.That(built.Ally.BuffManager.Buffs, Does.Contain(buff));
            Assert.That(result.Actions.Single(), Is.TypeOf<ModifyPlayerBuffLevelResultAction>());
            Assert.That(result.Events.OfType<ModifyPlayerBuffLevelEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunToCompletion_WithMissingPlayerBuffToModify_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new ModifyPlayerBuffLevelIntentAction(SystemSource.Instance));
            var effect = new ModifyPlayerBuffLevelEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                BuffId = BuffTestBuilder.PlayerBuffId,
                DeltaLevel = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithNegativeDispositionIncrease_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var initialDisposition = built.Ally.DispositionManager.CurrentDisposition;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new IncreaseDispositionIntentAction(SystemSource.Instance));
            var effect = new IncreaseDispositionEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.DispositionManager.CurrentDisposition, Is.EqualTo(initialDisposition));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithNegativeDispositionDecrease_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            built.Ally.DispositionManager.IncreaseDisposition(1);
            var initialDisposition = built.Ally.DispositionManager.CurrentDisposition;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new DecreaseDispositionIntentAction(SystemSource.Instance));
            var effect = new DecreaseDispositionEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = -1 }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.DispositionManager.CurrentDisposition, Is.EqualTo(initialDisposition));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WithMixedCardBuffAdditions_SkipsNegativeLevelOnly()
        {
            const string negativeBuffId = "negative-card-buff";
            const string validBuffId = "valid-card-buff";
            var negativeBuffData = new CardBuffData
            {
                ID = negativeBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var validBuffData = new CardBuffData
            {
                ID = validBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(negativeBuffData)
                .WithCardBuff(validBuffData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new AddCardBuffIntentAction(SystemSource.Instance));
            var effect = new AddCardBuffEffect
            {
                TargetCards = new CardsOfPlayer
                {
                    Player = new CurrentPlayer(),
                    Zone = CardCollectionType.HandCard
                },
                AddCardBuffDatas =
                {
                    new AddCardBuffData
                    {
                        CardBuffId = negativeBuffId,
                        Level = new ConstInteger { Value = -1 }
                    },
                    new AddCardBuffData
                    {
                        CardBuffId = validBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(card.BuffManager.Buffs.Single().CardBuffDataID, Is.EqualTo(validBuffId));
            Assert.That(result.Actions.Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<AddCardBuffEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunToCompletion_WithCreateCardMixedBuffAdditions_CreatesCardWithValidBuffOnly()
        {
            const string negativeBuffId = "create-negative-card-buff";
            const string validBuffId = "create-valid-card-buff";
            var negativeBuffData = new CardBuffData
            {
                ID = negativeBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var validBuffData = new CardBuffData
            {
                ID = validBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(negativeBuffData)
                .WithCardBuff(validBuffData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new CreateCardIntentAction(SystemSource.Instance));
            var effect = new CreateCardEffect
            {
                Target = new CurrentPlayer(),
                CardDataIds = { CardTestBuilder.CardId },
                CreateDestination = CardCollectionType.HandCard,
                AddCardBuffDatas =
                {
                    new AddCardBuffData
                    {
                        CardBuffId = negativeBuffId,
                        Level = new ConstInteger { Value = -1 }
                    },
                    new AddCardBuffData
                    {
                        CardBuffId = validBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            var createdCard = built.Ally.CardManager.HandCard.Cards.Single();
            Assert.That(createdCard.BuffManager.Buffs.Single().CardBuffDataID, Is.EqualTo(validBuffId));
            Assert.That(result.Actions.Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<AddCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunToCompletion_WithCloneCardMixedBuffAdditions_ClonesCardWithValidBuffOnly()
        {
            const string negativeBuffId = "clone-negative-card-buff";
            const string validBuffId = "clone-valid-card-buff";
            var negativeBuffData = new CardBuffData
            {
                ID = negativeBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var validBuffData = new CardBuffData
            {
                ID = validBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(negativeBuffData)
                .WithCardBuff(validBuffData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var originCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(originCard);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new CloneCardIntentAction(SystemSource.Instance));
            var effect = new CloneCardEffect
            {
                Target = new CurrentPlayer(),
                ClonedCards = new CardsOfPlayer
                {
                    Player = new CurrentPlayer(),
                    Zone = CardCollectionType.HandCard
                },
                CloneDestination = CardCollectionType.HandCard,
                AddCardBuffDatas =
                {
                    new AddCardBuffData
                    {
                        CardBuffId = negativeBuffId,
                        Level = new ConstInteger { Value = -1 }
                    },
                    new AddCardBuffData
                    {
                        CardBuffId = validBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            var clonedCard = built.Ally.CardManager.HandCard.Cards.Single(card => card != originCard);
            Assert.That(originCard.BuffManager.Buffs, Is.Empty);
            Assert.That(clonedCard.BuffManager.Buffs.Single().CardBuffDataID, Is.EqualTo(validBuffId));
            Assert.That(result.Actions.Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<AddCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunToCompletion_WithPlayerBuffMixedCardBuffAdditions_SkipsNegativeLevelOnly()
        {
            const string negativeBuffId = "player-buff-negative-card-buff";
            const string validBuffId = "player-buff-valid-card-buff";
            var negativeBuffData = new CardBuffData
            {
                ID = negativeBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var validBuffData = new CardBuffData
            {
                ID = validBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(negativeBuffData)
                .WithCardBuff(validBuffData)
                .Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = new TriggerContext(
                built.Manager,
                new PlayerBuffTrigger(built.Ally, BuffTestBuilder.CreatePlayerBuff()),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var effect = new AddCardBuffEffect
            {
                TargetCards = new CardsOfPlayer
                {
                    Player = new CurrentPlayer(),
                    Zone = CardCollectionType.HandCard
                },
                AddCardBuffDatas =
                {
                    new AddCardBuffData
                    {
                        CardBuffId = negativeBuffId,
                        Level = new ConstInteger { Value = -1 }
                    },
                    new AddCardBuffData
                    {
                        CardBuffId = validBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                }
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new PlayerBuffEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(card.BuffManager.Buffs.Single().CardBuffDataID, Is.EqualTo(validBuffId));
            Assert.That(result.Actions.Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<AddCardBuffEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunToCompletion_WithPlayerBuffEffect_AppliesCommands()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var context = new TriggerContext(
                built.Manager,
                new PlayerBuffTrigger(built.Ally, BuffTestBuilder.CreatePlayerBuff()),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();
            var effect = new DamageEffect
            {
                Type = DamageType.Effective,
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
                },
                Value = new ConstInteger { Value = 5 }
            };

            runner.Enqueue(new PlayerBuffEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(95));
            Assert.That(result.Actions.Single(), Is.TypeOf<DamageResultAction>());
            Assert.That(result.Events.OfType<DamageEvent>().Single().Character, Is.SameAs(built.Ally.MainCharacter));
        }

        [Test]
        public void RunToCompletion_WithCharacterBuffEffect_AppliesCommands()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Enemy);
            var context = new TriggerContext(
                built.Manager,
                new CharacterBuffTrigger(
                    built.Enemy.MainCharacter,
                    BuffTestBuilder.CreateCharacterBuff()),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();
            var effect = new DamageEffect
            {
                Type = DamageType.Effective,
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
                },
                Value = new ConstInteger { Value = 7 }
            };

            runner.Enqueue(new CharacterBuffEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Enemy.MainCharacter.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.Actions.Single(), Is.TypeOf<DamageResultAction>());
            Assert.That(result.Events.OfType<DamageEvent>().Single().Character, Is.SameAs(built.Enemy.MainCharacter));
        }

        [Test]
        public void RunToCompletion_WithUnknownCardBuffEffect_ReturnsEmptyResult()
        {
            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalCardBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new NoOpCardBuffEffect()
                });
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(cardBuffData)
                .Build();
            var createBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var buff = BuffTestBuilder.CreateCardBuff(createBuffContext, built.ContextManager.CardBuffLibrary);
            var context = new TriggerContext(
                built.Manager,
                new CardBuffTrigger(CardEntity.DummyCard, buff),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardBuffEffectQueueItem(context, new NoOpCardBuffEffect()));
            var result = runner.RunToCompletion();

            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WhenItemEnqueuesAnotherItem_ProcessesEnqueuedItemBeforeReturning()
        {
            var runner = new EffectQueueRunner();

            runner.Enqueue(new ChainedQueueItem(null, 1, 2));
            var result = runner.RunToCompletion();

            Assert.That(result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id).ToArray(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void EnqueueRange_PreservesProvidedOrderInExistingScope()
        {
            var runner = new EffectQueueRunner();

            runner.EnqueueRange(new EffectQueueItem[]
            {
                new StaticQueueItem(null, 1),
                new StaticQueueItem(null, 2)
            });
            runner.Enqueue(new StaticQueueItem(null, 3));
            var result = runner.RunToCompletion();

            Assert.That(
                result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(runner.ProcessedItemCount, Is.EqualTo(3));
        }

        [Test]
        public void StaticRunToCompletion_CreatesScopeAndProcessesFollowUpItems()
        {
            var result = EffectQueueRunner.RunToCompletion(new EffectQueueItem[]
            {
                new ChainedQueueItem(null, 1, 2)
            });

            Assert.That(
                result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id),
                Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void RunToCompletion_WhenItemEnqueuesImmediateItem_ProcessesItBeforeQueuedTail()
        {
            var runner = new EffectQueueRunner();

            runner.Enqueue(new ImmediateQueueItem(null));
            runner.Enqueue(new StaticQueueItem(null, 3));
            var result = runner.RunToCompletion();

            Assert.That(result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id).ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void RunToCompletion_WhenImmediateItemsUseExecutionOrder_PreservesProvidedOrder()
        {
            var runner = new EffectQueueRunner();

            runner.Enqueue(new ImmediateSequenceQueueItem(null));
            runner.Enqueue(new StaticQueueItem(null, 4));
            var result = runner.RunToCompletion();

            Assert.That(
                result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id).ToArray(),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void RunToCompletion_TwoRunners_HaveIndependentBudgets()
        {
            var firstRunner = new EffectQueueRunner();
            var secondRunner = new EffectQueueRunner();

            firstRunner.Enqueue(new StaticQueueItem(null, 1));
            firstRunner.Enqueue(new StaticQueueItem(null, 2));
            var firstResult = firstRunner.RunToCompletion();

            secondRunner.Enqueue(new StaticQueueItem(null, 3));
            secondRunner.Enqueue(new StaticQueueItem(null, 4));
            var secondResult = secondRunner.RunToCompletion();

            Assert.That(
                firstResult.Events.OfType<TestQueueEvent>().Select(evt => evt.Id),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(
                secondResult.Events.OfType<TestQueueEvent>().Select(evt => evt.Id),
                Is.EqualTo(new[] { 3, 4 }));
            Assert.That(firstRunner.ProcessedItemCount, Is.EqualTo(2));
            Assert.That(secondRunner.ProcessedItemCount, Is.EqualTo(2));
            Assert.That(firstRunner.IsHalted, Is.False);
            Assert.That(secondRunner.IsHalted, Is.False);
        }

        [Test]
        public void TimingDispatchPlan_OrdersGeneralReactionsBeforeFormTransitions()
        {
            var plan = new TimingDispatchPlan(
                new EffectQueueItem[]
                {
                    new StaticQueueItem(null, 1),
                    new StaticQueueItem(null, 2)
                },
                new EffectQueueItem[]
                {
                    new StaticQueueItem(null, 3)
                });

            Assert.That(
                plan.OrderedItems
                    .Cast<StaticQueueItem>()
                    .Select(item => item.Id),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        private static StandardCardData _CreateDrawedGainEnergyCardData(string cardId)
        {
            return _CreateTriggeredGainEnergyCardData(cardId, CardTriggeredTiming.Drawed);
        }

        private static StandardCardData _CreateTriggeredGainEnergyCardData(
            string cardId,
            CardTriggeredTiming timing)
        {
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.TriggeredEffects[timing] = new[]
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
            return cardData;
        }
    }
}

public sealed record TestQueueEvent(int Id) : IGameEvent;

public sealed record ChainedQueueItem(
    TriggerContext Context,
    int CurrentId,
    int NextId) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        if (NextId > 0)
        {
            queue.Enqueue(new ChainedQueueItem(Context, NextId, 0));
        }

        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(CurrentId) });
    }
}

public sealed record SelfEnqueueingQueueItem(TriggerContext Context) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        queue.Enqueue(new SelfEnqueueingQueueItem(Context));
        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(queue.ProcessedItemCount) });
    }
}

public sealed record ImmediateQueueItem(TriggerContext Context) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        queue.EnqueueImmediate(new StaticQueueItem(Context, 2));
        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(1) });
    }
}

public sealed record StaticQueueItem(TriggerContext Context, int Id) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(Id) });
    }
}

public sealed record ImmediateSequenceQueueItem(TriggerContext Context) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        queue.EnqueueImmediate(new EffectQueueItem[]
        {
            new StaticQueueItem(Context, 2),
            new StaticQueueItem(Context, 3)
        });
        return new EffectResult(
            Array.Empty<BaseResultAction>(),
            new IGameEvent[] { new TestQueueEvent(1) });
    }
}
