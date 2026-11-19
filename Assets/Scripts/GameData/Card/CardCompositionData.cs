using System;
using System.Collections.Generic;
using MortalGame.GameModel;

namespace MortalGame.GameData
{
    /// <summary>選項只追加效果及描述，不改寫模板基本資料。</summary>
    [Serializable]
    public sealed class CardEffectOption
    {
        public string Id;
        public string DescriptionKey;
        public List<ICardEffect> Effects = new();
        public Dictionary<CardTriggeredTiming, ConditionalCardEffect[]> TriggeredEffects = new();
    }

    [Serializable]
    public sealed class CardTemplateSelectionGroup : ISubSelectionGroup
    {
        public string Id;
        string ISubSelectionGroup.Id => Id;
        public List<string> TemplateCardDataIds = new();
        public int OfferCount;
    }

    [Serializable]
    public sealed class CardEffectSelectionGroup : ISubSelectionGroup
    {
        public string Id;
        string ISubSelectionGroup.Id => Id;
        public string TemplateSelectionId;
        public Dictionary<string, List<CardEffectOption>> PoolsByTemplateId = new();
        public int OfferCount;
        public bool ExcludePreviouslySelected = true;
    }
}
