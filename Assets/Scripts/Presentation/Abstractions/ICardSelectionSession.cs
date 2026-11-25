using System;
using UniRx;

namespace MortalGame.Presentation.Abstractions
{
    // 代表單次開窗；呼叫端釋放時，一併解除畫面綁定與操作事件。
    public interface ICardSelectionSession : IDisposable
    {
        IObservable<Unit> CloseRequested { get; }
        IObservable<Unit> ConfirmRequested { get; }
        IObservable<Unit> VisibilityToggleRequested { get; }
        IObservable<Guid> CardClicked { get; }
        IObservable<Guid> CardLongPressed { get; }
    }
}
