using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Unity.VisualScripting;
using UnityEngine;

public interface ICharacterBuffEntity
{
    string CharacterBuffDataId { get; }
    Guid Identity { get; }
    int Level { get; }
    Option<IPlayerEntity> Caster { get; }
    IReadOnlyCollection<ICharacterBuffPropertyEntity> Properties { get; }
    ICharacterBuffLifeTimeEntity LifeTime { get; }
    IReadOnlyDictionary<string, IReactionSessionEntity> ReactionSessions { get; }

    bool IsExpired();
    void AddLevel(int level);
}

public class CharacterBuffEntity : ICharacterBuffEntity
{
    private readonly string _characterBuffDataId;
    private readonly Guid _identity;
    private int _level;
    private readonly Option<IPlayerEntity> _caster;
    private readonly IReadOnlyList<ICharacterBuffPropertyEntity> _properties;
    private readonly ICharacterBuffLifeTimeEntity _lifeTime;
    private readonly IReadOnlyDictionary<string, IReactionSessionEntity> _reactionSessions;

    public string CharacterBuffDataId => _characterBuffDataId;
    public Guid Identity => _identity;
    public int Level => _level;
    public Option<IPlayerEntity> Caster => _caster;
    public IReadOnlyCollection<ICharacterBuffPropertyEntity> Properties => _properties;
    public ICharacterBuffLifeTimeEntity LifeTime => _lifeTime;
    public IReadOnlyDictionary<string, IReactionSessionEntity> ReactionSessions => _reactionSessions;

    public bool IsDummy => this == DummyBuff;
    public static ICharacterBuffEntity DummyBuff = new DummyCharacterBuff();

    public CharacterBuffEntity(
        string characterBuffDataId,
        Guid identity,
        int level,
        Option<IPlayerEntity> caster,
        IEnumerable<ICharacterBuffPropertyEntity> properties,
        ICharacterBuffLifeTimeEntity lifeTime,
        IReadOnlyDictionary<string, IReactionSessionEntity> reactionSessions) 
    {
        _characterBuffDataId = characterBuffDataId;
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

public class DummyCharacterBuff : CharacterBuffEntity
{
    public DummyCharacterBuff() : base(
        string.Empty,
        Guid.Empty,
        1,
        Option.None<IPlayerEntity>(),
        Enumerable.Empty<ICharacterBuffPropertyEntity>(),
        new AlwaysLifeTimeCharacterBuffEntity(),
        new Dictionary<string, IReactionSessionEntity>())
    {
    }
}

public static class CharacterBuffEntityExtensions
{
    public static CharacterBuffInfo ToInfo(this ICharacterBuffEntity characterBuff, IGameplayModel gameWatcher)
    {
        return new CharacterBuffInfo(
            characterBuff.CharacterBuffDataId,
            characterBuff.Identity,
            characterBuff.Level,
            characterBuff.ReactionSessions
                .Where(kvp => kvp.Value.IntegerValue.HasValue)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.IntegerValue.ValueOr(0)));
    }
    public static Option<IPlayerEntity> Owner(this ICharacterBuffEntity characterBuff, IGameplayModel gameplayWatcher)
    {
        if (gameplayWatcher.GameStatus.Ally.Characters.Any(c => c.BuffManager.Buffs.Contains(characterBuff)))
            return (gameplayWatcher.GameStatus.Ally as IPlayerEntity).Some();
        if (gameplayWatcher.GameStatus.Enemy.Characters.Any(c => c.BuffManager.Buffs.Contains(characterBuff)))
            return (gameplayWatcher.GameStatus.Enemy as IPlayerEntity).Some();
        return Option.None<IPlayerEntity>();
    }
}