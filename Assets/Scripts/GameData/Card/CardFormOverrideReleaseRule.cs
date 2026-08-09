using System;
using System.Collections.Generic;
using MortalGame.GameModel;
using Sirenix.OdinInspector;

namespace MortalGame.GameData
{
    /// <summary>
    /// 定義 External Override 在指定 Timing 的解除條件。
    /// 同一規則內的 Conditions 全部成立時，才允許解除目前 Override。
    /// </summary>
    [Serializable]
    public sealed class CardFormOverrideReleaseRule
    {
        public GameTiming Timing;

        [ShowInInspector]
        public List<ICondition> Conditions = new();
    }
}
