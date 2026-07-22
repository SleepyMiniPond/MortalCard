using UnityEditor;
using UnityEngine;

namespace MortalGame.Editor
{
    public static class GameDataValidationMenu
    {
        [MenuItem("MortalGame/驗證遊戲資料")]
        public static void ValidateGameData()
        {
            var errors = GameDataValidator.ValidateAll();
            if (errors.Count == 0)
            {
                Debug.Log("遊戲資料驗證完成：未發現錯誤。");
                EditorUtility.DisplayDialog("遊戲資料驗證", "未發現錯誤。", "確定");
                return;
            }

            foreach (var error in errors)
                Debug.LogError($"[遊戲資料驗證] {error}");

            EditorUtility.DisplayDialog(
                "遊戲資料驗證",
                $"發現 {errors.Count} 個錯誤，請查看 Console。",
                "確定");
        }
    }
}
