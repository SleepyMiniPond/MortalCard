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
    public sealed class CardSelectionAssetTests
    {
        [TestCase(CardSelectionCountRule.Optional, false)]
        [TestCase(CardSelectionCountRule.RequiredAvailable, false)]
        [TestCase(CardSelectionCountRule.RequiredExact, false)]
        [TestCase(CardSelectionCountRule.Optional, true)]
        [TestCase(CardSelectionCountRule.RequiredAvailable, true)]
        [TestCase(CardSelectionCountRule.RequiredExact, true)]
        public void CountRuleAndDependentTargets_SurviveSaveUnloadReload(CardSelectionCountRule rule, bool useOverride)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/EditMode/GameData/SelectionRoundTrip.asset");
            CardDataScriptableBase asset = useOverride
                ? ScriptableObject.CreateInstance<OverrideCardDataScriptable>()
                : ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.CardData.ID = "selection-round-trip";
                asset.CardData.SubSelects.Add(_Group("first"));
                var second = _Group("second");
                second.CountRule = rule;
                second.CardCandidates = new CombinedCardCollection
                {
                    FirstCollection = new SingleCardCollection { TargetCard = new TriggeredCard() },
                    SecondCollection = new SubSelectedCardCollection { SelectionId = "first" },
                    Operation = SetOperationType.Difference
                };
                asset.CardData.SubSelects.Add(second);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssetIfDirty(asset);
                Resources.UnloadAsset(asset);
                asset = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                asset = AssetDatabase.LoadAssetAtPath<CardDataScriptableBase>(path);
                var loaded = (ExistCardSelectionGroup)asset.CardData.SubSelects[1];
                Assert.That(loaded.CountRule, Is.EqualTo(rule));
                Assert.That(loaded.Id, Is.EqualTo("second"));
                Assert.That(asset.CardData.ID, Is.EqualTo("selection-round-trip"));
                Assert.That(((ConstInteger)loaded.SelectCount).Value, Is.EqualTo(1));
                Assert.That(((SubSelectedCardCollection)((CombinedCardCollection)loaded.CardCandidates).SecondCollection)
                    .SelectionId, Is.EqualTo("first"));
                Assert.That(((CombinedCardCollection)loaded.CardCandidates).Operation, Is.EqualTo(SetOperationType.Difference));
                Assert.That(typeof(ExistCardSelectionGroup).GetField("IsMustSelect"), Is.Null);
            }
            finally
            {
                if (asset != null && !AssetDatabase.Contains(asset)) UnityEngine.Object.DestroyImmediate(asset);
                // 僅清理本測試建立的唯一暫存資產。
                AssetDatabase.DeleteAsset(path);
            }
        }

        [TestCase("first", true)]
        [TestCase("second", true)]
        [TestCase("missing", true)]
        [TestCase("first", false)]
        public void Validator_ReferencesMustPointToEarlierGroups(string reference, bool fromFirst)
        {
            _WithCard((card, catalog) =>
            {
                var first = _Group("first");
                var second = _Group("second");
                (fromFirst ? first : second).CardCandidates = new CombinedCardCollection
                {
                    FirstCollection = new SingleCardCollection { TargetCard = new TriggeredCard() },
                    SecondCollection = new SubSelectedCardCollection { SelectionId = reference },
                    Operation = SetOperationType.Difference
                };
                card.Data.SubSelects.Add(first);
                card.Data.SubSelects.Add(second);
                var errors = GameDataValidator.ValidateNestedContent(catalog);
                Assert.That(errors.Any(error => error.Contains("必須引用前面的")), Is.EqualTo(fromFirst));
                if (!fromFirst) Assert.That(errors, Is.Empty);
            });
        }

        [Test]
        public void Validator_RejectsInvalidSetOperationAndMissingCollections()
        {
            _WithCard((card, catalog) =>
            {
                var group = _Group("first");
                group.CardCandidates = new CombinedCardCollection { Operation = (SetOperationType)999 };
                card.Data.SubSelects.Add(group);
                var errors = GameDataValidator.ValidateNestedContent(catalog);
                Assert.That(errors, Has.Some.Contains("CombinedCardCollection.Operation 無效"));
                Assert.That(errors, Has.Some.Contains("FirstCollection 為空"));
                Assert.That(errors, Has.Some.Contains("SecondCollection 為空"));
            });
        }

        [Test]
        public void Validator_ChecksCountDependenciesAndDoesNotRestrictEffectReferences()
        {
            _WithCard((card, catalog) =>
            {
                var group = _Group("first");
                group.SelectCount = new CardCollectionCountInteger { CardCollection = new SubSelectedCardCollection { SelectionId = "first" } };
                card.Data.SubSelects.Add(group);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Has.Some.Contains("必須引用前面的"));
                group.SelectCount = new ConstInteger { Value = 1 };
                card.Data.Effects.Add(new DiscardCardEffect { TargetCards = new SubSelectedCardCollection { SelectionId = "first" } });
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
            });
        }

        [Test]
        public void Validator_ReportsInvalidModeNegativeCountAndMissingCandidates()
        {
            _WithCard((card, catalog) =>
            {
                var group = _Group("first");
                group.CountRule = (CardSelectionCountRule)99;
                group.SelectCount = new ArithmeticInteger
                {
                    Left = new ConstInteger { Value = 0 }, Right = new ConstInteger { Value = 1 },
                    Operation = ArithmeticType.Subtract
                };
                group.CardCandidates = null;
                card.Data.SubSelects.Add(group);
                var errors = GameDataValidator.ValidateNestedContent(catalog);
                Assert.That(errors, Has.Some.Contains("CountRule 無效"));
                Assert.That(errors, Has.Some.Contains("SelectCount 不得為負數"));
                Assert.That(errors, Has.Some.Contains("CardCandidates 為空"));
            });
        }

        [Test]
        public void Localization_SelectionSpecificTextIsOptional()
        {
            _WithCard((card, catalog) =>
            {
                var localization = ScriptableObject.CreateInstance<ExcelDatas>();
                try
                {
                    localization.LocalizeUI = new List<LocalizeExcelData>();
                    localization.LocalizeCardBuff = new List<LocalizeExcelTitleData>();
                    localization.LocalizePlayerBuff = new List<LocalizeExcelTitleData>();
                    localization.LocalizePlayer = new List<LocalizeExcelTitleData>();
                    localization.LocalizeKeyWord = new List<LocalizeExcelTitleData>();
                    localization.LocalizeCard = new List<LocalizeExcelTitleData>
                    {
                        new() { Id = card.Data.ID, Title = "測試卡片", Info = "測試說明" }
                    };
                    var group = _Group("first");
                    card.Data.SubSelects.Add(group);
                    Assert.That(GameDataValidator.ValidateLocalization(catalog, localization, Array.Empty<PlayerData>()), Is.Empty);
                    localization.LocalizeUI.Add(new LocalizeExcelData
                    {
                        Id = $"CardSelection.{card.Data.ID}.{group.Id}", Info = "選擇第一張卡片"
                    });
                    Assert.That(GameDataValidator.ValidateLocalization(catalog, localization, Array.Empty<PlayerData>()), Is.Empty);
                }
                finally { UnityEngine.Object.DestroyImmediate(localization); }
            });
        }

        private static ExistCardSelectionGroup _Group(string id) => new()
        {
            Id = id, CountRule = CardSelectionCountRule.RequiredAvailable,
            SelectCount = new ConstInteger { Value = 1 },
            CardCandidates = new SingleCardCollection { TargetCard = new TriggeredCard() }
        };

        private static void _WithCard(Action<StandardCardDataScriptable, GameContentCatalog> action)
        {
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            try
            {
                card.Data.ID = "selection-validation";
                typeof(GameContentCatalog).GetField("_cardAssets", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(catalog, new CardDataScriptableBase[] { card });
                action(card, catalog);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(card);
            }
        }
    }
}
