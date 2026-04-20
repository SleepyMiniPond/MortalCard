using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Unity.VisualScripting;
using UnityEngine;

public interface IPlayerBuffEntity
{
    string PlayerBuffDataId { get; }
    Guid Identity { get; }
    int Level { get; }
    Option<IPlayerEntity> Caster { get; }
    IReadOnlyCollection<IPlayerBuffPropertyEntity> Properties { get; }
    IPlayerBuffLifeTimeEntity LifeTime { get; }
    IReadOnlyDictionary<string, IReactionSessionEntity> ReactionSessions { get; }

    bool IsExpired();
    void AddLevel(int level);
}

public class PlayerBuffEntity : IPlayerBuffEntity
{
    private readonly string _playerBuffDataId;
    private readonly Guid _identity;
    private int _level;
    private readonly Option<IPlayerEntity> _caster;
    private readonly IReadOnlyList<IPlayerBuffPropertyEntity> _properties;
    private readonly IPlayerBuffLifeTimeEntity _lifeTime;
    private readonly IReadOnlyDictionary<string, IReactionSessionEntity> _reactionSessions;

    public string PlayerBuffDataId => _playerBuffDataId;
    public Guid Identity => _identity;
    public int Level => _level;
    public Option<IPlayerEntity> Caster => _caster;
    public IReadOnlyCollection<IPlayerBuffPropertyEntity> Properties => _properties;
    public IPlayerBuffLifeTimeEntity LifeTime => _lifeTime;
    public IReadOnlyDictionary<string, IReactionSessionEntity> ReactionSessions => _reactionSessions;

    public bool IsDummy => this == DummyBuff;
    public static IPlayerBuffEntity DummyBuff = new DummyPlayerBuff();

    public PlayerBuffEntity(
        string playerBuffDataId,
        Guid identity,
        int level,
        Option<IPlayerEntity> caster,
        IEnumerable<IPlayerBuffPropertyEntity> properties,
        IPlayerBuffLifeTimeEntity lifeTime,
        IReadOnlyDictionary<string, IReactionSessionEntity> reactionSessions) 
    {
        _playerBuffDataId = playerBuffDataId;
        _identity = identity;
        _level = level;
        _caster = caster;
        _properties = properties.ToList();
        _lifeTime = lifeTime;
        _reactionSessions = reactionSessions;
    }

    public bool IsExpired()
    {
        return _lifeTime.IsExpired();
    }

    public void AddLevel(int level)
    {
        _level += level;
    }
}

public class DummyPlayerBuff : PlayerBuffEntity
{
    public DummyPlayerBuff() : base(
        string.Empty,
        Guid.Empty,
        1,
        Option.None<IPlayerEntity>(),
        Enumerable.Empty<IPlayerBuffPropertyEntity>(),
        new AlwaysLifeTimePlayerBuffEntity(),
        new Dictionary<string, IReactionSessionEntity>())
    {
    }
}

public static class PlayerBuffEntityExtensions
{    
    public static PlayerBuffInfo ToInfo(this IPlayerBuffEntity playerBuff, IGameplayModel gameWatcher)
    {
        return new PlayerBuffInfo(
            playerBuff.PlayerBuffDataId,
            playerBuff.Identity,
            playerBuff.Level,
            playerBuff.ReactionSessions
                .Where(kvp => kvp.Value.IntegerValue.HasValue)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.IntegerValue.ValueOr(0))
        );
    } 

    public static Option<IPlayerEntity> Owner(this IPlayerBuffEntity playerBuffEntity, IGameplayModel watcher)
    {
        if (watcher.GameStatus.Ally.BuffManager.Buffs.Contains(playerBuffEntity))
            return (watcher.GameStatus.Ally as IPlayerEntity).Some();
        if (watcher.GameStatus.Enemy.BuffManager.Buffs.Contains(playerBuffEntity))
            return (watcher.GameStatus.Enemy as IPlayerEntity).Some();
        return Option.None<IPlayerEntity>();
    }

    public static Option<bool> GetSessionBoolean(
        this IPlayerBuffEntity playerBuffEntity,
        string key) 
    {
        if (playerBuffEntity.ReactionSessions.TryGetValue(key, out var session))
        {
            return session.BooleanValue;
        }
        return Option.None<bool>();
    }
    public static Option<int> GetSessionInteger(
        this IPlayerBuffEntity playerBuffEntity,
        string key)
    {
        if (playerBuffEntity.ReactionSessions.TryGetValue(key, out var session))
        {
            return session.IntegerValue;
        }
        return Option.None<int>();
    }
}