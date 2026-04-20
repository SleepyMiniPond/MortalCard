using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;
using UniRx;
using Sirenix.Utilities;
using Optional.Collections;


public class AllyHandCardView : MonoBehaviour
{
    [BoxGroup("Card View")]
    [SerializeField]
    private CardViewFactory _cardViewFactory;
    [BoxGroup("Card View")]
    [SerializeField]
    private Transform _cardViewParent;

    [BoxGroup("Arc Setting")]
    [SerializeField]
    private float _arcAngle;
    [BoxGroup("Arc Setting")]
    [SerializeField]
    private float _arcRadiusX;
    [BoxGroup("Arc Setting")]
    [SerializeField]
    private float _arcRadiusY;
    [BoxGroup("Arc Setting")]
    [SerializeField]
    private float _arcStepMinAngle;

    [BoxGroup("Focus")]
    [SerializeField]
    private float _focusOtherOffsetX;
    [BoxGroup("Focus")]
    [SerializeField]
    private float _focusDuration = 0.5f;

    [BoxGroup("Arrow Setting")]
    [SerializeField]
    private CustomLineRenderer _customLineRenderer;

    private List<ICardView> _cardViews = new List<ICardView>();
    private Dictionary<Guid, ICardView> _cardViewDict = new Dictionary<Guid, ICardView>();
    private IGameplayModel _statusWatcher;
    private IGameViewModel _gameViewModel;
    private IGameplayActionReciever _reciever;
    private LocalizeLibrary _localizeLibrary;

    // Focusing
    private FocusCardDetailView _focusCardDetailView;
    // Dragging
    private Vector2 _beginDragPosition;
    private Vector2 _beginDragWorldPosition;
    private Vector2 _dragOffset;
    private ISelectableView _currentSelectedView;

    public IEnumerable<ISelectableView> SelectableViews => _cardViews;
    
    private CompositeDisposable _handleDisposables = new ();
    private CompositeDisposable _dragDisposables = new ();
    private readonly ReactiveProperty<Option<CardInfo>> _currentFocusInfo = new(Option.None<CardInfo>());
    private readonly ReactiveProperty<Option<(CardInfo, Vector2)>> _currentDragInfo = new(Option.None<(CardInfo, Vector2)>());
    private IReadOnlyReactiveProperty<bool> IsDragging => _currentDragInfo.Select(info => info.HasValue).ToReactiveProperty();

    public void Init(
        IGameplayModel statusWatcher,
        IGameplayActionReciever reciever,
        IGameViewModel gameInfoModel,
        IAllCardDetailPanelView allCardDetailPanelView,
        LocalizeLibrary localizeLibrary)
    {
        _statusWatcher = statusWatcher;
        _reciever = reciever;
        _gameViewModel = gameInfoModel;
        _focusCardDetailView = allCardDetailPanelView.FocusCardDetailView;
        _localizeLibrary = localizeLibrary;
    }

    public void CreateCardView(CardInfo newCardInfo)
    {
        var cardView = _cardViewFactory.CreatePrefab();
        cardView.Initialize(_gameViewModel, _localizeLibrary);
        cardView.transform.SetParent(_cardViewParent, false);
        cardView.Render(new ICardView.CardSimpleProperty(newCardInfo));
        _cardViews.Add(cardView);
        _cardViewDict.Add(newCardInfo.Identity, cardView);

        _RearrangeCardViews();
    }
    
    public void RemoveCardView(UsedCardEvent usedCardEvent)
    {
        if(_cardViewDict.TryGetValue(usedCardEvent.UsedCardInfo.Identity, out var cardView))
        {
            _cardViews.Remove(cardView);
            _cardViewDict.Remove(usedCardEvent.UsedCardInfo.Identity);
            _cardViewFactory.RecyclePrefab(cardView as CardView);

            foreach(var view in _cardViews)
                view.RemoveLocationOffset(usedCardEvent.UsedCardInfo.Identity, _focusDuration);
            _RearrangeCardViews();
        }
    }

    public void RemoveCardView(MoveCardEvent moveCardEvent)
    {
        if(_cardViewDict.TryGetValue(moveCardEvent.CardInfo.Identity, out var cardView))
        {
            _cardViews.Remove(cardView);
            _cardViewDict.Remove(moveCardEvent.CardInfo.Identity);
            _cardViewFactory.RecyclePrefab(cardView as CardView);

            foreach(var view in _cardViews)
                view.RemoveLocationOffset(moveCardEvent.CardInfo.Identity, _focusDuration);
            _RearrangeCardViews();
        }
    }

