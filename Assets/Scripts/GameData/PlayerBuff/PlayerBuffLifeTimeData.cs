using System;
using MortalGame.GameModel;

namespace MortalGame.GameData
{

    public interface IPlayerBuffLifeTimeData
    {
    }

    [Serializable]
    public class AlwaysLifeTimePlayerBuffData : IPlayerBuffLifeTimeData
    {
    }

    [Serializable]
    public class PlayerBuffTurnLifeTimeData : IPlayerBuffLifeTimeData
    {
        public IIntegerValue Turn;

    }

}
