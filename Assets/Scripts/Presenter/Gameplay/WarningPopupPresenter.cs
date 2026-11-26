using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using UniRx;

namespace MortalGame.Presenter
{
    public interface IWarningPopupPresenter
    {
        public record CloseEvent : IUniTaskPresenter.Event;

        UniTask Run(string message, CancellationToken cancellationToken);
    }

    public class WarningPopupPresenter : IWarningPopupPresenter
    {
        private readonly IWarningPopupPanel _panel;

        public WarningPopupPresenter(IWarningPopupPanel panel)
        {
            _panel = panel;
        }

        public async UniTask Run(string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var state = new ReactiveProperty<WarningPopupDisplayState>(new(message));
            IUniTaskPresenter presenter = new UniTaskPresenter();
            using var session = _panel.Open(state);
            await presenter.Run(
                _SetupSubscriptions(session, presenter),
                () => true,
                cancellationToken,
                EventHandler);

            UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
                => UniTask.FromResult<IUniTaskPresenter.Event>(evt is IWarningPopupPresenter.CloseEvent
                    ? new IUniTaskPresenter.Halt() : new IUniTaskPresenter.None());
        }

        private IDisposable _SetupSubscriptions(IWarningPopupSession session, IUniTaskPresenter presenter)
            => session.CloseRequested.Subscribe(_ =>
                presenter.TryEnqueueNextEvent(new IWarningPopupPresenter.CloseEvent()));
    }
}
