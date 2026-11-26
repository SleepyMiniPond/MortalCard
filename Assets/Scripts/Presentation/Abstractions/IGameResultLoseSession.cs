using System;
using MortalGame.GameModel;

namespace MortalGame.Presentation.Abstractions
{
    public interface IGameResultLoseSession : IDisposable
    {
        IObservable<LoseReactionType> ReactionRequested { get; }
    }
}
