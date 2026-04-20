using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;
using UnityEngine;

public class CharacterBuffLibrary
{
    private readonly Dictionary<string, CharacterBuffData> _buffs;

    public CharacterBuffLibrary(IReadOnlyDictionary<string, CharacterBuffData> buffs)
    {
        _buffs = new Dictionary<string, CharacterBuffData>(buffs);
    }

    public Option<ConditionalCharacterBuffEffect[]> GetBuffEffects(string buffId, GameTiming triggerTiming)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"CharacterBuff ID[{buffId}] not found in library.");
            return Option.None<ConditionalCharacterBuffEffect[]>();
        }

        return _buffs[buffId].BuffEffects.TryGetValue(triggerTiming, out var effects)
            ? effects.SomeNotNull()
            : Option.None<ConditionalCharacterBuffEffect[]>();
    }

    public IReadOnlyDictionary<string, IReactionSessionData> GetBuffSessions(string buffId)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"CharacterBuff ID[{buffId}] not found in library.");
            return null;
        }

        return _buffs[buffId].Sessions;
    }
    
    public ICharacterBuffLifeTimeData GetBuffLifeTime(string buffId)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"CharacterBuff ID[{buffId}] not found in library.");
            return null;
        }

        return _buffs[buffId].LifeTimeData;
    }

    public ICharacterBuffPropertyData[] GetBuffProperties(string buffId)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"CharacterBuff ID[{buffId}] not found in library.");
            return Array.Empty<ICharacterBuffPropertyData>();
        }

        return _buffs[buffId].PropertyDatas.ToArray();
    }
}
