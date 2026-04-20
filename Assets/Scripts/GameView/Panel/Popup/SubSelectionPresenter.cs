using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;


public interface ISubSelectionPresenter
{
    public record CloseEvent : IUniTaskPresenter.Event;
    public record ConfirmEvent : IUniTaskPresenter.Event;
    public record VisibleToggleEvent : IUniTaskPresenter.Event;
    public record SelectCardEvent(CardInfo CardInfo, ICardView CardView) : IUniTaskPresenter.Event;
    public record LongTouchCardEvent(CardInfo CardInfo, ICardView CardView) : IUniTaskPresenter.Event;

    UniTask<IReadOnlyDictionary<string, ISubSelectionAction>> RunSubSelection(SubSelectionInfo subSelectionInfoOpt);
}

public class SubSelectionPresenter : ISubSelectionPresenter
{
    private readonly ICardSelectionPanel _cardSelectionPanel;
    private readonly SingleCardDetailPopupPanel _singleCardDetailPopupPanel;
    private readonly IUniTaskPresenter _uniTaskPresenter;

    public SubSelectionPresenter(
        IGameViewModel gameViewModel,
        LocalizeLibrary localizeLibrary,
        SingleCardDetailPopupPanel singleCardDetailPopupPanel,
        ICardSelectionPanel cardSelectionPanel)
    {
        _singleCardDetailPopupPanel = singleCardDetailPopupPanel;
        _singleCardDetailPopupPanel.Init(gameViewModel, localizeLibrary);
        _cardSelectionPanel = cardSelectionPanel;
        _cardSelectionPanel.Init(gameViewModel, localizeLibrary);

        _uniTaskPresenter = new UniTaskPresenter();
    }

    public async UniTask<IReadOnlyDictionary<string, ISubSelectionAction>> RunSubSelection(SubSelectionInfo subSelectionInfo)
    {
        var subSelectionActions = new Dictionary<string, ISubSelectionAction>();

        foreach (var kvp in subSelectionInfo.SelectionInfos)
        {
            switch (kvp.Value)
            {
                case ExistCardSelectionInfo existCardSelection:
                    var subSelectionAction = await _RunExistCardSelection(existCardSelection);
                    subSelectionActions[kvp.Key] = subSelectionAction;
                    break;
            }
        }

        return subSelectionActions;
    }
    
    private async UniTask<ExistCardSubSelectionAction> _RunExistCardSelection(ExistCardSelectionInfo existCardSelection)
    {
        var isClose = false;
        var isVisible = true;
        var selectedCardIds = new List<Guid>();
        var disposables = new CompositeDisposable();

        _cardSelectionPanel.Open(CreateProperty());

        await _uniTaskPresenter.Run(
            disposables,
            () => !isClose,
            CancellationToken.None,
            EventHandler);

        _cardSelectionPanel.Close();

        return new ExistCardSubSelectionAction(selectedCardIds);

        UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
        {
            switch (evt)
            {
                case ISubSelectionPresenter.CloseEvent:
                    return UniTask.FromResult<IUniTaskPresenter.Event>(
                        existCardSelection.IsMustSelect
                        ? new IUniTaskPresenter.None()
                        : new IUniTaskPresenter.Halt());

                case ISubSelectionPresenter.ConfirmEvent:
                    var canClose = existCardSelection.IsMustSelect
                        ? selectedCardIds.Count >= existCardSelection.Count
                        : true;
                    return UniTask.FromResult<IUniTaskPresenter.Event>(canClose
                        ? new IUniTaskPresenter.Halt()
                        : new IUniTaskPresenter.None());

                case ISubSelectionPresenter.VisibleToggleEvent:
                    isVisible = !isVisible;
                    _cardSelectionPanel.RenderUpdate(GetUpdateProperty());
                    break;
                
                case ISubSelectionPresenter.SelectCardEvent selectCardEvent:
                    {
                        if (selectedCardIds.Contains(selectCardEvent.CardInfo.Identity))
                        {
                            selectedCardIds.Remove(selectCardEvent.CardInfo.Identity);
                        }
                        else if (selectedCardIds.Count < existCardSelection.Count)
                        {
                            selectedCardIds.Add(selectCardEvent.CardInfo.Identity);
                        }

                        _cardSelectionPanel.RenderUpdate(GetUpdateProperty());
                    }
                    break;

                case ISubSelectionPresenter.LongTouchCardEvent longTouchCardEvent:
                    return _singleCardDetailPopupPanel
                        .Run(CardDetailProperty.Create(longTouchCardEvent.CardInfo))
                        .ContinueWith<IUniTaskPresenter.Event>(
                            () => new IUniTaskPresenter.None());
            }

            return UniTask.FromResult<IUniTaskPresenter.Event>(
                new IUniTaskPresenter.None());
        }

        ICardSelectionPanel.SelectionProperty CreateSelectionProperty(CardInfo cardInfo)
        {
            return new ICardSelectionPanel.SelectionProperty(
                cardInfo.Identity.ToString(),
                cardInfo,
                (info, cardView) =>
                    _uniTaskPresenter.TryEnqueueNextEvent(new ISubSelectionPresenter.SelectCardEvent(info, cardView)),
                (info, cardView) =>
                    _uniTaskPresenter.TryEnqueueNextEvent(new ISubSelectionPresenter.LongTouchCardEvent(info, cardView))
            );
        }
        ICardSelectionPanel.Property CreateProperty()
        {
            return new ICardSelectionPanel.Property(
                existCardSelection.IsMustSelect,
                !existCardSelection.IsMustSelect,
                existCardSelection.Count,
                existCardSelection.CardInfos
                    .Select(cardInfo => CreateSelectionProperty(cardInfo)),
                OnClose: () => _uniTaskPresenter.TryEnqueueNextEvent(new ISubSelectionPresenter.CloseEvent()),
                OnConfirm: () => _uniTaskPresenter.TryEnqueueNextEvent(new ISubSelectionPresenter.ConfirmEvent()),
                OnVisibleToggle: () => _uniTaskPresenter.TryEnqueueNextEvent(new ISubSelectionPresenter.VisibleToggleEvent())
            );
        }
        ICardSelectionPanel.UpdateProperty GetUpdateProperty()
        {
            return new ICardSelectionPanel.UpdateProperty(
                isVisible,
                existCardSelection.IsMustSelect
                    ? selectedCardIds.Count >= existCardSelection.Count
                    : true,
                existCardSelection.Count,
                existCardSelection.CardInfos
                    .Where(cardInfo => selectedCardIds.Contains(cardInfo.Identity))
            );
        }
    }
}
