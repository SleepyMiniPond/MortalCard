using System;

namespace MortalGame.GameData
{

public interface ICharacterBuffLifeTimeData
{
}

[Serializable]
public class AlwaysLifeTimeCharacterBuffData : ICharacterBuffLifeTimeData
{
}

[Serializable]
public class TurnLifeTimeCharacterBuffData : ICharacterBuffLifeTimeData
{
    public int Turn;

}

}
