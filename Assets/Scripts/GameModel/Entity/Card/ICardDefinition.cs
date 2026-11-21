using System;
using System.Collections.Generic;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public interface ICardDefinition
    {
        CardData Data { get; }
        IReadOnlyList<CardEffectOption> DescriptionOptions { get; }
        ICardDefinition CopyForNewCard();
    }

    /// <summary>一般卡引用 Catalog 配置，個別狀態仍由 Entity 保存。</summary>
    public sealed class CatalogCardDefinition : ICardDefinition
    {
        public CardData Data { get; }
        public IReadOnlyList<CardEffectOption> DescriptionOptions => Array.Empty<CardEffectOption>();

        public CatalogCardDefinition(CardData data) => Data = data;
        public ICardDefinition CopyForNewCard() => this;
    }
}
