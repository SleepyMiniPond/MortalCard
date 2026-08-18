using MortalGame.GameData;
using MortalGame.Editor;
using MortalGame.Presenter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class ScriptableDataLoaderCatalogTests
    {
        [Test]
        public void MainScene_ReferencesDefaultCatalog()
        {
            var dependencies = AssetDatabase.GetDependencies(
                ProjectAssetPaths.Scenes.Main,
                true);

            Assert.That(
                dependencies,
                Does.Contain(ProjectAssetPaths.GameContent.Catalog));
        }

        [Test]
        public void GameContentCollections_AreReadFromCatalog()
        {
            var gameObject = new GameObject(nameof(ScriptableDataLoaderCatalogTests));
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var cardBuff = ScriptableObject.CreateInstance<CardBuffScriptable>();
            var playerBuff = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            var characterBuff = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();

            try
            {
                _SetCatalogAssets(catalog, card, cardBuff, playerBuff, characterBuff);

                var loader = gameObject.AddComponent<ScriptableDataLoader>();
                var serializedLoader = new SerializedObject(loader);
                serializedLoader.FindProperty("_gameContentCatalog").objectReferenceValue = catalog;
                serializedLoader.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(loader.AllCards, Is.EqualTo(new[] { card.CardData }));
                Assert.That(loader.AllCardBuffs, Is.EqualTo(new[] { cardBuff.Data }));
                Assert.That(loader.AllPlayerBuffs, Is.EqualTo(new[] { playerBuff.Data }));
                Assert.That(loader.AllCharacterBuffs, Is.EqualTo(new[] { characterBuff.Data }));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(card);
                Object.DestroyImmediate(cardBuff);
                Object.DestroyImmediate(playerBuff);
                Object.DestroyImmediate(characterBuff);
            }
        }

        private static void _SetCatalogAssets(
            GameContentCatalog catalog,
            CardDataScriptableBase card,
            CardBuffScriptable cardBuff,
            PlayerBuffDataScriptable playerBuff,
            CharacterBuffDataScriptable characterBuff)
        {
            var serializedCatalog = new SerializedObject(catalog);
            _SetSingleReference(serializedCatalog.FindProperty("_cardAssets"), card);
            _SetSingleReference(serializedCatalog.FindProperty("_cardBuffAssets"), cardBuff);
            _SetSingleReference(serializedCatalog.FindProperty("_playerBuffAssets"), playerBuff);
            _SetSingleReference(serializedCatalog.FindProperty("_characterBuffAssets"), characterBuff);
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void _SetSingleReference(
            SerializedProperty arrayProperty,
            Object asset)
        {
            arrayProperty.arraySize = 1;
            arrayProperty.GetArrayElementAtIndex(0).objectReferenceValue = asset;
        }
    }
}
