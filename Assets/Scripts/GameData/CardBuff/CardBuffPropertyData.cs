using System;
using UnityEngine;

public interface ICardBuffPropertyData
{
    ICardBuffPropertyEntity CreateEntity(TriggerContext triggerContext);
}

[Serializable]
public class SealedCardBuffPropertyData : ICardBuffPropertyData
{
    public ICardBuffPropertyEntity CreateEntity(TriggerContext triggerContext)
    {
        return new SealedCardBuffPropertyEntity();
    }
}

[Serializable]
public class PowerCardBuffPropertyData : ICardBuffPropertyData
{
    public IIntegerValue Value;

    public ICardBuffPropertyEntity CreateEntity(TriggerContext triggerContext)
    {
        return new PowerCardBuffPropertyEntity(Value);
    }
}