using System;
using UniRx;

namespace MortalGame.Presentation.Abstractions
{
    public interface IWarningPopupSession : IDisposable
    {
        IObservable<Unit> CloseRequested { get; }
    }
}
