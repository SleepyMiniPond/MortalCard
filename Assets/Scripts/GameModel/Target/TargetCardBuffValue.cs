using System;
using Optional;
using UnityEngine;

public interface ITargetCardBuffValue
{
    Option<ICardBuffEntity> Eval(TriggerContext triggerContext);
}

[Serializable]
public class NoneCardBuff : ITargetCardBuffValue
{
    public Option<ICardBuffEntity> Eval(TriggerContext triggerContext)
    {
        return Option.None<ICardBuffEntity>();
    }
}

[Serializable]
public class TriggeredCardBuff : ITargetCardBuffValue
{
    public Option<ICardBuffEntity> Eval(TriggerContext triggerContext)
    {
        return triggerContext.Triggered switch
        {
            CardBuffTrigger cardBuff => cardBuff.Buff.SomeNotNull(),
            _ => Option.None<ICardBuffEntity>()
        };
    }
}