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
        private const int StageCount = 2;
        private int _stageNumber = 1;

        public bool IsFinished => _stageNumber > StageCount;

        public void Reset() => _stageNumber = 1;

        public void CompleteStage() => _stageNumber++;

        public GameStageSetting CreateBattle(Context context)
        {
            Debug.Log($"[臨時兩關流程] 第 {_stageNumber}／{StageCount} 關");
            return new GameStageSetting(
                StageID: $"TemporaryStage{_stageNumber}",
                RandomSeed: Environment.TickCount,
                Ally: context.Ally with { Deck = context.Ally.Deck.ToList() },
                Enemy: context.AllEnemies[0]);
        }
    }
}
