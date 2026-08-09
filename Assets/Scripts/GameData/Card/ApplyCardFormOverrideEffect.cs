using System;
using System.Collections.Generic;
using MortalGame.GameModel;
using Sirenix.OdinInspector;

namespace MortalGame.GameData
{
    /// <summary>
    /// 將目標卡片暫時切換為指定的 Override CardData。
    /// 再次套用不同 Override 時，新的狀態會永久取代舊狀態。
    /// </summary>
    [Serializable]
    public sealed class ApplyCardFormOverrideEffect : ICardEffect
    {
        public ITargetCardCollectionValue TargetCards;
        public string OverrideKey;
        public string TargetCardDataId;

        [ShowInInspector]
        public List<CardFormOverrideReleaseRule> ReleaseRules = new();

        [ShowInInspector]
        public Dictionary<string, IReactionSessionData> ReactionSessions = new();
    }
}