    public void RecycleHandCards(DiscardHandCardEvent recycleHandCardEvent)
    {
        foreach(var cardInfo in recycleHandCardEvent.DiscardedCardInfos.Concat(recycleHandCardEvent.ExcludedCardInfos))
        {
            if(_cardViewDict.TryGetValue(cardInfo.Identity, out var cardView))
            {
                _cardViews.Remove(cardView);
                _cardViewDict.Remove(cardInfo.Identity);
                
                _cardViewFactory.RecyclePrefab(cardView as CardView);
                foreach(var view in _cardViews)
                    view.RemoveLocationOffset(cardInfo.Identity, _focusDuration);
            }
        }

        _RearrangeCardViews();
    }
    
    public void EnableHandCardsUseCardAction(PlayerExecuteStartEvent playerExecuteStartEvent)
    {
        void OnPointerEnter(CardInfo info)
        {
            if (IsDragging.Value) return;
            _currentFocusInfo.Value = info.Some();
        }
        void OnDragging(CardInfo info, Vector2 position)
        {
            if (_currentDragInfo.Value.Map(d => d.Item1.Identity == info.Identity).ValueOr(false))
                _currentDragInfo.Value = (info, position).Some();
        }
        void OnEndDrag(CardInfo info, Vector2 position)
        {
            if (_currentDragInfo.Value.Map(d => d.Item1.Identity == info.Identity).ValueOr(false))
                _currentDragInfo.Value = Option.None<(CardInfo, Vector2)>();
        }

        _handleDisposables.Dispose();
        _handleDisposables = new CompositeDisposable();

        var handCardInfos = playerExecuteStartEvent.HandCardInfo.CardInfos;
        foreach (var cardInfo in handCardInfos.Keys)
        {
            if (_cardViewDict.TryGetValue(cardInfo.Identity, out var cardView))
            {
                cardView.Render(
                    new ICardView.RuntimeHandCardProperty(
                        CardInfo: cardInfo,
                        OnPointerEnter: info => OnPointerEnter(info),
                        OnPointerExit: info => _currentFocusInfo.Value = Option.None<CardInfo>(),
                        OnBeginDrag: (info, position) => _currentDragInfo.Value = (info, position).Some(),
                        OnDrag: (info, position) => OnDragging(info, position),
                        OnEndDrag: (info, position) => OnEndDrag(info, position)));
            }
        }

        _currentFocusInfo
            .Scan(
                seed: (Previous: Option.None<CardInfo>(), Current: Option.None<CardInfo>()),
                accumulator: (acc, current) => (Previous: acc.Current, Current: current)
            )
            .DistinctUntilChanged()
            .Subscribe(pair => _HandleFocusInfoChange(pair.Previous, pair.Current, handCardInfos))
            .AddTo(_handleDisposables);

        _currentDragInfo
            .Scan(
                seed: (Previous: Option.None<(CardInfo, Vector2)>(), Current: Option.None<(CardInfo, Vector2)>()),
                accumulator: (acc, current) => (Previous: acc.Current, Current: current)
            )
            .Subscribe(pair => _HandleDragInfoChange(pair.Previous, pair.Current, handCardInfos))
            .AddTo(_handleDisposables);
    }
    private void _HandleFocusInfoChange(
        Option<CardInfo> previousFocusInfoOpt,
        Option<CardInfo> currentFocusInfoOpt,
        IReadOnlyDictionary<CardInfo, int> handCardInfoIndexes)
    {
        void ApplyLocationOffset(
            Guid focusIdentity,
            Func<int, bool> condition,
            Vector3 offset)
            => handCardInfoIndexes
                .Where(kvp => condition(kvp.Value))
                .SelectValue(kvp => _cardViewDict.TryGetValue(kvp.Key.Identity, out var cardView) ? cardView : null)
                .ForEach(cardView => cardView.AddLocationOffset(focusIdentity, offset, _focusDuration));

        if (currentFocusInfoOpt.TryGetValue(out var focusCardInfo) &&
            _cardViewDict.TryGetValue(focusCardInfo.Identity, out var focusCardView) &&
            handCardInfoIndexes.TryGetValue(focusCardInfo, out var focusCardIndex))
        {
            ApplyLocationOffset(
                focusIdentity: focusCardInfo.Identity,
                condition: index => index < focusCardIndex,
                offset: new Vector3(-_focusOtherOffsetX, 0, 0));
            ApplyLocationOffset(
                focusIdentity: focusCardInfo.Identity,
                condition: index => index > focusCardIndex,
                offset: new Vector3(_focusOtherOffsetX, 0, 0));

            focusCardView.ShowHandCardFocusContent();
            _focusCardDetailView.ShowFocus(CardDetailProperty.Create(focusCardInfo), focusCardView.RectTransform);
        }
        else
        {
            _TryStopFocus(previousFocusInfoOpt);
        }
    }
    private void _HandleDragInfoChange(
        Option<(CardInfo Info, Vector2 Position)> previousDragInfoOpt,
        Option<(CardInfo Info, Vector2 Position)> currentDragInfoOpt,
        IReadOnlyDictionary<CardInfo, int> handCardInfos)
    {
        if (!previousDragInfoOpt.HasValue &&
            currentDragInfoOpt.TryGetValue(out var newDragInfo) &&
            _cardViewDict.TryGetValue(newDragInfo.Info.Identity, out var beginDradView) &&
            handCardInfos.TryGetValue(newDragInfo.Info, out var sibilingIndex))
        {
            _TryStopFocus(_currentFocusInfo.Value);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                beginDradView.ParentRectTransform, newDragInfo.Position, beginDradView.Canvas.worldCamera, out Vector2 localPoint);
            _beginDragPosition = beginDradView.RectTransform.anchoredPosition;
            _beginDragWorldPosition = beginDradView.RectTransform.position;
            _dragOffset = _beginDragPosition - localPoint;

            _dragDisposables.Clear();
            _dragDisposables.Add(beginDradView.BeginDrag(sibilingIndex));
            _dragDisposables.Add(Disposable.Create(() =>
            {
                _beginDragPosition = Vector2.zero;
                _beginDragWorldPosition = Vector3.zero;
                _dragOffset = Vector2.zero;
                _currentSelectedView?.OnDeselect();
                _currentSelectedView = null;
                _customLineRenderer.gameObject.SetActive(false);
            }));
        }
        else if (previousDragInfoOpt.TryGetValue(out var latestDragInfo) &&
                !currentDragInfoOpt.HasValue &&
                _cardViewDict.TryGetValue(latestDragInfo.Info.Identity, out var endDragView) &&
                handCardInfos.TryGetValue(latestDragInfo.Info, out var originIndex))
        {
            _dragDisposables.Clear();

            _TryUseCardOnEndDrag(latestDragInfo.Info, latestDragInfo.Position, endDragView);
        }
        else if (currentDragInfoOpt.TryGetValue(out var currentDragInfo) &&
                previousDragInfoOpt.HasValue &&
                _cardViewDict.TryGetValue(currentDragInfo.Info.Identity, out var dragView))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragView.ParentRectTransform, currentDragInfo.Position, dragView.Canvas.worldCamera, out Vector2 localPoint);
            var localDragPoint = localPoint + _dragOffset;

            var (dragTargetStatus, selectViewOpt) = _GetDragCardStatusAndTargetView(
                currentDragInfo.Info, currentDragInfo.Position, dragView);
            dragView.Drag(localDragPoint, dragTargetStatus);

            _UpdateSelectedViewAndLine(currentDragInfo.Info, dragView, selectViewOpt);
        }
    }
    private void _TryStopFocus(Option<CardInfo> focusInfoOpt)
    { 
        if (focusInfoOpt.TryGetValue(out var focusInfo) &&
            _cardViewDict.TryGetValue(focusInfo.Identity, out var focusView))
        {
            focusView.HideHandCardFocusContent();
            _focusCardDetailView.HideFocus();

            foreach (var cardView in _cardViews)
            {
                cardView.RemoveLocationOffset(
                    focusInfo.Identity,
                    // focusing view dont need to play animation of return location
                    cardView == focusView ? 0f : _focusDuration);
            }
        }
    }

    public void DisableAllHandCardsAction()
    {
        _handleDisposables.Dispose();
        _handleDisposables = new CompositeDisposable();
    }

    private void _RearrangeCardViews()
    {
        var cardCount = _cardViews.Count;
        if (cardCount <= 0) return;

        float centerIndex = (cardCount - 1) / 2f;
        var angleStep = _arcAngle / (cardCount - 1);
        angleStep = Mathf.Min(angleStep, _arcStepMinAngle);

        for (var i = 0; i < cardCount; i++)
        {
            var cardView = _cardViews[i];
            float angle = 90 + (centerIndex - i) * angleStep;

            var x = _arcRadiusX * Mathf.Cos(angle * Mathf.Deg2Rad);
            var y = _arcRadiusY * Mathf.Sin(angle * Mathf.Deg2Rad);
            var localPosition = new Vector3(x, y, 0);
            var localRotation = Quaternion.Euler(0, 0, angle - 90);
            cardView.SetPositionAndRotation(localPosition, localRotation);
        }
    }

    #region TargetEventLogic
    private bool _TryUseCardOnEndDrag(CardInfo dragCardInfo, Vector2 dragCardPosition, ICardView dragCardView)
    {
        if (dragCardInfo.MainSelectable.SelectType != SelectType.None)
        {
            return OptionCollectionExtensions.FirstOrNone(_reciever.SelectableViews
                .Where(view => view != dragCardView &&
                    dragCardInfo.MainSelectable.SelectType.IsSelectable(view.TargetType) &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        view.RectTransform, dragCardPosition, dragCardView.Canvas.worldCamera)))
                .Match(
                    selectView =>
                    {
                        _reciever.RecieveEvent(new UseCardCommand(dragCardInfo.Identity, (selectView as ISelectionTarget).Some()));
                        return true;
                    },
                    () => false);
        }
        else if (RectTransformUtility.RectangleContainsScreenPoint(
                _reciever.BasicSelectableView.RectTransform, dragCardPosition, dragCardView.Canvas.worldCamera))
        {
            _reciever.RecieveEvent(new UseCardCommand(dragCardInfo.Identity));
            return true;
        }
        return false;
    }

    private (IDragableCardView.DradTargetStatus Status, Option<ISelectableView> SelectView) _GetDragCardStatusAndTargetView(
        CardInfo dragCardInfo,
        Vector2 dragCardPosition,
        ICardView dragCardView)
    {
        if (dragCardInfo.MainSelectable.SelectType != SelectType.None)
        {
            var selectView = OptionCollectionExtensions
                .FirstOrNone(_reciever.SelectableViews
                    .Where(view => view != dragCardView &&
                        dragCardInfo.MainSelectable.SelectType.IsSelectable(view.TargetType) &&
                        RectTransformUtility.RectangleContainsScreenPoint(
                            view.RectTransform, dragCardPosition, dragCardView.Canvas.worldCamera)));
            if (selectView.HasValue)
            {
                return (IDragableCardView.DradTargetStatus.ValidTarget, selectView);
            }
            else
            {
                return (IDragableCardView.DradTargetStatus.None, Option.None<ISelectableView>());
            }
        }
        else if (RectTransformUtility.RectangleContainsScreenPoint(
                _reciever.BasicSelectableView.RectTransform, dragCardPosition, dragCardView.Canvas.worldCamera))
        {
            return (IDragableCardView.DradTargetStatus.WithoutTarget, _reciever.BasicSelectableView.Some());
        }
        else
        {
            return (IDragableCardView.DradTargetStatus.None, Option.None<ISelectableView>());
        }
    }
    
    private void _UpdateSelectedViewAndLine(
        CardInfo dragCardInfo,
        ICardView dragCardView,
        Option<ISelectableView> selectViewOpt)
    {
        if (dragCardInfo.MainSelectable.SelectType == SelectType.None)
            return;
            
        selectViewOpt.Match(
            selectView =>
            {
                if (_currentSelectedView != selectView)
                {
                    selectView?.OnSelect();
                    _currentSelectedView?.OnDeselect();
                    _currentSelectedView = selectView;
                }
                
                _customLineRenderer.gameObject.SetActive(true);
                _customLineRenderer.SetLineProperty(_beginDragWorldPosition, selectView.RectTransform);
            },
            () => _customLineRenderer.gameObject.SetActive(false)
        );
    }
    #endregion
}