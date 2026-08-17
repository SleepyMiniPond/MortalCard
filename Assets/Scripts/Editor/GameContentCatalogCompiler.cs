using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Editor
{
    public static class GameContentCatalogCompiler
    {
        public static GameContentCatalog CompileDefault()
        {
            return Compile(
                ProjectAssetPaths.GameContent.Catalog,
                ProjectAssetPaths.GameContent.SearchFolders);
        }

        public static GameContentCatalog Compile(
            string catalogAssetPath,
            IReadOnlyList<string> searchFolders)
        {
            if (string.IsNullOrWhiteSpace(catalogAssetPath))
                throw new ArgumentException("Catalog 資產路徑不可為空。", nameof(catalogAssetPath));

            var folders = (searchFolders ?? Array.Empty<string>())
                .Where(folder => !string.IsNullOrWhiteSpace(folder))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (folders.Length == 0)
                throw new ArgumentException("內容搜尋目錄不可為空。", nameof(searchFolders));

            var catalog = _LoadOrCreateCatalog(catalogAssetPath);
            var serializedCatalog = new SerializedObject(catalog);

            _SetAssetReferences(
                serializedCatalog,
                "_cardAssets",
                EditorAssetUtility.FindAssets<CardDataScriptableBase>(folders));
            _SetAssetReferences(
                serializedCatalog,
                "_cardBuffAssets",
                EditorAssetUtility.FindAssets<CardBuffScriptable>(folders));
            _SetAssetReferences(
                serializedCatalog,
                "_playerBuffAssets",
                EditorAssetUtility.FindAssets<PlayerBuffDataScriptable>(folders));
            _SetAssetReferences(
                serializedCatalog,
                "_characterBuffAssets",
                EditorAssetUtility.FindAssets<CharacterBuffDataScriptable>(folders));

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static GameContentCatalog _LoadOrCreateCatalog(string catalogAssetPath)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(catalogAssetPath);
            if (catalog != null)
                return catalog;

            var existingAsset = AssetDatabase.LoadMainAssetAtPath(catalogAssetPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException(
                    $"Catalog 輸出路徑已存在其他類型資產：{catalogAssetPath}");
            }

            catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogAssetPath);
            return catalog;
        }

        private static void _SetAssetReferences<T>(
            SerializedObject serializedCatalog,
            string propertyName,
            IReadOnlyList<T> assets)
            where T : UnityEngine.Object
        {
            var property = serializedCatalog.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"GameContentCatalog 缺少序列化欄位：{propertyName}");
            }

            property.arraySize = assets.Count;
            for (var index = 0; index < assets.Count; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = assets[index];
        }
    }
}
