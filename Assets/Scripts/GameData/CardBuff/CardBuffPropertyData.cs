using System;
using MortalGame.GameModel;
using UnityEngine;

namespace MortalGame.GameData
{

public interface ICardBuffPropertyData
{
}

[Serializable]
public class SealedCardBuffPropertyData : ICardBuffPropertyData
{
}

[Serializable]
public class PowerCardBuffPropertyData : ICardBuffPropertyData
{
    public IIntegerValue Value;
}

}
