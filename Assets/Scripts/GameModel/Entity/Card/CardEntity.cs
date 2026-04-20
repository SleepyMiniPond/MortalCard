using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Optional;
using Unity.VisualScripting;

public interface ICardEntity
{
    Guid Identity { get; }
    Option<Guid> OriginCardInstanceGuid { get; }
    string CardDataId { get; }

    CardType Type { get; }
    CardRarity Rarity { get; }
    IEnumerable<CardTheme> Themes { get; }

    MainTargetSelectLogic MainSelect { get; }
    IEnumerable<ISubSelectionGroup> SubSelects { get; }

    IEnumerable<ICardEffect> Effects { get; }
    IReadOnlyDictionary<CardTriggeredTiming, IEnumerable<ICardEffect>> TriggeredEffects { get; }
    IEnumerable<ICardPropertyEntity> Properties { get; }
    ICardBuffManager BuffManager { get; }

    int OriginCost { get; }
    int OriginPower { get; }

    ICardEntity Clone(bool includeCardProperties, bool includeCardBuffs);
}

public class CardEntity : ICardEntity
{
    // Card static data
    private readonly Guid _indentity;
    private readonly Option<Guid> _originCardInstanceGuid;
    private readonly string _mainCardDataId;

    // Card runtime data
    private readonly List<string> _mutationCardDataIds = new();
    private readonly IReadOnlyList<ICardPropertyEntity> _properties;

    // Card components
    private readonly ICardBuffManager _buffManager;
    private readonly CardLibrary _cardLibrary;

    // from ActingCardData
    private string _actingCardDataId => _mutationCardDataIds.FirstOrDefault() ?? _mainCardDataId;
    private CardData _actingCardData => _cardLibrary.GetCardData(_actingCardDataId);
    public string CardDataId => _actingCardDataId;
    public CardType Type => _actingCardData.Type;
    public CardRarity Rarity => _actingCardData.Rarity;
    public int OriginCost => _actingCardData.Cost;
    public int OriginPower => _actingCardData.Power;
    public IEnumerable<CardTheme> Themes => _actingCardData.Themes;
    public MainTargetSelectLogic MainSelect => _actingCardData.MainSelect;
    public IEnumerable<ISubSelectionGroup> SubSelects => _actingCardData.SubSelects;
    public IEnumerable<ICardEffect> Effects => _actingCardData.Effects;
    public IReadOnlyDictionary<CardTriggeredTiming, IEnumerable<ICardEffect>> TriggeredEffects
        => _actingCardData.TriggeredEffects.ToDictionary(
            pair => pair.Timing,
            pair => (IEnumerable<ICardEffect>)pair.Effects);
    
    public Guid Identity => _indentity;
    public Option<Guid> OriginCardInstanceGuid => _originCardInstanceGuid;
    public IEnumerable<ICardPropertyEntity> Properties => _properties;
    public ICardBuffManager BuffManager => _buffManager;
    public bool IsDummy => this == DummyCard;

    public static ICardEntity DummyCard = new CardEntity(
        indentity: Guid.Empty,
        originCardInstanceGuid: Option.None<Guid>(),
        cardDataId: string.Empty,
        properties: new List<ICardPropertyEntity>(),
        buffs: new List<ICardBuffEntity>(),
        cardLibrary: null
    );
    
    private CardEntity(
        Guid indentity,
        Option<Guid> originCardInstanceGuid,
        string cardDataId,
        IEnumerable<ICardPropertyEntity> properties,
        IEnumerable<ICardBuffEntity> buffs,
        CardLibrary cardLibrary
    )
    {
        _indentity = indentity;
        _originCardInstanceGuid = originCardInstanceGuid;
        _mainCardDataId = cardDataId;
        _mutationCardDataIds = new List<string>();
        _properties = properties.ToList();
        _buffManager = new CardBuffManager(buffs);
        _cardLibrary = cardLibrary;
    }

    public static ICardEntity CreateFromInstance(CardInstance cardInstance, CardLibrary cardLibrary)
    {
        return new CardEntity(
            indentity: Guid.NewGuid(),
            originCardInstanceGuid: cardInstance.InstanceGuid.Some(),
            cardDataId: cardInstance.CardDataId,
            properties: cardLibrary.GetCardData(cardInstance.CardDataId).PropertyDatas
                .Select(p => p.CreateEntity())
                .Concat(cardInstance.AdditionPropertyDatas.Select(p => p.CreateEntity())),
            buffs: Array.Empty<ICardBuffEntity>(),
            cardLibrary: cardLibrary
        );
    }

