using System;
using Sirenix.OdinInspector;

public interface ICharacterBuffPropertyData
{
    ICharacterBuffPropertyEntity CreateEntity(TriggerContext triggerContext);
}

[Serializable]
public class MaxHealthPropertyCharacterBuffData : ICharacterBuffPropertyData
{
    public ICharacterBuffPropertyEntity CreateEntity(TriggerContext triggerContext)
    {
        return new MaxHealthPropertyCharacterBuffEntity();
    }
}

[Serializable]
public class MaxEnergyPropertyCharacterBuffData : ICharacterBuffPropertyData
{
    public ICharacterBuffPropertyEntity CreateEntity(TriggerContext triggerContext)
    {
        return new MaxEnergyPropertyCharacterBuffEntity();
    }
}