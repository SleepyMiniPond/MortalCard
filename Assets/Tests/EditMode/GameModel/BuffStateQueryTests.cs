using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class BuffStateQueryTests
    {
        [Test]
        public void BuffConditionContracts_UseSingleIConditionInterface()
        {
            var playerCondition = new PlayerBuffCondition();
            var characterCondition = new CharacterBuffCondition();
            var cardCondition = new CardBuffCondition();

            Assert.That(playerCondition, Is.InstanceOf<ICondition>());
            Assert.That(characterCondition, Is.InstanceOf<ICondition>());
            Assert.That(cardCondition, Is.InstanceOf<ICondition>());
            Assert.That(typeof(ICondition).Assembly.GetType(
                "MortalGame.GameModel.IPlayerBuffCondition"), Is.Null);
            Assert.That(typeof(ICondition).Assembly.GetType(
                "MortalGame.GameModel.ICharacterBuffCondition"), Is.Null);
            Assert.That(typeof(ICondition).Assembly.GetType(
                "MortalGame.GameModel.ICardBuffCondition"), Is.Null);
            Assert.That(
                typeof(ConditionalPlayerBuffEffect).GetField(nameof(ConditionalPlayerBuffEffect.Conditions))
                    ?.FieldType,
                Is.EqualTo(typeof(List<ICondition>)));
            Assert.That(
                typeof(ConditionalCharacterBuffEffect).GetField(nameof(ConditionalCharacterBuffEffect.Conditions))
                    ?.FieldType,
                Is.EqualTo(typeof(ICondition[])));
            Assert.That(
                typeof(ConditionalCardBuffEffect).GetField(nameof(ConditionalCardBuffEffect.Conditions))
                    ?.FieldType,
                Is.EqualTo(typeof(List<ICondition>)));
        }

        [Test]
        public void ExistingPlayerBuffAssets_AfterConditionInterfaceMigration_PreserveConditions()
        {
            var expectedTypesByAsset = new Dictionary<string, Type[]>
            {
                ["ComboAttack.asset"] = new[]
                {
                    typeof(PlayerBuffCondition),
                    typeof(CardPlayCondition)
                },
                ["CounterAttack.asset"] = new[]
                {
                    typeof(PlayerCondition),
                    typeof(CardPlayResultCondition)
                },
                ["FollowAttack.asset"] = new[]
                {
                    typeof(IsTriggeredOwnerTurnCondition),
                    typeof(CardPlayCondition),
                    typeof(CardPlayResultCondition)
                },
                ["FullAttack.asset"] = new[]
                {
                    typeof(IsTriggeredOwnerTurnCondition),
                    typeof(CardPlayCondition),
                    typeof(PlayerCondition)
                },
                ["Palsy.asset"] = Array.Empty<Type>(),
                ["Poison.asset"] = new[]
                {
                    typeof(IsTriggeredOwnerTurnCondition)
                },
                ["QuickAttack.asset"] = new[]
                {
                    typeof(IsTriggeredOwnerTurnCondition),
                    typeof(CardPlayCondition)
                }
            };

            foreach (var pair in expectedTypesByAsset)
            {
                var path = $"Assets/ScriptableObjects/PlayerBuff/{pair.Key}";
                var asset = AssetDatabase.LoadAssetAtPath<PlayerBuffDataScriptable>(path);
                Assert.That(asset, Is.Not.Null, path);
                var actualTypes = asset.Data.BuffEffects
                    .SelectMany(effectPair => effectPair.Value ?? Array.Empty<ConditionalPlayerBuffEffect>())
                    .Where(effect => effect != null)
                    .SelectMany(effect => effect.Conditions)
                    .Select(condition => condition?.GetType())
                    .ToArray();

                Assert.That(actualTypes, Is.EqualTo(pair.Value), path);
                Assert.That(File.ReadAllText(path), Does.Not.Contain("IPlayerBuffCondition"), path);
            }
        }

        [Test]
        public void ConditionalBuffEffects_AssetRoundTrip_PreserveUnifiedConditions()
        {
            var playerPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/PlayerBuffConditionRoundTrip.asset");
            var characterPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/CharacterBuffConditionRoundTrip.asset");
            var cardPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/CardBuffConditionRoundTrip.asset");
            var playerAsset = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            var characterAsset = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();
            var cardAsset = ScriptableObject.CreateInstance<CardBuffScriptable>();
            try
            {
                playerAsset.Data.BuffEffects[GameTiming.AfterTurnStart] = new[]
                {
                    new ConditionalPlayerBuffEffect
                    {
                        Conditions =
                        {
                            new PlayerBuffCondition
                            {
                                PlayerBuff = new NoneBuff(),
                                Conditions =
                                {
                                    new PlayerBuffDataIdCondition
                                    {
                                        BuffId = BuffTestBuilder.PlayerBuffId
                                    }
                                }
                            }
                        }
                    }
                };
                characterAsset.Data.BuffEffects[GameTiming.AfterTurnStart] = new[]
                {
                    new ConditionalCharacterBuffEffect
                    {
                        Conditions = new ICondition[]
                        {
                            new CharacterBuffCollectionContainsIdCondition
                            {
                                CharacterBuffs = new NoneCharacterBuffs(),
                                BuffId = BuffTestBuilder.CharacterBuffId
                            }
                        }
                    }
                };
                cardAsset.Data.BuffEffects[GameTiming.AfterTurnStart] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Conditions =
                        {
                            new CardBuffCondition
                            {
                                CardBuff = new NoneCardBuff(),
                                Conditions =
                                {
                                    new CardBuffDataIdCondition
                                    {
                                        BuffId = BuffTestBuilder.CardBuffId
                                    }
                                }
                            }
                        }
                    }
                };

                AssetDatabase.CreateAsset(playerAsset, playerPath);
                AssetDatabase.CreateAsset(characterAsset, characterPath);
                AssetDatabase.CreateAsset(cardAsset, cardPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(playerAsset);
                Resources.UnloadAsset(characterAsset);
                Resources.UnloadAsset(cardAsset);
                AssetDatabase.ImportAsset(playerPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(characterPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(cardPath, ImportAssetOptions.ForceSynchronousImport);

                var loadedPlayer = AssetDatabase.LoadAssetAtPath<PlayerBuffDataScriptable>(playerPath);
                var loadedCharacter = AssetDatabase.LoadAssetAtPath<CharacterBuffDataScriptable>(characterPath);
                var loadedCard = AssetDatabase.LoadAssetAtPath<CardBuffScriptable>(cardPath);

                Assert.That(
                    loadedPlayer.Data.BuffEffects[GameTiming.AfterTurnStart][0].Conditions[0],
                    Is.TypeOf<PlayerBuffCondition>());
                Assert.That(
                    loadedCharacter.Data.BuffEffects[GameTiming.AfterTurnStart][0].Conditions[0],
                    Is.TypeOf<CharacterBuffCollectionContainsIdCondition>());
                Assert.That(
                    loadedCard.Data.BuffEffects[GameTiming.AfterTurnStart][0].Conditions[0],
                    Is.TypeOf<CardBuffCondition>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(playerPath);
                AssetDatabase.DeleteAsset(characterPath);
                AssetDatabase.DeleteAsset(cardPath);
            }
        }

        [Test]
        public void BuffCollections_WhenOwnersExist_ReturnCurrentRuntimeBuffs()
        {
            var setup = _BuildSetup();

            var playerBuffs = _PlayerBuffs().Eval(setup.Context);
            var characterBuffs = _CharacterBuffs().Eval(setup.Context);
            var cardBuffs = _CardBuffs(setup.Card).Eval(setup.Context);

            Assert.That(playerBuffs.Single(), Is.SameAs(setup.PlayerBuff));
            Assert.That(characterBuffs.Single(), Is.SameAs(setup.CharacterBuff));
            Assert.That(cardBuffs.Single(), Is.SameAs(setup.CardBuff));
        }

        [Test]
        public void BuffCollections_WhenOwnerTargetsAreMissing_ReturnEmptyCollections()
        {
            var setup = _BuildSetup();

            Assert.That(new PlayerBuffsOfPlayer { Player = new NonePlayer() }
                .Eval(setup.Context), Is.Empty);
            Assert.That(new CharacterBuffsOfCharacter { Character = new NoneCharacter() }
                .Eval(setup.Context), Is.Empty);
            Assert.That(new CardBuffsOfCard { Card = new NoneCard() }
                .Eval(setup.Context), Is.Empty);
        }

        [Test]
        public void BuffById_WhenIdsMatch_ReturnsExactRuntimeEntities()
        {
            var setup = _BuildSetup();

            var playerBuff = new PlayerBuffById
            {
                PlayerBuffs = _PlayerBuffs(),
                BuffId = BuffTestBuilder.PlayerBuffId
            }.Eval(setup.Context);
            var characterBuff = new CharacterBuffById
            {
                CharacterBuffs = _CharacterBuffs(),
                BuffId = BuffTestBuilder.CharacterBuffId
            }.Eval(setup.Context);
            var cardBuff = new CardBuffById
            {
                CardBuffs = _CardBuffs(setup.Card),
                BuffId = BuffTestBuilder.CardBuffId
            }.Eval(setup.Context);

            Assert.That(playerBuff.ValueOr((IPlayerBuffEntity)null), Is.SameAs(setup.PlayerBuff));
            Assert.That(characterBuff.ValueOr((ICharacterBuffEntity)null), Is.SameAs(setup.CharacterBuff));
            Assert.That(cardBuff.ValueOr((ICardBuffEntity)null), Is.SameAs(setup.CardBuff));
        }

        [Test]
        public void BuffById_WhenIdsDoNotMatch_ReturnsNone()
        {
            var setup = _BuildSetup();

            Assert.That(new PlayerBuffById
            {
                PlayerBuffs = _PlayerBuffs(),
                BuffId = "missing"
            }.Eval(setup.Context).HasValue, Is.False);
            Assert.That(new CharacterBuffById
            {
                CharacterBuffs = _CharacterBuffs(),
                BuffId = "missing"
            }.Eval(setup.Context).HasValue, Is.False);
            Assert.That(new CardBuffById
            {
                CardBuffs = _CardBuffs(setup.Card),
                BuffId = "missing"
            }.Eval(setup.Context).HasValue, Is.False);
        }

        [Test]
        public void BuffIntegerProperties_ReturnLevelsForAllThreeBuffTypes()
        {
            var setup = _BuildSetup(playerLevel: 2, characterLevel: 3, cardLevel: 4);

            var playerLevel = new PlayerBuffIntegerProperty
            {
                PlayerBuff = new PlayerBuffById
                {
                    PlayerBuffs = _PlayerBuffs(),
                    BuffId = BuffTestBuilder.PlayerBuffId
                },
                Property = PlayerBuffIntegerProperty.PlayerBuffIntegerValueType.Level
            }.Eval(setup.Context);
            var characterLevel = new CharacterBuffIntegerProperty
            {
                CharacterBuff = new CharacterBuffById
                {
                    CharacterBuffs = _CharacterBuffs(),
                    BuffId = BuffTestBuilder.CharacterBuffId
                },
                Property = CharacterBuffIntegerProperty.CharacterBuffIntegerValueType.Level
            }.Eval(setup.Context);
            var cardLevel = new CardBuffIntegerProperty
            {
                CardBuff = new CardBuffById
                {
                    CardBuffs = _CardBuffs(setup.Card),
                    BuffId = BuffTestBuilder.CardBuffId
                },
                Property = CardBuffIntegerProperty.CardBuffIntegerValueType.Level
            }.Eval(setup.Context);

            Assert.That(playerLevel.ValueOr(-1), Is.EqualTo(2));
            Assert.That(characterLevel.ValueOr(-1), Is.EqualTo(3));
            Assert.That(cardLevel.ValueOr(-1), Is.EqualTo(4));
        }

        [Test]
        public void BuffIntegerProperties_WhenTargetsAreMissing_ReturnNone()
        {
            var setup = _BuildSetup();

            Assert.That(new PlayerBuffIntegerProperty
            {
                PlayerBuff = new NoneBuff()
            }.Eval(setup.Context).HasValue, Is.False);
            Assert.That(new CharacterBuffIntegerProperty
            {
                CharacterBuff = new NoneCharacterBuff()
            }.Eval(setup.Context).HasValue, Is.False);
            Assert.That(new CardBuffIntegerProperty
            {
                CardBuff = new NoneCardBuff()
            }.Eval(setup.Context).HasValue, Is.False);
        }

        [Test]
        public void BuffIdAndExistenceConditions_ComposeForAllThreeBuffTypes()
        {
            var setup = _BuildSetup();

            var conditions = new ICondition[]
            {
                new PlayerBuffCondition
                {
                    PlayerBuff = new TriggeredPlayerBuff(),
                    Conditions = { new PlayerBuffDataIdCondition { BuffId = BuffTestBuilder.PlayerBuffId } }
                },
                new CharacterBuffCollectionContainsIdCondition
                {
                    CharacterBuffs = _CharacterBuffs(),
                    BuffId = BuffTestBuilder.CharacterBuffId
                },
                new CardBuffCollectionContainsIdCondition
                {
                    CardBuffs = _CardBuffs(setup.Card),
                    BuffId = BuffTestBuilder.CardBuffId
                }
            };

            Assert.That(conditions.All(condition => condition.Eval(setup.Context)), Is.True);
            Assert.That(new PlayerBuffCollectionContainsIdCondition
            {
                PlayerBuffs = _PlayerBuffs(),
                BuffId = "missing"
            }.Eval(setup.Context), Is.False);
        }

        [Test]
        public void TriggeredCharacterBuff_WhenContextMatches_ReturnsTriggeredBuff()
        {
            var setup = _BuildSetup();
            var context = setup.Context with
            {
                Triggered = new CharacterBuffTrigger(setup.Built.Ally.MainCharacter, setup.CharacterBuff)
            };

            var result = new TriggeredCharacterBuff().Eval(context);

            Assert.That(result.ValueOr((ICharacterBuffEntity)null), Is.SameAs(setup.CharacterBuff));
        }

        [Test]
        public void CardBuffQueries_WhenOverrideLayerIsActive_OnlySeeActiveLayer()
        {
            var setup = _BuildSetup();
            var baseBuff = setup.CardBuff;
            setup.Card.BuffManager.ReplaceOverrideLayer();
            var overrideBuff = BuffTestBuilder.CreateCardBuff(
                setup.Context,
                setup.Built.ContextManager.CardBuffLibrary,
                BuffTestBuilder.CardBuffId,
                level: 5);
            setup.Card.BuffManager.AddBuff(overrideBuff);

            var collection = _CardBuffs(setup.Card).Eval(setup.Context);
            var target = new CardBuffById
            {
                CardBuffs = _CardBuffs(setup.Card),
                BuffId = BuffTestBuilder.CardBuffId
            }.Eval(setup.Context);

            Assert.That(collection.Single(), Is.SameAs(overrideBuff));
            Assert.That(collection.Any(buff => ReferenceEquals(buff, baseBuff)), Is.False);
            Assert.That(target.ValueOr((ICardBuffEntity)null), Is.SameAs(overrideBuff));
        }

        [Test]
        public void BuffStateComposition_AssetRoundTrip_PreservesPolymorphicTypes()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/BuffStateCompositionRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "buff-state-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new CharacterBuffIntegerProperty
                    {
                        CharacterBuff = new CharacterBuffById
                        {
                            CharacterBuffs = _CharacterBuffs(),
                            BuffId = BuffTestBuilder.CharacterBuffId
                        },
                        Property = CharacterBuffIntegerProperty.CharacterBuffIntegerValueType.Level
                    }
                });
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "buff-state",
                    TransformKey = "buff-state",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new PlayerBuffCollectionContainsIdCondition
                        {
                            PlayerBuffs = _PlayerBuffs(),
                            BuffId = BuffTestBuilder.PlayerBuffId
                        },
                        new CardBuffCondition
                        {
                            CardBuff = new CardBuffById
                            {
                                CardBuffs = new CardBuffsOfCard { Card = new TriggeredCard() },
                                BuffId = BuffTestBuilder.CardBuffId
                            },
                            Conditions =
                            {
                                new CardBuffDataIdCondition { BuffId = BuffTestBuilder.CardBuffId }
                            }
                        }
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var value = ((DamageEffect)loaded.Data.Effects[0]).Value
                    as CharacterBuffIntegerProperty;
                var exists = loaded.Data.TransformRules[0].Conditions[0]
                    as PlayerBuffCollectionContainsIdCondition;
                var cardCondition = loaded.Data.TransformRules[0].Conditions[1]
                    as CardBuffCondition;

                Assert.That(value?.CharacterBuff, Is.TypeOf<CharacterBuffById>());
                Assert.That(
                    ((CharacterBuffById)value.CharacterBuff).CharacterBuffs,
                    Is.TypeOf<CharacterBuffsOfCharacter>());
                Assert.That(exists?.PlayerBuffs, Is.TypeOf<PlayerBuffsOfPlayer>());
                Assert.That(cardCondition?.CardBuff, Is.TypeOf<CardBuffById>());
                Assert.That(cardCondition?.Conditions[0], Is.TypeOf<CardBuffDataIdCondition>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void Validator_WithEmptyBuffIdsAndInvalidProperties_ReportsErrors()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                card.Data.ID = "invalid-buff-state";
                card.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new MinimumInteger
                    {
                        Values =
                        {
                            new PlayerBuffIntegerProperty
                            {
                                PlayerBuff = new PlayerBuffById
                                {
                                    PlayerBuffs = _PlayerBuffs(),
                                    BuffId = string.Empty
                                },
                                Property = (PlayerBuffIntegerProperty.PlayerBuffIntegerValueType)999
                            },
                            new CharacterBuffIntegerProperty
                            {
                                CharacterBuff = new CharacterBuffById
                                {
                                    CharacterBuffs = _CharacterBuffs(),
                                    BuffId = string.Empty
                                },
                                Property = (CharacterBuffIntegerProperty.CharacterBuffIntegerValueType)999
                            },
                            new CardBuffIntegerProperty
                            {
                                CardBuff = new CardBuffById
                                {
                                    CardBuffs = new NoneCardBuffs(),
                                    BuffId = string.Empty
                                },
                                Property = (CardBuffIntegerProperty.CardBuffIntegerValueType)999
                            }
                        }
                    }
                });
                _SetCatalogCard(catalog, card);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(errors, Has.Some.Contains("PlayerBuffIntegerProperty.Property 無效：999"));
                Assert.That(errors, Has.Some.Contains("CharacterBuffIntegerProperty.Property 無效：999"));
                Assert.That(errors, Has.Some.Contains("CardBuffIntegerProperty.Property 無效：999"));
                Assert.That(errors, Has.Some.Contains("PlayerBuff 查詢 BuffId 為空"));
                Assert.That(errors, Has.Some.Contains("CharacterBuff 查詢 BuffId 為空"));
                Assert.That(errors, Has.Some.Contains("CardBuff 查詢 BuffId 為空"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(card);
            }
        }

        private static BuffQuerySetup _BuildSetup(
            int playerLevel = 1,
            int characterLevel = 1,
            int cardLevel = 1)
        {
            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.None,
                null);
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(cardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = new TriggerContext(
                built.Manager,
                new PlayerBuffTrigger(built.Ally, BuffTestBuilder.CreatePlayerBuff()),
                new UpdateTimingAction(GameTiming.AfterTurnStart, SystemSource.Instance));
            var playerBuff = BuffTestBuilder.CreatePlayerBuff(level: playerLevel);
            var characterBuff = BuffTestBuilder.CreateCharacterBuff(level: characterLevel);
            var cardBuff = BuffTestBuilder.CreateCardBuff(
                context,
                built.ContextManager.CardBuffLibrary,
                level: cardLevel);
            built.Ally.BuffManager.AddBuff(playerBuff);
            built.Ally.MainCharacter.BuffManager.AddBuff(characterBuff);
            card.BuffManager.AddBuff(cardBuff);
            context = context with { Triggered = new PlayerBuffTrigger(built.Ally, playerBuff) };

            return new BuffQuerySetup(
                built,
                context,
                card,
                playerBuff,
                characterBuff,
                cardBuff);
        }

        private static PlayerBuffsOfPlayer _PlayerBuffs()
        {
            return new PlayerBuffsOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally }
            };
        }

        private static CharacterBuffsOfCharacter _CharacterBuffs()
        {
            return new CharacterBuffsOfCharacter
            {
                Character = new MainCharacterOfPlayer
                {
                    Player = new PlayerByFaction { Faction = Faction.Ally }
                }
            };
        }

        private static CardBuffsOfCard _CardBuffs(ICardEntity card)
        {
            return new CardBuffsOfCard { Card = new FixedCardTarget(card) };
        }

        private static void _SetCatalogCard(
            GameContentCatalog catalog,
            CardDataScriptableBase card)
        {
            var serializedCatalog = new SerializedObject(catalog);
            var cardAssets = serializedCatalog.FindProperty("_cardAssets");
            cardAssets.arraySize = 1;
            cardAssets.GetArrayElementAtIndex(0).objectReferenceValue = card;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed record BuffQuerySetup(
            BuiltGameplay Built,
            TriggerContext Context,
            ICardEntity Card,
            IPlayerBuffEntity PlayerBuff,
            ICharacterBuffEntity CharacterBuff,
            ICardBuffEntity CardBuff);

        [Serializable]
        private sealed class FixedCardTarget : ITargetCardValue
        {
            private readonly ICardEntity _card;

            public FixedCardTarget(ICardEntity card)
            {
                _card = card;
            }

            public Optional.Option<ICardEntity> Eval(TriggerContext triggerContext)
            {
                return _card.SomeNotNull();
            }
        }
    }
}
