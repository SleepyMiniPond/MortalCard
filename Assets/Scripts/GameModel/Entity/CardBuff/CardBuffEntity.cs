using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Unity.VisualScripting;
using UnityEngine;

public interface ICardBuffEntity
{
    string CardBuffDataID { get; }
    Guid Identity { get; }
    int Level { get; }
    Option<IPlayerEntity> Caster { get; }
    IReadOnlyCollection<ICardBuffPropertyEntity> Properties { get; }
    ICardBuffLifeTimeEntity LifeTime { get; }
    IReadOnlyDictionary<string, IReactionSessionEntity> ReactionSessions { get; }
    IReadOnlyDictionary<CardTriggeredTiming, IEnumerable<ConditionalCardBuffEffect>> Effects { get; }
    IEnumerable<string> Keywords { get; }

    bool IsExpired();
    void AddLevel(int level);
    ICardBuffEntity Clone();
}

public class CardBuffEntity : ICardBuffEntity
{
    private readonly string _cardBuffDataId;
    private readonly Guid _identity;
    private int _level;
    private readonly Option<IPlayerEntity> _caster;
    private readonly IReadOnlyList<ICardBuffPropertyEntity> _properties;
    private readonly ICardBuffLifeTimeEntity _lifeTime;
    private readonly IReadOnlyDictionary<string, IReactionSessionEntity> _reactionSessions;
    private readonly CardBuffLibrary _cardBuffLibrary;

    public string CardBuffDataID => _cardBuffDataId;
    public Guid Identity => _identity;
    public int Level => _level;
    public Option<IPlayerEntity> Caster => _caster;
    public IReadOnlyCollection<ICardBuffPropertyEntity> Properties => _properties;
    public ICardBuffLifeTimeEntity LifeTime => _lifeTime;
    public IReadOnlyDictionary<string, IReactionSessionEntity> ReactionSessions => _reactionSessions;
    public IReadOnlyDictionary<CardTriggeredTiming, IEnumerable<ConditionalCardBuffEffect>> Effects =>
        _cardBuffLibrary.GetCardBuffData(_cardBuffDataId).Effects.ToDictionary(
            kvp => kvp.Key,
            kvp => (IEnumerable<ConditionalCardBuffEffect>)kvp.Value
        );
    public IEnumerable<string> Keywords
        => Effects.Keys.Where(timing => timing != CardTriggeredTiming.None)
            .Select(t => t.ToString())
            .Concat(_properties.SelectMany(p => p.Keywords))
            .Distinct();

    private CardBuffEntity(
        string cardBuffDataID,
        Guid identity,
        int level,
        Option<IPlayerEntity> caster,
        IEnumerable<ICardBuffPropertyEntity> properties,
        ICardBuffLifeTimeEntity lifeTime,
        IReadOnlyDictionary<string, IReactionSessionEntity> reactionSessions,
        CardBuffLibrary cardBuffLibrary)
    {
        _cardBuffDataId = cardBuffDataID;
        _identity = identity;
        _level = level;
        _caster = caster;
        _properties = properties.ToList();
        _lifeTime = lifeTime;
        _reactionSessions = reactionSessions;
        _cardBuffLibrary = cardBuffLibrary;
    }

    public static CardBuffEntity CreateFromData(
        string cardBuffDataID,
        int level,
        Option<IPlayerEntity> caster,
        TriggerContext triggerContext,
        CardBuffLibrary cardBuffLibrary)
    {
        var buffData = cardBuffLibrary.GetCardBuffData(cardBuffDataID);
        var properties = buffData.PropertyDatas
            .Select(p => p.CreateEntity(triggerContext));
        var lifeTime = buffData.LifeTimeData.CreateEntity(triggerContext);
        var reactionSessions = buffData.Sessions.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.CreateEntity(triggerContext)
        );

        return new CardBuffEntity(
            cardBuffDataID: cardBuffDataID,
            identity: Guid.NewGuid(),
            level: level,
            caster: caster,
            properties: properties,
            lifeTime: lifeTime,
            reactionSessions: reactionSessions,
            cardBuffLibrary: cardBuffLibrary
        );
    }

    public bool IsExpired()
    {
        return LifeTime.IsExpired();
    }

    public void AddLevel(int level)
    {
        _level += level;
    }

    public ICardBuffEntity Clone()
    {
        return new CardBuffEntity(
            cardBuffDataID: _cardBuffDataId,
            identity: Guid.NewGuid(),
            level: _level,
            caster: _caster,
            properties: _properties.Select(p => p.Clone()),
            lifeTime: _lifeTime.Clone(),
            reactionSessions: _reactionSessions.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Clone()
            ),
            cardBuffLibrary: _cardBuffLibrary
        );
    }
}

public static class CardBuffEntityExtensions
{
    public static Option<IPlayerEntity> Owner(this ICardBuffEntity cardBuff, IGameplayModel gameplayWatcher)
    {
        if (gameplayWatcher.GameStatus.Ally.CardManager.GetCardOrNone(card => card.BuffManager.Buffs.Contains(cardBuff)).HasValue)
            return (gameplayWatcher.GameStatus.Ally as IPlayerEntity).Some();
        if (gameplayWatcher.GameStatus.Enemy.CardManager.GetCardOrNone(card => card.BuffManager.Buffs.Contains(cardBuff)).HasValue)
            return (gameplayWatcher.GameStatus.Enemy as IPlayerEntity).Some();
        return Option.None<IPlayerEntity>();
    }
}
