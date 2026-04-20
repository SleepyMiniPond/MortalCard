using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;
using UnityEngine;

public class PlayerBuffLibrary
{
    private readonly Dictionary<string, PlayerBuffData> _buffs;

    public PlayerBuffLibrary(IReadOnlyDictionary<string, PlayerBuffData> buffs)
    {
        _buffs = new Dictionary<string, PlayerBuffData>(buffs);
    }

    public Option<ConditionalPlayerBuffEffect[]> GetBuffEffects(string buffId, GameTiming triggerTiming)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"PlayerBuff ID[{buffId}] not found in library.");
            return Option.None<ConditionalPlayerBuffEffect[]>();
        }

        return _buffs[buffId].BuffEffects.TryGetValue(triggerTiming, out var effects)
            ? effects.SomeNotNull()
            : Option.None<ConditionalPlayerBuffEffect[]>();
    }

    public IReadOnlyDictionary<string, IReactionSessionData> GetBuffSessions(string buffId)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"PlayerBuff ID[{buffId}] not found in library.");
            return new Dictionary<string, IReactionSessionData>();
        }

        return _buffs[buffId].Sessions;
    }

    public IPlayerBuffLifeTimeData GetBuffLifeTime(string buffId)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"PlayerBuff ID[{buffId}] not found in library.");
            return null;
        }

        return _buffs[buffId].LifeTimeData;
    }

    public IPlayerBuffPropertyData[] GetBuffProperties(string buffId)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"PlayerBuff ID[{buffId}] not found in library.");
            return Array.Empty<IPlayerBuffPropertyData>();
        }

        return _buffs[buffId].PropertyDatas.ToArray();
    }
}
