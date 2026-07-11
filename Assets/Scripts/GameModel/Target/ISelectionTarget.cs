using System;
using MortalGame.GameData;

namespace MortalGame.GameModel
{

    public interface ISelectionTarget
    {
        TargetType TargetType { get; }
        Guid TargetIdentity { get; }
    }

}
