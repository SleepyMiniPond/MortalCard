using System;

namespace MortalGame.GameModel
{

public interface ISelectionTarget
{
    TargetType TargetType { get; }
    Guid TargetIdentity { get; }
}

}
