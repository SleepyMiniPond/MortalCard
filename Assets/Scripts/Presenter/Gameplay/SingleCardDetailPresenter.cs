using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using UniRx;

namespace MortalGame.Presenter
{
    public interface ISingleCardDetailPresenter
    {
        public record CloseEvent : IUniTaskPresenter.Event;
        UniTask Run(CardInfo card, CancellationToken cancellationToken);
    }

    public class SingleCardDetailPresenter : ISingleCardDetailPresenter
    {
        private readonly ISingleCardDetailPopupPanel _panel;
        private readonly IGameViewModel _gameViewModel;

        public SingleCardDetailPresenter(ISingleCardDetailPopupPanel panel,
            IGameViewModel gameViewModel, LocalizeLibrary localizeLibrary)
        {
            _panel = panel;
            _gameViewModel = gameViewModel;
            _panel.Init(gameViewModel, localizeLibrary);
        }

        public async UniTask Run(CardInfo card, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // 戰鬥卡訂閱目前資訊；獨立預覽卡沒有來源，保留傳入的預覽內容。
            var source = _gameViewModel.ObservableCardInfo(card.Identity)
                .Match<IObservable<CardInfo>>(value => value, () => Observable.Return(card));
            using var state = source.Select(value => new SingleCardDetailDisplayState(value))
                .ToReadOnlyReactiveProperty();
            IUniTaskPresenter presenter = new UniTaskPresenter();
            using var session = _panel.Open(state);
            await presenter.Run(
                session.CloseRequested.Subscribe(_ => presenter.TryEnqueueNextEvent(new ISingleCardDetailPresenter.CloseEvent())),
                () => true, cancellationToken,
                evt => UniTask.FromResult<IUniTaskPresenter.Event>(evt is ISingleCardDetailPresenter.CloseEvent
                    ? new IUniTaskPresenter.Halt() : new IUniTaskPresenter.None()));
        }
    }
}
