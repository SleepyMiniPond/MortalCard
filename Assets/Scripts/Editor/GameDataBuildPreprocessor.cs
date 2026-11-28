using System;
using MortalGame.Editor.Exceptions;
using System.Collections.Generic;
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
        internal static void EnsureValid(IReadOnlyList<ValidationIssue> errors)
        {
            var validationErrors = errors ?? Array.Empty<ValidationIssue>();
            if (validationErrors.Count == 0)
                return;

            throw new GameDataValidationBuildFailedException(validationErrors);
        }
    }
}
