using System;
using System.Collections.Generic;
using Optional;
using UnityEngine;

public interface ITargetPlayerBuffValue
{
    Option<IPlayerBuffEntity> Eval(TriggerContext triggerContext);
}

[Serializable]
public class NoneBuff : ITargetPlayerBuffValue
{
    public Option<IPlayerBuffEntity> Eval(TriggerContext triggerContext)
    {
        return Option.None<IPlayerBuffEntity>();
    }
}
[Serializable]
public class TriggeredPlayerBuff : ITargetPlayerBuffValue
{
    public Option<IPlayerBuffEntity> Eval(TriggerContext triggerContext)
    {
        return triggerContext.Triggered switch
        {
            PlayerBuffTrigger playerBuffTrigger => playerBuffTrigger.Buff.Some(),
            _ => Option.None<IPlayerBuffEntity>()
        };
    }
}
