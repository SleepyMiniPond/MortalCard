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

        internal static bool CanEnterPlayMode(IReadOnlyList<ValidationIssue> errors)
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
                ValidationDiagnosticReporter.ReportUnexpectedFailure(exception);
            }
        }

        private static void _BlockPlayMode(IReadOnlyList<ValidationIssue> errors)
        {
            EditorApplication.isPlaying = false;
            ValidationDiagnosticReporter.ReportInvalidForPlayMode(errors);
        }
    }
}
