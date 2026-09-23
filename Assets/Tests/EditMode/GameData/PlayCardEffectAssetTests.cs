using System;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class PlayCardEffectAssetTests
    {
        [Test]
        public void FourSources_RoundTripPreservesPlayCardEffectAndNestedTargets()
        {
            var cardPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/PlayCardCardRoundTrip.asset");
            var playerPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/PlayCardPlayerBuffRoundTrip.asset");
            var characterPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/PlayCardCharacterBuffRoundTrip.asset");
            var cardBuffPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/PlayCardCardBuffRoundTrip.asset");
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var player = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            var character = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();
            var cardBuff = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                card.Data.ID = "roundtrip-play-card";
                card.Data.SubSelects.Add(new ExistCardSelectionGroup
                {
                    Id = "chosen",
                    CardCandidates = new SingleCardCollection { TargetCard = new TriggeredCard() },
                    SelectCount = new ConstInteger { Value = 1 },
                    IsMustSelect = new TrueValue()
                });
                card.Data.Effects.Add(new PlayCardEffect
                {
                    TargetCards = new SubSelectedCardCollection { SelectionId = "chosen" }
                });
                player.Data.ID = "roundtrip-player-buff";
                player.Data.MaxLevel = 1;
                player.Data.LifeTimeData = new AlwaysLifeTimePlayerBuffData();
                player.Data.BuffEffects[GameTiming.AfterTurnEnd] = new[]
                {
                    new ConditionalPlayerBuffEffect
                    {
                        Effect = new PlayCardEffect
                        {
                            TargetCards = new SingleCardCollection { TargetCard = new TriggeredCard() }
                        }
                    }
                };
                character.Data.ID = "roundtrip-character-buff";
                character.Data.MaxLevel = 1;
                character.Data.LifeTimeData = new AlwaysLifeTimeCharacterBuffData();
                character.Data.BuffEffects[GameTiming.AfterTurnEnd] = new[]
                {
                    new ConditionalCharacterBuffEffect
                    {
                        Effect = new PlayCardEffect
                        {
                            TargetCards = new CardsOfPlayer
                            {
                                Player = new PlayerByFaction { Faction = Faction.Ally },
                                Zone = CardCollectionType.HandCard
                            }
                        }
                    }
                };
                cardBuff.Data.ID = "roundtrip-card-buff";
                cardBuff.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                cardBuff.Data.Effects[CardTriggeredTiming.EffectPlayed] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Effect = new PlayCardEffect
                        {
                            TargetCards = new SingleCardCollection { TargetCard = new TriggeredCard() }
                        }
                    }
                };

                AssetDatabase.CreateAsset(card, cardPath);
                AssetDatabase.CreateAsset(player, playerPath);
                AssetDatabase.CreateAsset(character, characterPath);
                AssetDatabase.CreateAsset(cardBuff, cardBuffPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(card);
                Resources.UnloadAsset(player);
                Resources.UnloadAsset(character);
                Resources.UnloadAsset(cardBuff);
                foreach (var path in new[] { cardPath, playerPath, characterPath, cardBuffPath })
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var loadedCard = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(cardPath);
                var loadedPlayer = AssetDatabase.LoadAssetAtPath<PlayerBuffDataScriptable>(playerPath);
                var loadedCharacter = AssetDatabase.LoadAssetAtPath<CharacterBuffDataScriptable>(characterPath);
                var loadedCardBuff = AssetDatabase.LoadAssetAtPath<CardBuffScriptable>(cardBuffPath);
                Assert.That(((SubSelectedCardCollection)((PlayCardEffect)loadedCard.Data.Effects[0]).TargetCards).SelectionId,
                    Is.EqualTo("chosen"));
                Assert.That(loadedCard.Data.SubSelects[0], Is.TypeOf<ExistCardSelectionGroup>());
                Assert.That(((PlayCardEffect)loadedPlayer.Data.BuffEffects[GameTiming.AfterTurnEnd][0].Effect).TargetCards,
                    Is.TypeOf<SingleCardCollection>());
                Assert.That(((PlayCardEffect)loadedCharacter.Data.BuffEffects[GameTiming.AfterTurnEnd][0].Effect).TargetCards,
                    Is.TypeOf<CardsOfPlayer>());
                Assert.That(((PlayCardEffect)loadedCardBuff.Data.Effects[CardTriggeredTiming.EffectPlayed][0].Effect).TargetCards,
                    Is.TypeOf<SingleCardCollection>());

                _SetCatalogArray(catalog, "_cardAssets", loadedCard);
                _SetCatalogArray(catalog, "_playerBuffAssets", loadedPlayer);
                _SetCatalogArray(catalog, "_characterBuffAssets", loadedCharacter);
                _SetCatalogArray(catalog, "_cardBuffAssets", loadedCardBuff);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
                Assert.That(GameDataValidator.ValidateEffectResolvers(catalog), Is.Empty);
                Assert.That(GameDataValidator.ValidateEffectCommandHandlers(), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                foreach (var path in new[] { cardPath, playerPath, characterPath, cardBuffPath })
                    AssetDatabase.DeleteAsset(path);
            }
        }

        private static void _SetCatalogArray(
            GameContentCatalog catalog, string propertyName, UnityEngine.Object asset)
        {
            var serializedCatalog = new SerializedObject(catalog);
            var property = serializedCatalog.FindProperty(propertyName);
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = asset;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
