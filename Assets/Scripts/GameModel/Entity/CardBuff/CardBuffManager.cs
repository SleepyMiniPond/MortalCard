using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using UnityEngine;

public interface ICardBuffManager
{
    IReadOnlyCollection<ICardBuffEntity> Buffs { get; }
    ModifyCardBuffLevelResult ModifyBuffLevel(string buffId, int level);
    AddCardBuffResult AddBuff(ICardBuffEntity newBuff);
    RemoveCardBuffResult RemoveBuff(ICardBuffEntity existBuff);

    bool Update(TriggerContext triggerContext);
}

public class CardBuffManager : ICardBuffManager
{
    private readonly List<ICardBuffEntity> _buffs;

    public IReadOnlyCollection<ICardBuffEntity> Buffs => _buffs;

    public CardBuffManager(IEnumerable<ICardBuffEntity> buffs)
    {
        _buffs = new List<ICardBuffEntity>(buffs);
    }

    public ModifyCardBuffLevelResult ModifyBuffLevel(string buffId, int level)
    {
        foreach (var existBuff in _buffs)
        {
            if (existBuff.CardBuffDataID == buffId)
            {
                existBuff.AddLevel(level);
                return new ModifyCardBuffLevelResult(
                    CardBuff: existBuff,
                    DeltaLevel: level,
                    NewLevel: existBuff.Level);
            }
        }

        throw new Exception($"Card buff {buffId} not found to modify level.");
    }

    public AddCardBuffResult AddBuff(ICardBuffEntity newBuff)
    {
        if (_buffs.Any(buff => buff.CardBuffDataID == newBuff.CardBuffDataID))
        {
            throw new Exception($"Card buff {newBuff.CardBuffDataID} already exists to add.");
        }

        _buffs.Add(newBuff);
        return new AddCardBuffResult(newBuff);
    }

    public RemoveCardBuffResult RemoveBuff(ICardBuffEntity existBuff)
    {
        _buffs.Remove(existBuff);

        return new RemoveCardBuffResult(existBuff);
    }

    public bool Update(TriggerContext triggerContext)
    {
        var isUpdated = false;
        foreach (var buff in _buffs.ToList())
        {
            var triggerBuff = new CardBuffTrigger(buff);
            var updateBuffContext = triggerContext with
            {
                Triggered = triggerBuff
            };
            foreach (var session in buff.ReactionSessions.Values)
            {
                isUpdated |= session.Update(updateBuffContext);
            }

            isUpdated |= buff.LifeTime.Update(updateBuffContext);
        }
        return isUpdated;
    }    
}