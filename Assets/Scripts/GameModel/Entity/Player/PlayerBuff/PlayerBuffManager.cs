using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using UnityEngine;

public interface IPlayerBuffManager
{
    IReadOnlyCollection<IPlayerBuffEntity> Buffs { get; }
    ModifyPlayerBuffResult ModifyBuffLevel(
        string buffId, 
        int level);
    AddPlayerBuffResult AddBuff(
        IPlayerBuffEntity newBuff);
    RemovePlayerBuffResult RemoveBuff(
        IPlayerBuffEntity existBuff);

    IEnumerable<IPlayerBuffEntity> Update(TriggerContext triggerContext);
}

public class PlayerBuffManager : IPlayerBuffManager
{
    private List<IPlayerBuffEntity> _buffs;

    public IReadOnlyCollection<IPlayerBuffEntity> Buffs => _buffs;

    public PlayerBuffManager()
    {
        _buffs = new List<IPlayerBuffEntity>();
    }

    public ModifyPlayerBuffResult ModifyBuffLevel(
        string buffId, 
        int level)
    {
        foreach (var existBuff in _buffs)
        {
            if (existBuff.PlayerBuffDataId == buffId)
            {
                existBuff.AddLevel(level);
                return new ModifyPlayerBuffResult(
                    PlayerBuff: existBuff,
                    DeltaLevel: level,
                    NewLevel: existBuff.Level
                );
            }
        }

        throw new Exception($"Player buff {buffId} not found to modify level.");
    }

    public AddPlayerBuffResult AddBuff(IPlayerBuffEntity newBuff)
    {
        if (_buffs.Any(buff => buff.PlayerBuffDataId == newBuff.PlayerBuffDataId))
        {
            throw new Exception($"Player buff {newBuff.PlayerBuffDataId} already exists to add.");
        }

        _buffs.Add(newBuff);
        return new AddPlayerBuffResult(newBuff);
    }
    
    public RemovePlayerBuffResult RemoveBuff(IPlayerBuffEntity existBuff)
    {
        _buffs.Remove(existBuff);

        return new RemovePlayerBuffResult(existBuff);
    }

    public IEnumerable<IPlayerBuffEntity> Update(TriggerContext triggerContext)
    {
        foreach (var buff in _buffs.ToList())
        {
            var isUpdated = false;
            var triggerBuff = new PlayerBuffTrigger(buff);
            var updateBuffTriggerContext = triggerContext with { Triggered = triggerBuff };

            foreach (var session in buff.ReactionSessions.Values)
            {
                isUpdated |= session.Update(updateBuffTriggerContext);
            }

            isUpdated |= buff.LifeTime.Update(updateBuffTriggerContext);

            if (isUpdated)
            {
                yield return buff;
            }
        }
    }
}
