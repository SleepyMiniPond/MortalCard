using System;
using System.Collections.Generic;
using Optional;
using Optional.Collections;
using UnityEngine;

public class CardBuffLibrary
{
    private readonly Dictionary<string, CardBuffData> _buffs;

    public CardBuffLibrary(IReadOnlyDictionary<string, CardBuffData> cardBuffs)
    {
        _buffs = new Dictionary<string, CardBuffData>(cardBuffs);
    }

    public CardBuffData GetCardBuffData(string buffId)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"CardBuff ID[{buffId}] not found in library.");
            return null;
        }

        return _buffs[buffId];
    }

    public Option<ConditionalCardBuffEffect[]> GetBuffEffects(string buffId, GameTiming triggerTiming)
    {
        if (!_buffs.ContainsKey(buffId))
        {
            Debug.LogError($"CardBuff ID[{buffId}] not found in library.");
            return Option.None<ConditionalCardBuffEffect[]>();
        }

        return _buffs[buffId].BuffEffects.TryGetValue(triggerTiming, out var effects)
            ? effects.SomeNotNull()
            : Option.None<ConditionalCardBuffEffect[]>();
    }
}
