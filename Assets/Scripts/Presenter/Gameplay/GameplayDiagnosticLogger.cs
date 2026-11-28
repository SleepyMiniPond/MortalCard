using MortalGame.Presentation.Abstractions;
using UnityEngine;

namespace MortalGame.Presenter
{
    internal static class GameplayDiagnosticLogger
    {
        public static void LogReceivedCommand(IGameCommand command) =>
            Debug.Log($"-- GameplayPresenter.RecieveEvent:[{command}] --");

        public static void LogTemporaryStage(int stageNumber, int stageCount) =>
            Debug.Log($"[臨時兩關流程] 第 {stageNumber}／{stageCount} 關");
    }
}
