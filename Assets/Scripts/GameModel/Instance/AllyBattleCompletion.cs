using System;
using System.Collections.Generic;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 勝利結束時可延續的玩家狀態快照；產生快照不代表已提交至跨戰鬥狀態。
    /// </summary>
    public sealed record AllyBattleCompletion(
        Guid OriginAllyInstanceGuid,
        int CurrentHealth,
        int CurrentDisposition,
        IReadOnlyList<CardInstanceChangeSet> CardInstanceChanges);
}
