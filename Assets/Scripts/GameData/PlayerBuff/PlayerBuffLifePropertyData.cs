using System;
using MortalGame.GameModel;
using Sirenix.OdinInspector;

namespace MortalGame.GameData
{

public interface IPlayerBuffPropertyData
{
}

[Serializable]
public class AllCardPowerPlayerBuffPropertyData : IPlayerBuffPropertyData
{
    public IIntegerValue Value;

}
[Serializable]
public class AllCardCostPlayerBuffPropertyData : IPlayerBuffPropertyData
{
    public IIntegerValue Value;

}

[Serializable]
public class NormalDamageAdditionPlayerBuffPropertyData : IPlayerBuffPropertyData
{
    public IIntegerValue Value;

}
[Serializable]
public class NormalDamageRatioPlayerBuffPropertyData : IPlayerBuffPropertyData
{
    public float Value;

}

[Serializable]
public class MaxHealthPlayerBuffPropertyData : IPlayerBuffPropertyData
{
    public IIntegerValue Value;
}

[Serializable]
public class MaxEnergyPlayerBuffPropertyData : IPlayerBuffPropertyData
{
    public IIntegerValue Value;
}

}
