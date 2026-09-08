using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class DamageEffectIntegrationTests
    {
        [Test]
        public void DamageEffect_ThreeBuffSources_DelegateToTheSameDamageCommandPipeline()
        {
            var cardData = CardTestBuilder.CreateCardData();
            var cardBuffData = new CardBuffData
            {
                ID = BuffTestBuilder.CardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .Build();
            var enemyCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(enemyCard);
            var createBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var cardBuff = BuffTestBuilder.CreateCardBuff(
                createBuffContext,
                built.ContextManager.CardBuffLibrary);
            enemyCard.BuffManager.AddBuff(cardBuff);

            var playerContext = new TriggerContext(
                built.Manager,
                new PlayerBuffTrigger(built.Ally, BuffTestBuilder.CreatePlayerBuff()),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var characterContext = new TriggerContext(
                built.Manager,
                new CharacterBuffTrigger(
                    built.Ally.MainCharacter,
                    BuffTestBuilder.CreateCharacterBuff()),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var cardContext = new TriggerContext(
                built.Manager,
                new CardBuffTrigger(enemyCard, cardBuff),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));

            var playerResult = _RunPlayerBuffDamage(playerContext, 1);
            var characterResult = _RunCharacterBuffDamage(characterContext, 2);
            var cardResult = _RunCardBuffDamage(cardContext, 3);

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(97));
            Assert.That(built.Enemy.MainCharacter.CurrentHealth, Is.EqualTo(97));
            Assert.That(
                playerResult.Actions
                    .Concat(characterResult.Actions)
                    .Concat(cardResult.Actions),
                Has.All.TypeOf<DamageResultAction>());
            Assert.That(
                playerResult.Events
                    .Concat(characterResult.Events)
                    .Concat(cardResult.Events)
                    .OfType<DamageEvent>()
                    .ToArray(),
                Has.Length.EqualTo(3));
        }

        [Test]
        public void DamageEffect_ThreeBuffDataSources_RunThroughTimingPlanner()
        {
            var playerBuffData = BuffTestBuilder.CreatePlayerBuffData(
                BuffTestBuilder.PlayerBuffId,
                GameTiming.AfterExecuteEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _CreateDamage(1)
                });
            var characterBuffData = BuffTestBuilder.CreateCharacterBuffData(
                BuffTestBuilder.CharacterBuffId,
                GameTiming.AfterExecuteEnd,
                new ConditionalCharacterBuffEffect
                {
                    Conditions = new ICondition[] { new ConstCondition { Value = true } },
                    Effect = _CreateDamage(2)
                });
            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.AfterExecuteEnd,
                new ConditionalCardBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = _CreateDamage(3)
                });
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(playerBuffData)
                .WithCharacterBuff(characterBuffData)
                .WithCardBuff(cardBuffData)
                .Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff());
            built.Ally.MainCharacter.BuffManager.AddBuff(BuffTestBuilder.CreateCharacterBuff());
            var enemyCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(enemyCard);
            var createBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            enemyCard.BuffManager.AddBuff(BuffTestBuilder.CreateCardBuff(
                createBuffContext,
                built.ContextManager.CardBuffLibrary));

            var events = built.Manager
                .TriggerTiming(GameTiming.AfterExecuteEnd, SystemSource.Instance)
                .OfType<DamageEvent>()
                .ToArray();

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(97));
            Assert.That(built.Enemy.MainCharacter.CurrentHealth, Is.EqualTo(97));
            Assert.That(events, Has.Length.EqualTo(3));
        }

        [Test]
        public void TimedBombCardBuff_AfterExecuteEnd_DealsTriggeredCardPowerAndPreservesSelectedCard()
        {
            var bombData = CardTestBuilder.CreateCardData();
            bombData.Power = 10;
            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.AfterExecuteEnd,
                new ConditionalCardBuffEffect
                {
                    Conditions = _CreateTimedBombConditions(),
                    Effect = new DamageEffect
                    {
                        Type = DamageType.Normal,
                        Targets = new SingleCharacterCollection
                        {
                            Target = new MainCharacterOfPlayer
                            {
                                Player = new ReactionOwnerPlayer()
                            }
                        },
                        Value = new ConditionalValue
                        {
                            Pairs =
                            {
                                new ConditionalValue.ConditionPair
                                {
                                    Conditions =
                                    {
                                        new GameTimingCondition
                                        {
                                            Timing = GameTiming.AfterExecuteEnd
                                        }
                                    },
                                    Value = new CardIntegerProperty
                                    {
                                        Card = new TriggeredCard(),
                                        Property = CardIntegerProperty.CardIntegerValueType.CardPower
                                    }
                                }
                            }
                        }
                    }
                });
            var built = new GameplayManagerTestBuilder()
                .WithCard(bombData)
                .WithCardBuff(cardBuffData)
                .Build();
            var bomb = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var selectedCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(bomb);
            built.Ally.CardManager.HandCard.AddCard(selectedCard);
            var createBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            bomb.BuffManager.AddBuff(BuffTestBuilder.CreateCardBuff(
                createBuffContext,
                built.ContextManager.CardBuffLibrary));

            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            using var selectedCardScope = built.ContextManager.SetSelectedCard(selectedCard.Some());

            var firstEvents = built.Manager
                .TriggerTiming(GameTiming.AfterExecuteEnd, SystemSource.Instance)
                .ToList();

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(90));
            Assert.That(firstEvents.OfType<DamageEvent>().ToArray(), Has.Length.EqualTo(1));
            Assert.That(
                built.ContextManager.Context.SelectedCard,
                Is.EqualTo(selectedCard.Identity));

            built.Ally.CardManager.HandCard.RemoveCard(bomb);
            built.Ally.CardManager.Graveyard.AddCard(bomb);
            var secondEvents = built.Manager
                .TriggerTiming(GameTiming.AfterExecuteEnd, SystemSource.Instance)
                .ToList();

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(90));
            Assert.That(secondEvents.OfType<DamageEvent>(), Is.Empty);
        }

        [Test]
        public void ReactionOwnerAndCaster_ResolveByTriggeredSourceWithoutGuessingCurrentPlayer()
        {
            var cardBuffData = new CardBuffData
            {
                ID = BuffTestBuilder.CardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(cardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var createBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var cardBuff = BuffTestBuilder.CreateCardBuff(
                createBuffContext,
                built.ContextManager.CardBuffLibrary,
                caster: built.Enemy);
            card.BuffManager.AddBuff(cardBuff);

            var playerContext = new TriggerContext(
                built.Manager,
                new PlayerBuffTrigger(
                    built.Ally,
                    BuffTestBuilder.CreatePlayerBuff(caster: built.Enemy)),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var characterContext = new TriggerContext(
                built.Manager,
                new CharacterBuffTrigger(
                    built.Ally.MainCharacter,
                    BuffTestBuilder.CreateCharacterBuff(caster: built.Enemy)),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var cardContext = new TriggerContext(
                built.Manager,
                new CardBuffTrigger(card, cardBuff),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));

            Assert.That(new ReactionOwnerPlayer().Eval(playerContext).ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
            Assert.That(new ReactionOwnerPlayer().Eval(characterContext).ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
            Assert.That(new ReactionOwnerPlayer().Eval(cardContext).ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
            Assert.That(new ReactionCasterPlayer().Eval(playerContext).ValueOr((IPlayerEntity)null), Is.SameAs(built.Enemy));
            Assert.That(new ReactionCasterPlayer().Eval(characterContext).ValueOr((IPlayerEntity)null), Is.SameAs(built.Enemy));
            Assert.That(new ReactionCasterPlayer().Eval(cardContext).ValueOr((IPlayerEntity)null), Is.SameAs(built.Enemy));
        }

        [Test]
        public void CardOwner_WhenBothPlayersHaveCards_ResolvesTheActualCardOwner()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var allyCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var enemyCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(allyCard);
            built.Enemy.CardManager.HandCard.AddCard(enemyCard);
            var context = new TriggerContext(
                built.Manager,
                new CardTrigger(enemyCard),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));

            var owner = new CardOwner { Card = new TriggeredCard() }.Eval(context);

            Assert.That(owner.ValueOr((IPlayerEntity)null), Is.SameAs(built.Enemy));
        }

        [Test]
        public void DamageEffect_RoundTripAndValidator_PreserveTypeAndRejectInvalidType()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/DamageEffectRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                asset.Data.ID = "damage-effect-round-trip";
                asset.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                asset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Conditions = { new ConstCondition { Value = true } },
                        Effect = new DamageEffect
                        {
                            Type = DamageType.Penetrate,
                            Targets = new NoneCharacters(),
                            Value = new ConstInteger { Value = 1 }
                        }
                    }
                };
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<CardBuffScriptable>(assetPath);
                var damageEffect = loaded.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect
                    as DamageEffect;
                Assert.That(damageEffect, Is.Not.Null);
                Assert.That(damageEffect.Type, Is.EqualTo(DamageType.Penetrate));

                _SetCatalogCardBuffs(catalog, loaded);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
                Assert.That(GameDataValidator.ValidateEffectResolvers(catalog), Is.Empty);

                damageEffect.Type = (DamageType)999;
                var errors = GameDataValidator.ValidateNestedContent(catalog);
                Assert.That(
                    errors,
                    Has.Some.Contains("DamageEffect.Type 無效：999"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static EffectResult _RunPlayerBuffDamage(TriggerContext context, int value)
        {
            var runner = new EffectQueueRunner();
            runner.Enqueue(new PlayerBuffEffectQueueItem(context, _CreateDamage(value)));
            return runner.RunToCompletion();
        }

        private static EffectResult _RunCharacterBuffDamage(TriggerContext context, int value)
        {
            var runner = new EffectQueueRunner();
            runner.Enqueue(new CharacterBuffEffectQueueItem(context, _CreateDamage(value)));
            return runner.RunToCompletion();
        }

        private static EffectResult _RunCardBuffDamage(TriggerContext context, int value)
        {
            var runner = new EffectQueueRunner();
            runner.Enqueue(new CardBuffEffectQueueItem(context, _CreateDamage(value)));
            return runner.RunToCompletion();
        }

        private static DamageEffect _CreateDamage(int value)
        {
            return new DamageEffect
            {
                Type = DamageType.Normal,
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer
                    {
                        Player = new ReactionOwnerPlayer()
                    }
                },
                Value = new ConstInteger { Value = value }
            };
        }

        private static List<ICondition> _CreateTimedBombConditions()
        {
            return new List<ICondition>
            {
                new GameTimingCondition { Timing = GameTiming.AfterExecuteEnd },
                new IsTriggeredOwnerTurnCondition(),
                new CardCollectionContainsCondition
                {
                    CardCollection = new CardsOfPlayer
                    {
                        Player = new CardOwner { Card = new TriggeredCard() },
                        Zone = CardCollectionType.HandCard
                    },
                    Card = new TriggeredCard()
                }
            };
        }

        private static void _SetCatalogCardBuffs(
            GameContentCatalog catalog,
            params CardBuffScriptable[] cardBuffs)
        {
            typeof(GameContentCatalog)
                .GetField("_cardBuffAssets", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(catalog, cardBuffs);
        }
    }
}
