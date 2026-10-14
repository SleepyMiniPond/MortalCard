using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameView;
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
            IUniTaskPresenter presenter = new UniTaskPresenter();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _panel.Open(message);
                await presenter.Run(
                    _SetupSubscriptions(presenter),
                    () => true,
                    cancellationToken,
                    EventHandler);
            }
            finally
            {
                _panel.Close();
            }

            UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
                => UniTask.FromResult<IUniTaskPresenter.Event>(evt is IWarningPopupPresenter.CloseEvent
                    ? new IUniTaskPresenter.Halt() : new IUniTaskPresenter.None());
        }

        private IDisposable _SetupSubscriptions(IUniTaskPresenter presenter)
        {
            var disposables = new CompositeDisposable();
            _panel.CloseButtons
                .Select(button => button.OnClickAsObservable())
                .Merge()
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new IWarningPopupPresenter.CloseEvent()))
                .AddTo(disposables);
            return disposables;
        }
    }
}
