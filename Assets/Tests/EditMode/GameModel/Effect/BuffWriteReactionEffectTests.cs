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
    public sealed class BuffWriteReactionEffectTests
    {
        private const string ExistingPlayerBuffId = "existing-player-buff";
        private const string CreatedPlayerBuffId = "created-player-buff";
        private const string SourceCardBuffId = "source-card-buff";
        private const string CreatedCardBuffId = "created-card-buff";

        public static IEnumerable<TestCaseData> CoreBuffWriteEffects()
        {
            yield return new TestCaseData(
                typeof(AddPlayerBuffEffect),
                typeof(AddPlayerBuffEffectCommand));
            yield return new TestCaseData(
                typeof(ModifyPlayerBuffLevelEffect),
                typeof(ModifyPlayerBuffLevelEffectCommand));
            yield return new TestCaseData(
                typeof(RemovePlayerBuffEffect),
                typeof(RemovePlayerBuffEffectCommand));
            yield return new TestCaseData(
                typeof(AddCardBuffEffect),
                typeof(AddCardBuffEffectCommand));
            yield return new TestCaseData(
                typeof(RemoveCardBuffEffect),
                typeof(RemoveCardBuffEffectCommand));
        }

        public static IEnumerable<TestCaseData> ReactionSources()
        {
            yield return new TestCaseData(ReactionSource.PlayerBuff);
            yield return new TestCaseData(ReactionSource.CharacterBuff);
            yield return new TestCaseData(ReactionSource.CardBuff);
        }

        [TestCaseSource(nameof(CoreBuffWriteEffects))]
        public void CoreBuffWriteEffect_FourSources_ResolveTheSameCommandPipeline(
            Type effectType,
            Type commandType)
        {
            var setup = _CreateSetup(ReactionSource.PlayerBuff);
            var effect = _CreateEffect(effectType);

            _AssertSingleCommand(
                EffectDataResolver.ResolveCardEffect(setup.Context, effect),
                commandType);
            _AssertSingleCommand(
                EffectDataResolver.ResolvePlayerBuffEffect(
                    setup.Context,
                    (IPlayerBuffEffect)effect),
                commandType);

            setup = _CreateSetup(ReactionSource.CharacterBuff);
            effect = _CreateEffect(effectType);
            _AssertSingleCommand(
                EffectDataResolver.ResolveCharacterBuffEffect(
                    setup.Context,
                    (ICharacterBuffEffect)effect),
                commandType);

            setup = _CreateSetup(ReactionSource.CardBuff);
            effect = _CreateEffect(effectType);
            _AssertSingleCommand(
                EffectDataResolver.ResolveCardBuffEffect(
                    setup.Context,
                    (ICardBuffEffect)effect),
                commandType);
        }

        [TestCaseSource(nameof(ReactionSources))]
        public void AddBuffEffects_ThreeReactionSources_PreserveTriggeredCaster(
            ReactionSource source)
        {
            var setup = _CreateSetup(source);
            var addPlayerBuff = _CreateEffect(typeof(AddPlayerBuffEffect));
            var addCardBuff = _CreateEffect(typeof(AddCardBuffEffect));

            _RunReactionEffect(setup, addPlayerBuff);
            _RunReactionEffect(setup, addCardBuff);

            var playerBuff = setup.Built.Ally.BuffManager.Buffs.Single(buff =>
                buff.PlayerBuffDataId == CreatedPlayerBuffId);
            var cardBuff = setup.Card.BuffManager.Buffs.Single(buff =>
                buff.CardBuffDataID == CreatedCardBuffId);

            Assert.That(
                playerBuff.Caster.ValueOr((IPlayerEntity)null),
                Is.SameAs(setup.Built.Enemy));
            Assert.That(
                cardBuff.Caster.ValueOr((IPlayerEntity)null),
                Is.SameAs(setup.Built.Enemy));
        }

        [Test]
        public void BuffWriteEffects_BuffAssetsRoundTrip_KeepSourceContractsAndResolvers()
        {
            var playerPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/PlayerBuffWriteReaction.asset");
            var characterPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/CharacterBuffWriteReaction.asset");
            var cardPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/CardBuffWriteReaction.asset");
            var playerAsset = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            var characterAsset = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();
            var cardAsset = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                _ConfigureRoundTripAssets(playerAsset, characterAsset, cardAsset);
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
                    loadedPlayer.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect,
                    Is.TypeOf<AddPlayerBuffEffect>());
                Assert.That(
                    loadedPlayer.Data.BuffEffects[GameTiming.AfterExecuteEnd][1].Effect,
                    Is.TypeOf<RemoveCardBuffEffect>());
                Assert.That(
                    loadedCharacter.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect,
                    Is.TypeOf<ModifyPlayerBuffLevelEffect>());
                Assert.That(
                    loadedCharacter.Data.BuffEffects[GameTiming.AfterExecuteEnd][1].Effect,
                    Is.TypeOf<AddCardBuffEffect>());
                Assert.That(
                    loadedCard.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect,
                    Is.TypeOf<RemovePlayerBuffEffect>());

                _SetCatalogArray(catalog, "_playerBuffAssets", loadedPlayer);
                _SetCatalogArray(catalog, "_characterBuffAssets", loadedCharacter);
                _SetCatalogArray(catalog, "_cardBuffAssets", loadedCard);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
                Assert.That(GameDataValidator.ValidateEffectResolvers(catalog), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                AssetDatabase.DeleteAsset(playerPath);
                AssetDatabase.DeleteAsset(characterPath);
                AssetDatabase.DeleteAsset(cardPath);
            }
        }

        [Test]
        public void ValidateReferenceIds_BuffWriteEffects_ChecksAllThreeBuffSources()
        {
            var playerAsset = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            var characterAsset = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();
            var cardAsset = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                playerAsset.Data.ID = "reference-player";
                playerAsset.Data.MaxLevel = 1;
                playerAsset.Data.LifeTimeData = new AlwaysLifeTimePlayerBuffData();
                playerAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalPlayerBuffEffect
                    {
                        Effect = new AddPlayerBuffEffect
                        {
                            Targets = new NonePlayers(),
                            BuffId = "missing-player-buff",
                            Level = new ConstInteger { Value = 1 }
                        }
                    }
                };
                characterAsset.Data.ID = "reference-character";
                characterAsset.Data.MaxLevel = 1;
                characterAsset.Data.LifeTimeData = new AlwaysLifeTimeCharacterBuffData();
                characterAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalCharacterBuffEffect
                    {
                        Effect = new AddCardBuffEffect
                        {
                            TargetCards = new SingleCardCollection { TargetCard = new NoneCard() },
                            AddCardBuffDatas =
                            {
                                new AddCardBuffData
                                {
                                    CardBuffId = "missing-card-buff",
                                    Level = new ConstInteger { Value = 1 }
                                }
                            }
                        }
                    }
                };
                cardAsset.Data.ID = "reference-card";
                cardAsset.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                cardAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Effect = new RemovePlayerBuffEffect
                        {
                            Targets = new NonePlayers(),
                            BuffId = "missing-player-buff"
                        }
                    },
                    new ConditionalCardBuffEffect
                    {
                        Effect = new RemoveCardBuffEffect
                        {
                            TargetCards = new SingleCardCollection { TargetCard = new NoneCard() },
                            BuffId = "missing-card-buff"
                        }
                    }
                };

                _SetCatalogArray(catalog, "_playerBuffAssets", playerAsset);
                _SetCatalogArray(catalog, "_characterBuffAssets", characterAsset);
                _SetCatalogArray(catalog, "_cardBuffAssets", cardAsset);
                var errors = GameDataValidator.ValidateReferenceIds(catalog);

                Assert.That(errors, Has.Some.Contains("AddPlayerBuffEffect.BuffId 指向不存在的 ID：missing-player-buff"));
                Assert.That(errors, Has.Some.Contains("AddCardBuffEffect.AddCardBuffDatas.CardBuffId 指向不存在的 ID：missing-card-buff"));
                Assert.That(errors, Has.Some.Contains("RemovePlayerBuffEffect.BuffId 指向不存在的 ID：missing-player-buff"));
                Assert.That(errors, Has.Some.Contains("RemoveCardBuffEffect.BuffId 指向不存在的 ID：missing-card-buff"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(playerAsset);
                UnityEngine.Object.DestroyImmediate(characterAsset);
                UnityEngine.Object.DestroyImmediate(cardAsset);
            }
        }

        private static void _ConfigureRoundTripAssets(
            PlayerBuffDataScriptable playerAsset,
            CharacterBuffDataScriptable characterAsset,
            CardBuffScriptable cardAsset)
        {
            playerAsset.Data.ID = "round-trip-player";
            playerAsset.Data.MaxLevel = 1;
            playerAsset.Data.LifeTimeData = new AlwaysLifeTimePlayerBuffData();
            playerAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
            {
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new AddPlayerBuffEffect
                    {
                        Targets = new NonePlayers(),
                        BuffId = "round-trip-player",
                        Level = new ConstInteger { Value = 1 }
                    }
                },
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new RemoveCardBuffEffect
                    {
                        TargetCards = new SingleCardCollection { TargetCard = new NoneCard() },
                        BuffId = "round-trip-card"
                    }
                }
            };
            characterAsset.Data.ID = "round-trip-character";
            characterAsset.Data.MaxLevel = 1;
            characterAsset.Data.LifeTimeData = new AlwaysLifeTimeCharacterBuffData();
            characterAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
            {
                new ConditionalCharacterBuffEffect
                {
                    Conditions = new ICondition[] { new ConstCondition { Value = true } },
                    Effect = new ModifyPlayerBuffLevelEffect
                    {
                        Targets = new NonePlayers(),
                        BuffId = "round-trip-player",
                        DeltaLevel = new ConstInteger { Value = 1 }
                    }
                },
                new ConditionalCharacterBuffEffect
                {
                    Conditions = new ICondition[] { new ConstCondition { Value = true } },
                    Effect = new AddCardBuffEffect
                    {
                        TargetCards = new SingleCardCollection { TargetCard = new NoneCard() },
                        AddCardBuffDatas =
                        {
                            new AddCardBuffData
                            {
                                CardBuffId = "round-trip-card",
                                Level = new ConstInteger { Value = 1 }
                            }
                        }
                    }
                }
            };
            cardAsset.Data.ID = "round-trip-card";
            cardAsset.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
            cardAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
            {
                new ConditionalCardBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new RemovePlayerBuffEffect
                    {
                        Targets = new NonePlayers(),
                        BuffId = "round-trip-player"
                    }
                }
            };
        }

        private static void _SetCatalogArray<TAsset>(
            GameContentCatalog catalog,
            string fieldName,
            TAsset asset)
            where TAsset : UnityEngine.Object
        {
            typeof(GameContentCatalog)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(catalog, new[] { asset });
        }

        private static BuffWriteReactionSetup _CreateSetup(ReactionSource source)
        {
            var createdPlayerBuffData = new PlayerBuffData
            {
                ID = CreatedPlayerBuffId,
                MaxLevel = 9,
                LifeTimeData = new AlwaysLifeTimePlayerBuffData()
            };
            var sourceCardBuffData = new CardBuffData
            {
                ID = SourceCardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var createdCardBuffData = new CardBuffData
            {
                ID = CreatedCardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(createdPlayerBuffData)
                .WithCardBuff(sourceCardBuffData)
                .WithCardBuff(createdCardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff(
                ExistingPlayerBuffId,
                built.Enemy));
            var createCardBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var sourceCardBuff = BuffTestBuilder.CreateCardBuff(
                createCardBuffContext,
                built.ContextManager.CardBuffLibrary,
                SourceCardBuffId,
                built.Enemy);
            card.BuffManager.AddBuff(sourceCardBuff);

            var context = source switch
            {
                ReactionSource.PlayerBuff => new TriggerContext(
                    built.Manager,
                    new PlayerBuffTrigger(
                        built.Ally,
                        BuffTestBuilder.CreatePlayerBuff("source-player-buff", built.Enemy)),
                    new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance)),
                ReactionSource.CharacterBuff => new TriggerContext(
                    built.Manager,
                    new CharacterBuffTrigger(
                        built.Ally.MainCharacter,
                        BuffTestBuilder.CreateCharacterBuff("source-character-buff", built.Enemy)),
                    new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance)),
                ReactionSource.CardBuff => new TriggerContext(
                    built.Manager,
                    new CardBuffTrigger(card, sourceCardBuff),
                    new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance)),
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };

            return new BuffWriteReactionSetup(built, card, context, source);
        }

        private static ICardEffect _CreateEffect(Type effectType)
        {
            var playerTargets = new SinglePlayerCollection
            {
                Target = new PlayerByFaction { Faction = Faction.Ally }
            };
            var cardTargets = new CardsOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally },
                Zone = CardCollectionType.HandCard
            };

            if (effectType == typeof(AddPlayerBuffEffect))
            {
                return new AddPlayerBuffEffect
                {
                    Targets = playerTargets,
                    BuffId = CreatedPlayerBuffId,
                    Level = new ConstInteger { Value = 1 }
                };
            }
            if (effectType == typeof(ModifyPlayerBuffLevelEffect))
            {
                return new ModifyPlayerBuffLevelEffect
                {
                    Targets = playerTargets,
                    BuffId = ExistingPlayerBuffId,
                    DeltaLevel = new ConstInteger { Value = 1 }
                };
            }
            if (effectType == typeof(RemovePlayerBuffEffect))
            {
                return new RemovePlayerBuffEffect
                {
                    Targets = playerTargets,
                    BuffId = ExistingPlayerBuffId
                };
            }
            if (effectType == typeof(AddCardBuffEffect))
            {
                return new AddCardBuffEffect
                {
                    TargetCards = cardTargets,
                    AddCardBuffDatas =
                    {
                        new AddCardBuffData
                        {
                            CardBuffId = CreatedCardBuffId,
                            Level = new ConstInteger { Value = 1 }
                        }
                    }
                };
            }
            if (effectType == typeof(RemoveCardBuffEffect))
            {
                return new RemoveCardBuffEffect
                {
                    TargetCards = cardTargets,
                    BuffId = SourceCardBuffId
                };
            }

            throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null);
        }

        private static void _RunReactionEffect(
            BuffWriteReactionSetup setup,
            ICardEffect effect)
        {
            var runner = new EffectQueueRunner();
            switch (setup.Source)
            {
                case ReactionSource.PlayerBuff:
                    runner.Enqueue(new PlayerBuffEffectQueueItem(
                        setup.Context,
                        (IPlayerBuffEffect)effect));
                    break;
                case ReactionSource.CharacterBuff:
                    runner.Enqueue(new CharacterBuffEffectQueueItem(
                        setup.Context,
                        (ICharacterBuffEffect)effect));
                    break;
                case ReactionSource.CardBuff:
                    runner.Enqueue(new CardBuffEffectQueueItem(
                        setup.Context,
                        (ICardBuffEffect)effect));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            runner.RunToCompletion();
        }

        private static void _AssertSingleCommand(EffectCommandSet commandSet, Type commandType)
        {
            Assert.That(commandSet.Commands, Has.Count.EqualTo(1));
            Assert.That(commandSet.Commands.Single().GetType(), Is.EqualTo(commandType));
        }

        public enum ReactionSource
        {
            PlayerBuff,
            CharacterBuff,
            CardBuff
        }

        private sealed record BuffWriteReactionSetup(
            BuiltGameplay Built,
            ICardEntity Card,
            TriggerContext Context,
            ReactionSource Source);
    }
}
