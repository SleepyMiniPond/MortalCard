using System;
using MortalGame.GameModel;
using Sirenix.OdinInspector;

namespace MortalGame.GameData
{
    /// <summary>
    /// 卡片在指定生命週期時機觸發的效果資料，供 Standard 與 Override Card 共用。
    /// </summary>
    [Serializable]
    public sealed class TriggeredCardEffect
    {
        [TableColumnWidth(150, false)]
        public CardTriggeredTiming Timing;

        [ShowInInspector]
        // TODO: conditional cardeffect
        public ICardEffect[] Effects = Array.Empty<ICardEffect>();
    }
    

    [Serializable]
    public class MainTargetSelectLogic
    {
        public IMainTargetSelectable MainSelectable = new NoneSelectable();
        public TargetLogicTag LogicTag = TargetLogicTag.None;
    }
}