    public static ICardEntity RuntimeCreateFromId(string cardDataId, CardLibrary cardLibrary)
    {
        return new CardEntity(
            indentity: Guid.NewGuid(),
            originCardInstanceGuid: Option.None<Guid>(),
            cardDataId: cardDataId,
            properties: cardLibrary.GetCardData(cardDataId).PropertyDatas.Select(p => p.CreateEntity()),
            buffs: Array.Empty<ICardBuffEntity>(),
            cardLibrary: cardLibrary
        );
    }

    public ICardEntity Clone(bool includeCardProperties, bool includeCardBuffs)
    {
        var cloneCard = new CardEntity(
            indentity: Guid.NewGuid(),
            originCardInstanceGuid: Option.None<Guid>(),
            cardDataId: _mainCardDataId,
            properties: includeCardProperties
                ? _properties.Select(p => p.Clone())
                : Array.Empty<ICardPropertyEntity>(),
            buffs: includeCardBuffs
                ? _buffManager.Buffs.Select(b => b.Clone())
                : Array.Empty<ICardBuffEntity>(),
            cardLibrary: _cardLibrary
        );

        return cloneCard;
    }
}

public static class CardEntityExtensions
{
    public static Option<ICardEntity> GetCard(this IGameplayModel model, Guid identity)
    {
        var allyCardOpt = model.GameStatus.Ally.CardManager.GetCardOrNone(card => card.Identity == identity);
        if (allyCardOpt.HasValue)
            return allyCardOpt;
        var enemyCardOpt = model.GameStatus.Enemy.CardManager.GetCardOrNone(card => card.Identity == identity);
        if (enemyCardOpt.HasValue)
            return enemyCardOpt;
        return Option.None<ICardEntity>();
    }

    public static Option<IPlayerEntity> Owner(this ICardEntity card, IGameplayModel model)
    {
        var gameStatus = model.GameStatus;
        var allyCardOpt = gameStatus.Ally.CardManager.GetCardOrNone(card => card.Identity == card.Identity);
        if (allyCardOpt.HasValue)
            return (gameStatus.Ally as IPlayerEntity).Some();
        var enemyCardOpt = gameStatus.Enemy.CardManager.GetCardOrNone(card => card.Identity == card.Identity);
        if (enemyCardOpt.HasValue)
            return (gameStatus.Enemy as IPlayerEntity).Some();
        return Option.None<IPlayerEntity>();
    }
    public static Faction Faction(this ICardEntity card, IGameplayModel model)
    {
        return card.Owner(model).ValueOr(DummyPlayer.Instance).Faction;
    }

    public static bool IsConsumable(this ICardEntity card)
    {
        return card.HasProperty(CardProperty.Consumable);
    }
    public static bool IsDisposable(this ICardEntity card)
    {
        return card.HasProperty(CardProperty.Dispose) || card.HasProperty(CardProperty.AutoDispose);
    }

    public static bool HasProperty(this ICardEntity card, CardProperty property)
    {
        return
            card.Properties.Any(p => p.Property == property) ||
            card.BuffManager.Buffs.Any(b => b.Properties.Any(p => p.Property == property));
    }
    
    public static int GetCardProperty(
        this ICardEntity card, TriggerContext triggerContext, CardProperty targetProperty)
    {
        var value = 0;

        var cardTrigger = new CardTrigger(card);
        var propertyContext = triggerContext with { Triggered = cardTrigger };
        foreach (var property in card.Properties.Where(p => p.Property == targetProperty))
        {
            value += property.Eval(propertyContext);
        }

        foreach (var buff in card.BuffManager.Buffs)
        {
            var cardBuffTrigger = new CardBuffTrigger(buff);
            var cardBuffContext = triggerContext with { Triggered = cardBuffTrigger };
            foreach (var property in buff.Properties.Where(p => p.Property == targetProperty))
            {
                value += property.Eval(cardBuffContext);
            }
        }

        return value;
    }
}