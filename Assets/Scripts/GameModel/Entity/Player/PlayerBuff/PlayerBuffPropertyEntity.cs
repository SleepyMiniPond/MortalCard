using Rayark.Mast;
using UnityEngine;

public interface IPlayerBuffPropertyEntity
{
    PlayerBuffProperty Property { get; }
}
public interface IPlayerBuffIntegerPropertyEntity : IPlayerBuffPropertyEntity
{
    int Eval(TriggerContext triggerContext);
}
public interface IPlayerBuffRatioPropertyEntity : IPlayerBuffPropertyEntity
{
    float Eval(TriggerContext triggerContext);
}

public class AllCardPowerPlayerBuffPropertyEntity : IPlayerBuffIntegerPropertyEntity
{
    public PlayerBuffProperty Property => PlayerBuffProperty.AllCardPower;
    private readonly IIntegerValue _value;

    public AllCardPowerPlayerBuffPropertyEntity(IIntegerValue value)
    { 
        _value = value;
    }
    
    public int Eval(TriggerContext triggerContext)
        => _value.Eval(triggerContext with { Action = new PlayerBuffPropertyLookAction(this) });
}

public class AllCardCostPlayerBuffPropertyEntity : IPlayerBuffIntegerPropertyEntity
{
    public PlayerBuffProperty Property => PlayerBuffProperty.AllCardCost;
    private readonly IIntegerValue _value;

    public AllCardCostPlayerBuffPropertyEntity(IIntegerValue value)
    {
        _value = value;
    }

    public int Eval(TriggerContext triggerContext)
        => _value.Eval(triggerContext with { Action = new PlayerBuffPropertyLookAction(this) });
}
public class NormalDamageAdditionPlayerBuffPropertyEntity : IPlayerBuffIntegerPropertyEntity
{
    public PlayerBuffProperty Property => PlayerBuffProperty.NormalDamageAddition;
    private readonly IIntegerValue _value;

    public NormalDamageAdditionPlayerBuffPropertyEntity(IIntegerValue value)
    {
        _value = value;
    }

    public int Eval(TriggerContext triggerContext)
        => _value.Eval(triggerContext with { Action = new PlayerBuffPropertyLookAction(this) });
}
public class NormalDamageRatioPlayerBuffPropertyEntity : IPlayerBuffRatioPropertyEntity
{
    public PlayerBuffProperty Property => PlayerBuffProperty.NormalDamageRatio;
    private readonly float _value;

    public NormalDamageRatioPlayerBuffPropertyEntity(float value)
    { 
        _value = value;
    }
    
    public float Eval(TriggerContext triggerContext)
        => _value;
}
public class MaxHealthPlayerBuffPropertyEntity : IPlayerBuffIntegerPropertyEntity
{
    public PlayerBuffProperty Property => PlayerBuffProperty.MaxHealth;
    private readonly IIntegerValue _value;

    public MaxHealthPlayerBuffPropertyEntity(IIntegerValue value)
    { 
        _value = value;
    }
    public int Eval(TriggerContext triggerContext)
        => _value.Eval(triggerContext with { Action = new PlayerBuffPropertyLookAction(this) });
}
public class MaxEnergyPlayerBuffPropertyEntity : IPlayerBuffIntegerPropertyEntity
{
    public PlayerBuffProperty Property => PlayerBuffProperty.MaxEnergy;
    private readonly IIntegerValue _value;

    public MaxEnergyPlayerBuffPropertyEntity(IIntegerValue value)
    { 
        _value = value;
    }
    public int Eval(TriggerContext triggerContext)
        => _value.Eval(triggerContext with { Action = new PlayerBuffPropertyLookAction(this) });
}