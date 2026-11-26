using MortalGame.Presentation.Abstractions;
using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;
using UniRx;
using MortalGame.GameModel;
using MortalGame.GameView;

namespace MortalGame.Presenter
{

    public interface IAllCardDetailPresenter
    {
        public record CloseEvent() : IUniTaskPresenter.Event;
        public record CollectionEvent(CardCollectionType Type) : IUniTaskPresenter.Event;
        public record CardDetailEvent(Guid Identity) : IUniTaskPresenter.Event;

        UniTask Run(
            Faction faction,
            CardCollectionType type,
            CancellationToken cancellationToken);
    }

    public class AllCardDetailPresenter : IAllCardDetailPresenter
    {
        private readonly IAllCardDetailPanel _detailPanel;
        private readonly ISingleCardDetailPresenter _singleCardDetailPresenter;
        private readonly IGameViewModel _gameViewModel;

        public AllCardDetailPresenter(
            IAllCardDetailPanelView panelView,
            IGameViewModel gameViewModel,
            LocalizeLibrary localizeLibrary)
        {
            _detailPanel = panelView.DetailPanel;
            _singleCardDetailPresenter = new SingleCardDetailPresenter(panelView.SinglePopupPanel, gameViewModel, localizeLibrary);
            _gameViewModel = gameViewModel;

            _detailPanel.Init(_gameViewModel, localizeLibrary);
        }

        public async UniTask Run(
            Faction faction,
            CardCollectionType type,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var collectionType = new ReactiveProperty<CardCollectionType>(type);
            // 切換頁籤時替換卡區訂閱；資料通知不經過玩家操作的忙碌事件槽。
            using var state = collectionType
                .Select(currentType => _gameViewModel.ObservableCardCollectionInfo(faction, currentType)
                    .Select(collection => new AllCardDetailDisplayState(currentType,
                        Array.AsReadOnly(collection.CardInfos.Keys.ToArray()))))
                .Switch()
                .ToReadOnlyReactiveProperty();
            IUniTaskPresenter presenter = new UniTaskPresenter();
            using var session = _detailPanel.Open(state);
            await presenter.Run(
                _SetupSubscriptions(session, presenter), () => true, cancellationToken, EventHandler);

            async UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
            {
                switch (evt)
                {
                    case IAllCardDetailPresenter.CloseEvent:
                        return new IUniTaskPresenter.Halt();
                    case IAllCardDetailPresenter.CollectionEvent collection:
                        collectionType.Value = collection.Type;
                        break;
                    case IAllCardDetailPresenter.CardDetailEvent detail:
                        if (state.Value.Cards.Any(card => card.Identity == detail.Identity)
                            && _gameViewModel.GetCardInfoOrNone(detail.Identity).TryGetValue(out var latest))
                            await _singleCardDetailPresenter.Run(latest, cancellationToken);
                        break;
                }
                return new IUniTaskPresenter.None();
            }
        }

        private IDisposable _SetupSubscriptions(IAllCardDetailSession session, IUniTaskPresenter presenter)
        {
            var disposables = new CompositeDisposable();

            session.CollectionRequested
                .Subscribe(type => presenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.CollectionEvent(type)))
                .AddTo(disposables);
            session.CardClicked
                .Subscribe(identity => presenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.CardDetailEvent(identity)))
                .AddTo(disposables);
            session.CloseRequested
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.CloseEvent()))
                .AddTo(disposables);

            return disposables;
        }

    }
}
