using System;

public interface ICharacterBuffLifeTimeData
{
    ICharacterBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext);
}

[Serializable]
public class AlwaysLifeTimeCharacterBuffData : ICharacterBuffLifeTimeData
{
    public ICharacterBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext)
    {
        return new AlwaysLifeTimeCharacterBuffEntity();
    }
}

[Serializable]
public class TurnLifeTimeCharacterBuffData : ICharacterBuffLifeTimeData
{
    public int Turn;

    public ICharacterBuffLifeTimeEntity CreateEntity(TriggerContext triggerContext)
    {
        return new TurnLifeTimeCharacterBuffEntity(Turn);
    }
}