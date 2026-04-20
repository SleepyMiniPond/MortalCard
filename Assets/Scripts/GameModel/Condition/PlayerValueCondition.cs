using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IPlayerValueCondition
{
    bool Eval(TriggerContext triggerContext, IPlayerEntity player);
}

[Serializable]
public class PlayerFactionCondition : IPlayerValueCondition
{
    public enum FactionCondition
    {
        Same,
        Opposite
    }

    [HorizontalGroup("1")]

    public ITargetPlayerValue ComparePlayer;
    public FactionCondition Faction;

    public bool Eval(TriggerContext triggerContext, IPlayerEntity player)
    {
        return ComparePlayer
            .Eval(triggerContext)
            .Match(
                comparePlayer => Faction switch
                {
                    FactionCondition.Same     => player.Faction == comparePlayer.Faction,
                    FactionCondition.Opposite => player.Faction != comparePlayer.Faction,
                    _                          => false
                },
                () => false);
    }
}

[Serializable]
public class PlayerEnergyCondition : IPlayerValueCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<IPlayerEnergyCondition> Conditions = new ();

    public bool Eval(TriggerContext triggerContext, IPlayerEntity player)
    {
        return Conditions.All(c => c.Eval(triggerContext, player.EnergyManager));
    }
}

public interface IPlayerEnergyCondition
{
    bool Eval(TriggerContext triggerContext, IEnergyManager energyManager);
}

[Serializable]
public class EnergyIntegerCondition : IPlayerEnergyCondition
{
    public enum EnergyValueType
    {
        Current,
        Max
    }

    public EnergyValueType ValueType;

    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<IIntegerValueCondition> Conditions = new ();

    public bool Eval(TriggerContext triggerContext, IEnergyManager energyManager)
    {
        return ValueType switch
        {
            EnergyValueType.Current => Conditions.All(c => c.Eval(triggerContext, energyManager.Energy)),
            EnergyValueType.Max     => Conditions.All(c => c.Eval(triggerContext, energyManager.MaxEnergy)),
            _                       => false
        };
    }
}