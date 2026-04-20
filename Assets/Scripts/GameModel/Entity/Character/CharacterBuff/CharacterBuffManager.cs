using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using UnityEngine;

public interface ICharacterBuffManager
{
    IReadOnlyCollection<ICharacterBuffEntity> Buffs { get; }

    ModifyCharacterBuffResult ModifyBuffLevel(
        string buffId, 
        int level);
    AddCharacterBuffResult AddBuff(ICharacterBuffEntity newBuff);
    RemoveCharacterBuffResult RemoveBuff(ICharacterBuffEntity existBuff);
    
    IEnumerable<ICharacterBuffEntity> Update(TriggerContext triggerContext);
}

public class CharacterBuffManager : ICharacterBuffManager
{
    private List<ICharacterBuffEntity> _buffs;

    public IReadOnlyCollection<ICharacterBuffEntity> Buffs => _buffs;

    public CharacterBuffManager()
    {
        _buffs = new List<ICharacterBuffEntity>();
    }

    public ModifyCharacterBuffResult ModifyBuffLevel(
        string buffId, 
        int level)
    {
        foreach (var existBuff in _buffs)
        {
            if (existBuff.CharacterBuffDataId == buffId)
            {
                existBuff.AddLevel(level);
                return new ModifyCharacterBuffResult(
                    CharacterBuff: existBuff,
                    DeltaLevel: level,
                    NewLevel: existBuff.Level
                );
            }
        }

        throw new Exception($"Player buff {buffId} not found to modify level.");
    }

    public AddCharacterBuffResult AddBuff(ICharacterBuffEntity newBuff)
    {
        if (_buffs.Any(buff => buff.CharacterBuffDataId == newBuff.CharacterBuffDataId))
        {
            throw new Exception($"Player buff {newBuff.CharacterBuffDataId} already exists to add.");
        }

        _buffs.Add(newBuff);
        return new AddCharacterBuffResult(newBuff);
    }

    public RemoveCharacterBuffResult RemoveBuff(ICharacterBuffEntity existBuff)
    {
        _buffs.Remove(existBuff);

        return new RemoveCharacterBuffResult(existBuff);
    }

    public IEnumerable<ICharacterBuffEntity> Update(TriggerContext triggerContext)
    {
        foreach (var buff in _buffs.ToList())
        {
            var isUpdated = false;
            var triggeredBuff = new CharacterBuffTrigger(buff);
            var updateCharacterBuffContext = triggerContext with { Triggered = triggeredBuff };
            foreach (var session in buff.ReactionSessions.Values)
            {
                isUpdated |= session.Update(updateCharacterBuffContext);
            }

            isUpdated |= buff.LifeTime.Update(updateCharacterBuffContext);

            if (isUpdated)
            { 
                yield return buff;
            }
        }
    }
}
