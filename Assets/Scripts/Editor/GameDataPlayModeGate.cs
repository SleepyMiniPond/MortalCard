using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Editor
{
    [InitializeOnLoad]
    public static class GameDataPlayModeGate
    {
        static GameDataPlayModeGate()
        {
            EditorApplication.playModeStateChanged += _OnPlayModeStateChanged;
        }

        internal static bool CanEnterPlayMode(IReadOnlyList<string> errors)
        {
            return errors != null && errors.Count == 0;
        }

        private static void _OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            try
            {
                var errors = GameDataValidator.ValidateAll();
                if (CanEnterPlayMode(errors))
                    return;

                _BlockPlayMode(errors);
            }
            catch (Exception exception)
            {
                EditorApplication.isPlaying = false;
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "遊戲資料驗證",
                    "驗證遊戲資料時發生例外，已取消進入 Play Mode。請查看 Console。",
                    "確定");
            }
        }

        private static void _BlockPlayMode(IReadOnlyList<string> errors)
        {
            EditorApplication.isPlaying = false;
            foreach (var error in errors)
                Debug.LogError($"[Play Mode Gate] {error}");

            EditorUtility.DisplayDialog(
                "遊戲資料驗證",
                $"發現 {errors.Count} 個錯誤，已取消進入 Play Mode。請查看 Console。",
                "確定");
        }
    }
}
