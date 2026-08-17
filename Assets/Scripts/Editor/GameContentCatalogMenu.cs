using UnityEditor;
using UnityEngine;

namespace MortalGame.Editor
{
    public static class GameContentCatalogMenu
    {
        [MenuItem("MortalGame/遊戲內容/重新產生內容目錄")]
        public static void RebuildCatalog()
        {
            var catalog = GameContentCatalogCompiler.CompileDefault();

            Debug.Log(
                "遊戲內容目錄已重新產生：" +
                $"Card {catalog.CardAssets.Count}、" +
                $"CardBuff {catalog.CardBuffAssets.Count}、" +
                $"PlayerBuff {catalog.PlayerBuffAssets.Count}、" +
                $"CharacterBuff {catalog.CharacterBuffAssets.Count}。");

            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }
    }
}
