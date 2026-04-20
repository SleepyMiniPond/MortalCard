using System;

public interface IPlayerBuffLifeTimeData
{
    IPlayerBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext);
}

[Serializable]
public class AlwaysLifeTimePlayerBuffData : IPlayerBuffLifeTimeData
{
    public IPlayerBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext)
    {
        return new AlwaysLifeTimePlayerBuffEntity();
    }
}

[Serializable]
public class PlayerBuffTurnLifeTimeData : IPlayerBuffLifeTimeData
{
    public IIntegerValue Turn;

    public IPlayerBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext)
    {
        return new TurnLifeTimePlayerBuffEntity(Turn.Eval(triggerContext));
    }
}