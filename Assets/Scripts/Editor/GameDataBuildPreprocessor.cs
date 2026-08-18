using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MortalGame.Editor
{
    public sealed class GameDataBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            GameDataBuildGate.EnsureValid(GameDataValidator.ValidateAll());
        }
    }

    internal static class GameDataBuildGate
    {
        internal static void EnsureValid(IReadOnlyList<string> errors)
        {
            var validationErrors = errors ?? Array.Empty<string>();
            if (validationErrors.Count == 0)
                return;

            var details = string.Join(
                Environment.NewLine,
                validationErrors.Select(error => $"- {error}"));
            throw new BuildFailedException(
                $"遊戲資料驗證失敗，共 {validationErrors.Count} 個錯誤：" +
                Environment.NewLine + details);
        }
    }
}
