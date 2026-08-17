using System;
using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class GameContentCatalogCompilerTests
    {
        [Test]
        public void Compile_CreatesCatalogWithAllSupportedTypesSortedByPath()
        {
            var folderPath = _CreateTemporaryFolder();
            var catalogPath = $"{folderPath}/GameContentCatalog.asset";

            try
            {
                var standardCard = _CreateAsset<StandardCardDataScriptable>(
                    $"{folderPath}/Z_StandardCard.asset");
                standardCard.Data.ID = "standard-card";

                var overrideCard = _CreateAsset<OverrideCardDataScriptable>(
                    $"{folderPath}/A_OverrideCard.asset");
                overrideCard.Data.ID = "override-card";

                var cardBuff = _CreateAsset<CardBuffScriptable>(
                    $"{folderPath}/CardBuff.asset");
                cardBuff.Data.ID = "card-buff";

                var playerBuff = _CreateAsset<PlayerBuffDataScriptable>(
                    $"{folderPath}/PlayerBuff.asset");
                playerBuff.Data.ID = "player-buff";

                var characterBuff = _CreateAsset<CharacterBuffDataScriptable>(
                    $"{folderPath}/CharacterBuff.asset");
                characterBuff.Data.ID = "character-buff";

                AssetDatabase.SaveAssets();

                var catalog = GameContentCatalogCompiler.Compile(
                    catalogPath,
                    new[] { folderPath });

                Assert.That(catalog.CardAssets.Count, Is.EqualTo(2));
                Assert.That(catalog.CardAssets[0].CardData.ID, Is.EqualTo("override-card"));
                Assert.That(catalog.CardAssets[1].CardData.ID, Is.EqualTo("standard-card"));
                Assert.That(catalog.CardBuffAssets.Count, Is.EqualTo(1));
                Assert.That(catalog.CardBuffAssets[0].Data.ID, Is.EqualTo("card-buff"));
                Assert.That(catalog.PlayerBuffAssets.Count, Is.EqualTo(1));
                Assert.That(catalog.PlayerBuffAssets[0].Data.ID, Is.EqualTo("player-buff"));
                Assert.That(catalog.CharacterBuffAssets.Count, Is.EqualTo(1));
                Assert.That(catalog.CharacterBuffAssets[0].Data.ID, Is.EqualTo("character-buff"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void Compile_ReusesExistingCatalogAndRefreshesItsContents()
        {
            var folderPath = _CreateTemporaryFolder();
            var catalogPath = $"{folderPath}/GameContentCatalog.asset";
            var firstCardPath = $"{folderPath}/FirstCard.asset";

            try
            {
                var firstCard = _CreateAsset<StandardCardDataScriptable>(firstCardPath);
                firstCard.Data.ID = "first-card";
                AssetDatabase.SaveAssets();

                var firstCatalog = GameContentCatalogCompiler.Compile(
                    catalogPath,
                    new[] { folderPath });
                var catalogGuid = AssetDatabase.AssetPathToGUID(catalogPath);

                AssetDatabase.DeleteAsset(firstCardPath);
                var secondCard = _CreateAsset<StandardCardDataScriptable>(
                    $"{folderPath}/SecondCard.asset");
                secondCard.Data.ID = "second-card";
                AssetDatabase.SaveAssets();

                var secondCatalog = GameContentCatalogCompiler.Compile(
                    catalogPath,
                    new[] { folderPath });

                Assert.That(secondCatalog, Is.SameAs(firstCatalog));
                Assert.That(AssetDatabase.AssetPathToGUID(catalogPath), Is.EqualTo(catalogGuid));
                Assert.That(secondCatalog.CardAssets.Count, Is.EqualTo(1));
                Assert.That(secondCatalog.CardAssets[0].CardData.ID, Is.EqualTo("second-card"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        private static string _CreateTemporaryFolder()
        {
            var folderName = $"CompilerTemp_{Guid.NewGuid():N}";
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
    }
}
