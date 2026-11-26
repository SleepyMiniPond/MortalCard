using System;
using UniRx;

namespace MortalGame.Presentation.Abstractions
{
    public interface ISingleCardDetailSession : IDisposable
    {
        IObservable<Unit> CloseRequested { get; }
    }
}
