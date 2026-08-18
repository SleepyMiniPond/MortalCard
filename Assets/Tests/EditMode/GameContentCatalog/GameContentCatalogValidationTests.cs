using System;
using System.Linq;
using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class GameContentCatalogValidationTests
    {
        [Test]
        public void DefaultCatalog_CoversAllScannedGameContent()
        {
            Assert.That(GameDataValidator.ValidateCatalogCoverage(), Is.Empty);
        }

        [Test]
        public void ValidateCatalogCoverage_ReportsAssetCreatedAfterCompilation()
        {
            var folderPath = _CreateTemporaryFolder();

            try
            {
                _CreateAsset<StandardCardDataScriptable>($"{folderPath}/FirstCard.asset");
                AssetDatabase.SaveAssets();
                var catalog = GameContentCatalogCompiler.Compile(
                    $"{folderPath}/GameContentCatalog.asset",
                    new[] { folderPath });

                var secondCardPath = $"{folderPath}/SecondCard.asset";
                _CreateAsset<StandardCardDataScriptable>(secondCardPath);
                AssetDatabase.SaveAssets();

                var errors = GameDataValidator.ValidateCatalogCoverage(
                    catalog,
                    new[] { folderPath });

                Assert.That(errors, Has.Some.Contains("未收錄掃描到的資產"));
                Assert.That(errors, Has.Some.Contains(secondCardPath));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void ValidateCatalogCoverage_ReportsNullAndDuplicateReferences()
        {
            var folderPath = _CreateTemporaryFolder();

            try
            {
                var firstCard = _CreateAsset<StandardCardDataScriptable>(
                    $"{folderPath}/FirstCard.asset");
                var secondCard = _CreateAsset<StandardCardDataScriptable>(
                    $"{folderPath}/SecondCard.asset");
                AssetDatabase.SaveAssets();
                var catalog = GameContentCatalogCompiler.Compile(
                    $"{folderPath}/GameContentCatalog.asset",
                    new[] { folderPath });

                _SetCardAssets(catalog, firstCard, firstCard, null);

                var errors = GameDataValidator.ValidateCatalogCoverage(
                    catalog,
                    new[] { folderPath });

                Assert.That(errors, Has.Some.Contains("重複收錄資產"));
                Assert.That(errors, Has.Some.Contains("空資產引用"));
                Assert.That(
                    errors,
                    Has.Some.Contains(AssetDatabase.GetAssetPath(secondCard)));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void ValidateCatalogCoverage_ReportsNonDeterministicOrder()
        {
            var folderPath = _CreateTemporaryFolder();

            try
            {
                var firstCard = _CreateAsset<StandardCardDataScriptable>(
                    $"{folderPath}/A_FirstCard.asset");
                var secondCard = _CreateAsset<StandardCardDataScriptable>(
                    $"{folderPath}/B_SecondCard.asset");
                AssetDatabase.SaveAssets();
                var catalog = GameContentCatalogCompiler.Compile(
                    $"{folderPath}/GameContentCatalog.asset",
                    new[] { folderPath });

                _SetCardAssets(catalog, secondCard, firstCard);

                var errors = GameDataValidator.ValidateCatalogCoverage(
                    catalog,
                    new[] { folderPath });

                Assert.That(errors.Single(), Does.Contain("順序與資產路徑排序不一致"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        private static string _CreateTemporaryFolder()
        {
            var folderName = $"ValidationTemp_{Guid.NewGuid():N}";
            var folderGuid = AssetDatabase.CreateFolder(
                ProjectAssetPaths.Tests.GameContentCatalogRoot,
                folderName);
            Assert.That(folderGuid, Is.Not.Empty);
            return AssetDatabase.GUIDToAssetPath(folderGuid);
        }

        private static T _CreateAsset<T>(string assetPath)
            where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void _SetCardAssets(
            GameContentCatalog catalog,
            params CardDataScriptableBase[] cardAssets)
        {
            var serializedCatalog = new SerializedObject(catalog);
            var cardAssetsProperty = serializedCatalog.FindProperty("_cardAssets");
            cardAssetsProperty.arraySize = cardAssets.Length;

            for (var index = 0; index < cardAssets.Length; index++)
            {
                cardAssetsProperty.GetArrayElementAtIndex(index).objectReferenceValue =
                    cardAssets[index];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
