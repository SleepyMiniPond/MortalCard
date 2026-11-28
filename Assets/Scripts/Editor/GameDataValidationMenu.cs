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
                ValidationDiagnosticReporter.ReportValid();
                return;
            }

            ValidationDiagnosticReporter.ReportInvalidForMenu(errors);
        }
    }
}
