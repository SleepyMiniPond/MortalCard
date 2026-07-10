using System;
using MortalGame.GameModel;
using UnityEngine;

namespace MortalGame.GameData
{

public interface ICardBuffLifeTimeData
{
}

[Serializable]
public class AlwaysLifeTimeCardBuffData : ICardBuffLifeTimeData
{
}

[Serializable]
public class TurnLifeTimeCardBuffData : ICardBuffLifeTimeData
{
    public IIntegerValue Turn;
}

[Serializable]
public class HandCardLifeTimeCardBuffData : ICardBuffLifeTimeData
{
}

}
