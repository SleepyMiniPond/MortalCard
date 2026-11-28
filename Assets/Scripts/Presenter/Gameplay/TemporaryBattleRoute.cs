using System;
using System.Linq;
using MortalGame.GameModel;
using UnityEngine;

namespace MortalGame.Presenter
{
    /// <summary>
    /// 正式地圖完成前的兩關流程；關數、測試敵人與關卡設定集中於此，供後續整包替換。
    /// </summary>
    public sealed class TemporaryBattleRoute
    {
        public const int StageCount = 2;
        private int _stageNumber = 1;

        public bool IsFinished => _stageNumber > StageCount;

        public void Reset() => _stageNumber = 1;

        public void CompleteStage() => _stageNumber++;

        // 僅供臨時流程標示使用；其他關卡識別回傳 0，讓畫面隱藏標示。
        public static int GetStageNumber(string stageId)
        {
            for (var number = 1; number <= StageCount; number++)
                if (stageId == _StageId(number)) return number;
            return 0;
        }

        private static string _StageId(int number) => $"TemporaryStage{number}";

        public GameStageSetting CreateBattle(Context context)
        {
            GameplayDiagnosticLogger.LogTemporaryStage(_stageNumber, StageCount);
            return new GameStageSetting(
                StageID: _StageId(_stageNumber),
                RandomSeed: Environment.TickCount,
                Ally: context.Ally with { Deck = context.Ally.Deck.ToList() },
                Enemy: context.AllEnemies[0]);
        }
    }
}
