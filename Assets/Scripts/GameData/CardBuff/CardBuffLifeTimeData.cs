using System;
using UnityEngine;

public interface ICardBuffLifeTimeData
{
    ICardBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext);
}

[Serializable]
public class AlwaysLifeTimeCardBuffData : ICardBuffLifeTimeData
{
    public ICardBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext)
    {
        return new AlwaysLifeTimeCardBuffEntity();
    }
}

[Serializable]
public class TurnLifeTimeCardBuffData : ICardBuffLifeTimeData
{
    public IIntegerValue Turn;

    public ICardBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext)
    {
        return new TurnLifeTimeCardBuffEntity(Turn.Eval(triggerContext));
    }
}

[Serializable]
public class HandCardLifeTimeCardBuffData : ICardBuffLifeTimeData
{
    public ICardBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext)
    {
        return new HandCardLifeTimeCardBuffEntity();
    }
}