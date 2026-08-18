using System;
using System.Collections.Generic;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class GameContentIntegrityValidationTests
    {
        [Test]
        public void DefaultCatalog_HasValidNestedContent()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                ProjectAssetPaths.GameContent.Catalog);

            Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
        }

        [Test]
        public void ValidateNestedContent_ReportsDeepTargetValueAndCollectionNulls()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();

            try
            {
                card.Data.ID = "invalid-deep-card";
                card.Data.Effects.Add(new DamageEffect
                {
                    Targets = new SingleCharacterCollection(),
                    Value = null
                });
                card.Data.TriggeredEffects.Add(null);
                _SetCatalogArray(catalog, "_cardAssets", card);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(errors, Has.Some.Contains("Effects[0].Targets.Target 為空"));
                Assert.That(errors, Has.Some.Contains("Effects[0].Value 為空"));
                Assert.That(errors, Has.Some.Contains("TriggeredEffects[0] 為空"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(card);
            }
        }

        [Test]
        public void ValidateNestedContent_ReportsLifeTimeLevelAndSessionReferenceErrors()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var playerBuff = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();

            try
            {
                playerBuff.Data.ID = "invalid-session-buff";
                playerBuff.Data.MaxLevel = 0;
                playerBuff.Data.LifeTimeData = null;
                playerBuff.Data.Sessions["boolean-session"] = new SessionBoolean();
                playerBuff.Data.BuffEffects[GameTiming.BeforeTurnEnd] = new[]
                {
                    new ConditionalPlayerBuffEffect
                    {
                        Conditions = new List<IPlayerBuffCondition>
                        {
                            new PlayerBuffCondition
                            {
                                PlayerBuff = new TriggeredPlayerBuff(),
                                Conditions = new List<IPlayerBuffValueCondition>
                                {
                                    new PlayerBuffSessionCondition
                                    {
                                        SessionKey = "missing-session",
                                        Conditions = new List<IReactionSessionValueCondition>()
                                    }
                                }
                            }
                        },
                        Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                        {
                            Value = new PlayerBuffSessionInteger
                            {
                                PlayerBuff = new TriggeredPlayerBuff(),
                                SessionIntegerId = "boolean-session"
                            }
                        }
                    }
                };
                _SetCatalogArray(catalog, "_playerBuffAssets", playerBuff);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(errors, Has.Some.Contains("LifeTimeData 為空"));
                Assert.That(errors, Has.Some.Contains("MaxLevel 必須大於 0"));
                Assert.That(errors, Has.Some.Contains("引用不存在的 Session：missing-session"));
                Assert.That(errors, Has.Some.Contains("必須引用 SessionInteger：boolean-session"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(playerBuff);
            }
        }

        [Test]
        public void ValidateLocalization_ReportsMissingAndDuplicateKeys()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var cardBuff = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var localizationData = ScriptableObject.CreateInstance<ExcelDatas>();

            try
            {
                card.Data.ID = "missing-card";
                cardBuff.Data.ID = "missing-card-buff";
                _SetCatalogArray(catalog, "_cardAssets", card);
                _SetCatalogArray(catalog, "_cardBuffAssets", cardBuff);

                localizationData.LocalizeCard = new List<LocalizeExcelTitleData>
                {
                    _CreateTitleInfoRow("duplicate"),
                    _CreateTitleInfoRow("duplicate")
                };
                localizationData.LocalizeCardBuff = new List<LocalizeExcelTitleData>();
                localizationData.LocalizePlayerBuff = new List<LocalizeExcelTitleData>();
                localizationData.LocalizePlayer = new List<LocalizeExcelTitleData>();
                localizationData.LocalizeKeyWord = new List<LocalizeExcelTitleData>();
                localizationData.LocalizeUI = new List<LocalizeExcelData>();

                var errors = GameDataValidator.ValidateLocalization(
                    catalog,
                    localizationData,
                    new[]
                    {
                        new PlayerData
                        {
                            ID = "player",
                            NameKey = "missing-player"
                        }
                    });

                Assert.That(errors, Has.Some.Contains("LocalizeCard 的 Id 重複：duplicate"));
                Assert.That(errors, Has.Some.Contains("CardData[missing-card]"));
                Assert.That(errors, Has.Some.Contains("CardBuffData[missing-card-buff]"));
                Assert.That(errors, Has.Some.Contains("PlayerData[player].NameKey"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(card);
                UnityEngine.Object.DestroyImmediate(cardBuff);
                UnityEngine.Object.DestroyImmediate(localizationData);
            }
        }

        private static LocalizeExcelTitleData _CreateTitleInfoRow(string id)
        {
            return new LocalizeExcelTitleData
            {
                Id = id,
                Title = "標題",
                Info = "說明"
            };
        }

        private static void _SetCatalogArray(
            GameContentCatalog catalog,
            string propertyName,
            UnityEngine.Object asset)
        {
            var serializedCatalog = new SerializedObject(catalog);
            var arrayProperty = serializedCatalog.FindProperty(propertyName);
            arrayProperty.arraySize = 1;
            arrayProperty.GetArrayElementAtIndex(0).objectReferenceValue = asset;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
