using System;
using MortalGame.GameModel;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{
    /// <summary>
    /// 卡片在指定生命週期時機觸發的條件效果資料，供 Standard 與 Override Card 共用。
    /// 時機由 CardData.TriggeredEffects 的字典鍵提供。
    /// </summary>
    [Serializable]
    public sealed class ConditionalCardEffect
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ICondition> Conditions = new();

        [Space(20)]
        [HorizontalGroup("2")]
        public ICardEffect Effect;
    }
    

    [Serializable]
    public class MainTargetSelectLogic
    {
        public IMainTargetSelectable MainSelectable = new NoneSelectable();
        public TargetLogicTag LogicTag = TargetLogicTag.None;
    }
}
