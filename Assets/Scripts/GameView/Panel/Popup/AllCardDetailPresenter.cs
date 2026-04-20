using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;
using UniRx;

public interface IAllCardDetailPresenter
{
    public record CloseEvent() : IUniTaskPresenter.Event;
    public record DeckEvent(CardCollectionInfo DeckInfo) : IUniTaskPresenter.Event;
    public record HandCardEvent(CardCollectionInfo HandCardInfo) : IUniTaskPresenter.Event;
    public record GraveyardEvent(CardCollectionInfo GraveyardInfo) : IUniTaskPresenter.Event;
    public record CardDetailEvent(CardInfo CardInfo, ICardView CardView) : IUniTaskPresenter.Event;

    UniTask Run(
        Faction faction,
        CardCollectionType type,
        CancellationToken cancellationToken);
}

public record CardDetailProperty(
    ICardView.CardSimpleProperty CardProperty,
    CardPropertyHint.ViewData CardBuffHint,
    CardPropertyHint.ViewData CardKeywordHint)
{
    public static CardDetailProperty Create(CardInfo cardInfo)
    {
        return new CardDetailProperty(
            CardProperty: new ICardView.CardSimpleProperty(cardInfo),
            CardBuffHint: new CardPropertyHint.ViewData(
                cardInfo.BuffInfos
                    .Select(buffInfo =>
                        new CardPropertyHint.InfoCellViewData(
                            LocalizeTitleInfoType.CardBuff,
                            buffInfo.CardBuffDataId,
                            buffInfo.GetTemplateValues()))
                    .ToArray()),
            CardKeywordHint: new CardPropertyHint.ViewData(
                cardInfo.Keywords
                    .Select(keyword =>
                        new CardPropertyHint.InfoCellViewData(
                            LocalizeTitleInfoType.KeyWord,
                            keyword,
                            Utility.Dictionary<string, string>.EMPTY))
                    .ToArray()));
    }
}

public class AllCardDetailPresenter : IAllCardDetailPresenter
{
    private readonly IAllCardDetailPanel _detailPanel;
    private readonly SingleCardDetailPopupPanel _singlePopupPanel;
    private readonly IGameViewModel _gameViewModel;

    private CardInfo _selectedCardInfo;
    private IUniTaskPresenter _uniTaskPresenter;
    private bool _isClose;

    public AllCardDetailPresenter(
        IAllCardDetailPanelView panelView,
        IGameViewModel gameViewModel,
        LocalizeLibrary localizeLibrary)
    {
        _detailPanel = panelView.DetailPanel;
        _singlePopupPanel = panelView.SinglePopupPanel;
        _gameViewModel = gameViewModel;

        _detailPanel.Init(_gameViewModel, localizeLibrary);
        _uniTaskPresenter = new UniTaskPresenter();
    }

    public async UniTask Run(
        Faction faction,
        CardCollectionType type,
        CancellationToken cancellationToken)
    {
        _isClose = false;
        _detailPanel.Open();

        var cardCollectionInfo = _gameViewModel.ObservableCardCollectionInfo(faction, type);
        _ShowCardCollectionInfos(cardCollectionInfo.Value);

        await _uniTaskPresenter.Run(
            _SetupSubscriptions(faction),
            () => !_isClose,
            cancellationToken,
            EventHandler);

        _detailPanel.Close();
        
        async UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
        {
            switch (evt)
            {
                case IAllCardDetailPresenter.CloseEvent:
                    _isClose = true;
                    return new IUniTaskPresenter.Halt();
                case IAllCardDetailPresenter.DeckEvent deckEvent:
                    _ShowCardCollectionInfos(deckEvent.DeckInfo);
                    break;

                case IAllCardDetailPresenter.GraveyardEvent graveyardEvent:
                    _ShowCardCollectionInfos(graveyardEvent.GraveyardInfo);
                    break;

                case IAllCardDetailPresenter.HandCardEvent handCardEvent:
                    _ShowCardCollectionInfos(handCardEvent.HandCardInfo);
                    break;

                case IAllCardDetailPresenter.CardDetailEvent cardDetailEvent:
                    await _singlePopupPanel.Run(CardDetailProperty.Create(cardDetailEvent.CardInfo));
                    break;
            }
            return new IUniTaskPresenter.None();
        }
    }

    private IDisposable _SetupSubscriptions(Faction faction)
    {
        var disposables = new CompositeDisposable();

        _detailPanel.DeckButton.OnClickAsObservable()
            .WithLatestFrom(
                _gameViewModel.ObservableCardCollectionInfo(faction, CardCollectionType.Deck),
                (_, deckInfo) => deckInfo)
            .Subscribe(deckInfo => _uniTaskPresenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.DeckEvent(deckInfo)))
            .AddTo(disposables);
        _detailPanel.HandCardButton.OnClickAsObservable()
            .WithLatestFrom(
                _gameViewModel.ObservableCardCollectionInfo(faction, CardCollectionType.HandCard),
                (_, handCardsInfo) => handCardsInfo)
            .Subscribe(handCardsInfo => _uniTaskPresenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.HandCardEvent(handCardsInfo)))
            .AddTo(disposables);
        _detailPanel.GraveyardButton.OnClickAsObservable()
            .WithLatestFrom(
                _gameViewModel.ObservableCardCollectionInfo(faction, CardCollectionType.Graveyard),
                (_, graveyardInfo) => graveyardInfo)
            .Subscribe(graveyardInfo => _uniTaskPresenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.GraveyardEvent(graveyardInfo)))
            .AddTo(disposables);
        _detailPanel.CloseButtons
            .Select(button => button.OnClickAsObservable())
            .Merge()
            .Subscribe(_ => _uniTaskPresenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.CloseEvent()))
            .AddTo(disposables);
        
        return disposables;
    }

    private void _ShowCardCollectionInfos(CardCollectionInfo cardCollectionInfo)
    {
        var property = new IAllCardDetailPanel.Property(
            cardCollectionInfo.CardInfos.Keys.Select(cardInfo =>
                new ICardView.CardClickableProperty(
                    CardInfo: cardInfo,
                    IsClickable: true,
                    OnClickCard: (info, cardView) => 
                        _uniTaskPresenter.TryEnqueueNextEvent(new IAllCardDetailPresenter.CardDetailEvent(info, cardView))))
            );
        _detailPanel.Render(property);
    }
}
