using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

public record CardInfo(
    Guid Identity,
    string CardDataID,
    CardType Type,
    CardRarity Rarity,
    IEnumerable<CardTheme> Themes,
    int OriginCost,
    int Cost,
    int OriginPower,
    int Power,
    MainSelectionInfo MainSelectable,
    IReadOnlyList<CardBuffInfo> BuffInfos,
    IReadOnlyList<CardProperty> Properties,
    IReadOnlyList<string> Keywords)
{
    public static CardInfo Create(ICardEntity card, TriggerContext triggerContext)
    {
        return new CardInfo(
            Identity: card.Identity,
            CardDataID: card.CardDataId,
            Type: card.Type,
            Rarity: card.Rarity,
            Themes: card.Themes,
            OriginCost: card.OriginCost,
            Cost: GameFormula.CardCost(triggerContext, card),
            OriginPower: card.OriginPower,
            Power: GameFormula.CardPower(triggerContext, card),
            MainSelectable: card.MainSelect.ToInfo(),
            BuffInfos: card.BuffManager.Buffs.Select(s => new CardBuffInfo(s)).ToList(),
            Properties: card.Properties.Select(p => p.Property)
                .Concat(card.BuffManager.Buffs
                    .SelectMany(b => b.Properties.Select(p => p.Property)))
                .Distinct()
                .ToList(),
            Keywords: card.Properties.SelectMany(p => p.Keywords)
                .Concat(card.BuffManager.Buffs.SelectMany(b => b.Keywords))
                .Distinct()
                .ToList()
        );
    }

    public static CardInfo CreatePreview(ICardEntity card, TriggerContext triggerContext)
    {
        var basicInfo = Create(card, triggerContext);
        return basicInfo with
        {
            Power = GameFormula.CardPreviewPower(triggerContext, card)
        };
    }

    public const string KEY_COST = "cost";
    public const string KEY_POWER = "power";

    public Dictionary<string, string> GetTemplateValues()
    {
        return new Dictionary<string, string>()
        {
            { KEY_COST, Cost.ToString() },
            { KEY_POWER, Power.ToString() },
        };
    }
}

public record CardCollectionInfo(
    CardCollectionType Type,
    IImmutableDictionary<CardInfo, int> CardInfos)
{
    public static readonly CardCollectionInfo Empty = new CardCollectionInfo(
        CardCollectionType.None,
        ImmutableDictionary<CardInfo, int>.Empty);
    public int Count => CardInfos.Count;
}

public static class CardCollectionInfoUtility
{
    public static CardInfo ToInfo(
        this ICardEntity card, IGameplayModel gameWatcher)
    {        
        var cardLookTriggerContext = new TriggerContext(
            gameWatcher,
            new CardTrigger(card),
            new CardLookIntentAction(card));
        return CardInfo.Create(card, cardLookTriggerContext);
    }
    public static CardInfo ToPreviewInfo(
        this ICardEntity card, IGameplayModel gameWatcher)
    {
        var cardOwnerCardManager = card.Owner(gameWatcher).Map(owner => owner.CardManager.HandCard);
        var handCardIndex = cardOwnerCardManager.Map(manager => manager.Cards.ToList().IndexOf(card)).ValueOr(-1);
        var handCardsCount = cardOwnerCardManager.Map(manager => manager.Cards.Count).ValueOr(0);
        var basicInfo = card.ToInfo(gameWatcher);

        //TODO: collect real player.EnergyLoseCommand
        var energyLoseCommand = new LoseEnergyEffectCommand(card.Owner(gameWatcher).ValueOr(DummyPlayer.Instance), 0);

        var cardPlaySource = new CardPlaySource(card, handCardIndex, handCardsCount, energyLoseCommand, new CardPlayAttributeEntity());
        var cardPreviewTriggerContext = new TriggerContext(
            gameWatcher,
            new CardPlayTrigger(cardPlaySource),
            new CardPlayIntentAction(cardPlaySource));
        return CardInfo.CreatePreview(card, cardPreviewTriggerContext);
    }

    public static CardCollectionInfo ToCardCollectionInfo(this IEnumerable<CardInfo> cardInfos, CardCollectionType type)
    {
        return new CardCollectionInfo(
            type,
            cardInfos
                .Select((info, index) => (info, index))
                .ToImmutableDictionary(
                    pair => pair.Item1,
                    pair => pair.index));
    }

    public static CardCollectionInfo ToCardCollectionInfo(
        this ICardColletionZone cardCollectionZone, IGameplayModel gameWatcher)
    {
        return cardCollectionZone.Cards
            .Select(c => c.ToInfo(gameWatcher))
            .ToCardCollectionInfo(cardCollectionZone.Type);
    }

    public static ImmutableArray<Guid> ToCardIdentities(
        this ICardColletionZone cardCollectionZone)
    {        
        return cardCollectionZone.Cards.Select(c => c.Identity).ToImmutableArray();
    }

    public static ImmutableArray<CardInfo> ToCardInfos(
        this IEnumerable<ICardEntity> cards, IGameplayModel gameWatcher)
    {
        return cards.Select(c => c.ToInfo(gameWatcher)).ToImmutableArray();
    }
}

