using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace MortalGame.Editor
{
    public static class EditorAssetUtility
    {
        public static IReadOnlyList<T> FindAssets<T>(
            IReadOnlyList<string> searchFolders)
            where T : UnityEngine.Object
        {
            var folders = (searchFolders ?? Array.Empty<string>()).ToArray();
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", folders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToArray();
        }
    }
}
