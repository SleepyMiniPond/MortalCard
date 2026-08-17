using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class GameContentCatalogTests
    {
        [Test]
        public void NewCatalog_HasNonNullEmptyCollections()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            try
            {
                Assert.That(catalog.CardAssets, Is.Empty);
                Assert.That(catalog.CardBuffAssets, Is.Empty);
                Assert.That(catalog.PlayerBuffAssets, Is.Empty);
                Assert.That(catalog.CharacterBuffAssets, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void AssetRoundTrip_PreservesAllContentTypes()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                ProjectAssetPaths.Tests.GameContentCatalogRoot +
                "/GameContentCatalogRoundTrip.asset");
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var standardCard = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var overrideCard = ScriptableObject.CreateInstance<OverrideCardDataScriptable>();
            var cardBuff = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var playerBuff = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            var characterBuff = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();

            try
            {
                standardCard.name = "Standard Card";
                standardCard.Data.ID = "catalog-standard";
                overrideCard.name = "Override Card";
                overrideCard.Data.ID = "catalog-override";
                cardBuff.name = "Card Buff";
                cardBuff.Data.ID = "catalog-card-buff";
                playerBuff.name = "Player Buff";
                playerBuff.Data.ID = "catalog-player-buff";
                characterBuff.name = "Character Buff";
                characterBuff.Data.ID = "catalog-character-buff";

                AssetDatabase.CreateAsset(catalog, assetPath);
                AssetDatabase.AddObjectToAsset(standardCard, catalog);
                AssetDatabase.AddObjectToAsset(overrideCard, catalog);
                AssetDatabase.AddObjectToAsset(cardBuff, catalog);
                AssetDatabase.AddObjectToAsset(playerBuff, catalog);
                AssetDatabase.AddObjectToAsset(characterBuff, catalog);

                var serializedCatalog = new SerializedObject(catalog);
                _SetReferences(
                    serializedCatalog.FindProperty("_cardAssets"),
                    standardCard,
                    overrideCard);
                _SetReferences(
                    serializedCatalog.FindProperty("_cardBuffAssets"),
                    cardBuff);
                _SetReferences(
                    serializedCatalog.FindProperty("_playerBuffAssets"),
                    playerBuff);
                _SetReferences(
                    serializedCatalog.FindProperty("_characterBuffAssets"),
                    characterBuff);
                serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(assetPath);

                Assert.That(loaded.CardAssets.Count, Is.EqualTo(2));
                Assert.That(loaded.CardAssets[0], Is.TypeOf<StandardCardDataScriptable>());
                Assert.That(loaded.CardAssets[0].CardData.ID, Is.EqualTo("catalog-standard"));
                Assert.That(loaded.CardAssets[1], Is.TypeOf<OverrideCardDataScriptable>());
                Assert.That(loaded.CardAssets[1].CardData.ID, Is.EqualTo("catalog-override"));
                Assert.That(loaded.CardBuffAssets.Count, Is.EqualTo(1));
                Assert.That(loaded.CardBuffAssets[0].Data.ID, Is.EqualTo("catalog-card-buff"));
                Assert.That(loaded.PlayerBuffAssets.Count, Is.EqualTo(1));
                Assert.That(loaded.PlayerBuffAssets[0].Data.ID, Is.EqualTo("catalog-player-buff"));
                Assert.That(loaded.CharacterBuffAssets.Count, Is.EqualTo(1));
                Assert.That(loaded.CharacterBuffAssets[0].Data.ID, Is.EqualTo("catalog-character-buff"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void _SetReferences(
            SerializedProperty arrayProperty,
            params UnityEngine.Object[] assets)
        {
            Assert.That(arrayProperty, Is.Not.Null);
            arrayProperty.arraySize = assets.Length;
            for (var index = 0; index < assets.Length; index++)
                arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue = assets[index];
        }
    }
}
