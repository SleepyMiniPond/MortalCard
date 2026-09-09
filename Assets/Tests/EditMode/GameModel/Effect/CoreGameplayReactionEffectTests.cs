using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class CoreGameplayReactionEffectTests
    {
        public static IEnumerable<TestCaseData> CoreGameplayEffects()
        {
            yield return new TestCaseData(typeof(ShieldEffect), typeof(ShieldEffectCommand));
            yield return new TestCaseData(typeof(HealEffect), typeof(HealEffectCommand));
            yield return new TestCaseData(typeof(GainEnergyEffect), typeof(GainEnergyEffectCommand));
            yield return new TestCaseData(typeof(LoseEnegyEffect), typeof(LoseEnergyEffectCommand));
            yield return new TestCaseData(typeof(DrawCardEffect), typeof(DrawCardEffectCommand));
            yield return new TestCaseData(typeof(IncreaseDispositionEffect), typeof(IncreaseDispositionEffectCommand));
            yield return new TestCaseData(typeof(DecreaseDispositionEffect), typeof(DecreaseDispositionEffectCommand));
        }

        [TestCaseSource(nameof(CoreGameplayEffects))]
        public void CoreGameplayEffect_FourSources_ResolveTheSameCommandPipeline(
            Type effectType,
            Type commandType)
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
                built.ContextManager.CardBuffLibrary);
            card.BuffManager.AddBuff(cardBuff);

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
                new CardBuffTrigger(card, cardBuff),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));

            var effect = _CreateEffect(effectType);

            _AssertSingleCommand(
                EffectDataResolver.ResolveCardEffect(playerContext, effect),
                commandType);
            _AssertSingleCommand(
                EffectDataResolver.ResolvePlayerBuffEffect(
                    playerContext,
                    (IPlayerBuffEffect)effect),
                commandType);
            _AssertSingleCommand(
                EffectDataResolver.ResolveCharacterBuffEffect(
                    characterContext,
                    (ICharacterBuffEffect)effect),
                commandType);
            _AssertSingleCommand(
                EffectDataResolver.ResolveCardBuffEffect(
                    cardContext,
                    (ICardBuffEffect)effect),
                commandType);
        }

        [Test]
        public void CoreGameplayEffects_BuffAssetsRoundTrip_KeepSourceContractsAndPassValidator()
        {
            var playerPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/PlayerBuffCoreGameplayEffect.asset");
            var characterPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/CharacterBuffCoreGameplayEffect.asset");
            var cardPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/CardBuffCoreGameplayEffect.asset");
            var playerAsset = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            var characterAsset = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();
            var cardAsset = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                playerAsset.Data.ID = "player-buff-core-gameplay-effect";
                playerAsset.Data.MaxLevel = 1;
                playerAsset.Data.LifeTimeData = new AlwaysLifeTimePlayerBuffData();
                playerAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalPlayerBuffEffect
                    {
                        Conditions = { new ConstCondition { Value = true } },
                        Effect = new ShieldEffect
                        {
                            Targets = new NoneCharacters(),
                            Value = new ConstInteger { Value = 1 }
                        }
                    }
                };
                characterAsset.Data.ID = "character-buff-core-gameplay-effect";
                characterAsset.Data.MaxLevel = 1;
                characterAsset.Data.LifeTimeData = new AlwaysLifeTimeCharacterBuffData();
                characterAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalCharacterBuffEffect
                    {
                        Conditions = new ICondition[] { new ConstCondition { Value = true } },
                        Effect = new GainEnergyEffect
                        {
                            Targets = new NonePlayers(),
                            Value = new ConstInteger { Value = 1 }
                        }
                    }
                };
                cardAsset.Data.ID = "card-buff-core-gameplay-effect";
                cardAsset.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                cardAsset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Conditions = { new ConstCondition { Value = true } },
                        Effect = new IncreaseDispositionEffect
                        {
                            Targets = new NonePlayers(),
                            Value = new ConstInteger { Value = 1 }
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
                    loadedPlayer.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect,
                    Is.TypeOf<ShieldEffect>());
                Assert.That(
                    loadedCharacter.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect,
                    Is.TypeOf<GainEnergyEffect>());
                Assert.That(
                    loadedCard.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect,
                    Is.TypeOf<IncreaseDispositionEffect>());

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

        private static ICardEffect _CreateEffect(Type effectType)
        {
            var playerTargets = new SinglePlayerCollection
            {
                Target = new PlayerByFaction { Faction = Faction.Ally }
            };
            var characterTargets = new SingleCharacterCollection
            {
                Target = new MainCharacterOfPlayer
                {
                    Player = new PlayerByFaction { Faction = Faction.Ally }
                }
            };
            var value = new ConstInteger { Value = 1 };

            if (effectType == typeof(ShieldEffect))
                return new ShieldEffect { Targets = characterTargets, Value = value };
            if (effectType == typeof(HealEffect))
                return new HealEffect { Targets = characterTargets, Value = value };
            if (effectType == typeof(GainEnergyEffect))
                return new GainEnergyEffect { Targets = playerTargets, Value = value };
            if (effectType == typeof(LoseEnegyEffect))
                return new LoseEnegyEffect { Targets = playerTargets, Value = value };
            if (effectType == typeof(DrawCardEffect))
                return new DrawCardEffect { Targets = playerTargets, Value = value };
            if (effectType == typeof(IncreaseDispositionEffect))
                return new IncreaseDispositionEffect { Targets = playerTargets, Value = value };
            if (effectType == typeof(DecreaseDispositionEffect))
                return new DecreaseDispositionEffect { Targets = playerTargets, Value = value };

            throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null);
        }

        private static void _AssertSingleCommand(EffectCommandSet commandSet, Type commandType)
        {
            Assert.That(commandSet.Commands, Has.Count.EqualTo(1));
            Assert.That(commandSet.Commands.Single().GetType(), Is.EqualTo(commandType));
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
    }
}
