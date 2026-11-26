using System;
using UniRx;

namespace MortalGame.Presentation.Abstractions
{
    public interface IGameResultWinSession : IDisposable
    {
        IObservable<Unit> ContinueRequested { get; }
    }
}
