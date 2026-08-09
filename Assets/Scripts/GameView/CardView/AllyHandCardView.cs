using System;
using MortalGame.Presentation.Abstractions;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;
using UniRx;
using Sirenix.Utilities;
using Optional.Collections;
using MortalGame.UI;
using MortalGame.GameModel;


namespace MortalGame.GameView
{

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

        private readonly SerialDisposable _handleSubscription = new();
        private readonly SerialDisposable _dragSubscription = new();
        private readonly ReactiveProperty<Option<Guid>> _currentFocusIdentity = new(Option.None<Guid>());
        private readonly ReactiveProperty<Option<(Guid, Vector2)>> _currentDragInfo = new(Option.None<(Guid, Vector2)>());
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
            if (_cardViewDict.TryGetValue(usedCardEvent.UsedCardIdentity, out var cardView))
            {
                _cardViews.Remove(cardView);
                _cardViewDict.Remove(usedCardEvent.UsedCardIdentity);
                _cardViewFactory.RecyclePrefab(cardView as CardView);

                foreach (var view in _cardViews)
                    view.RemoveLocationOffset(usedCardEvent.UsedCardIdentity, _focusDuration);
                _RearrangeCardViews();
            }
        }

        public void RemoveCardView(MoveCardEvent moveCardEvent)
        {
            if (_cardViewDict.TryGetValue(moveCardEvent.CardIdentity, out var cardView))
            {
                _cardViews.Remove(cardView);
                _cardViewDict.Remove(moveCardEvent.CardIdentity);
                _cardViewFactory.RecyclePrefab(cardView as CardView);

                foreach (var view in _cardViews)
                    view.RemoveLocationOffset(moveCardEvent.CardIdentity, _focusDuration);
                _RearrangeCardViews();
            }
        }

        public void RecycleHandCards(DiscardHandCardEvent recycleHandCardEvent)
        {
            foreach (var cardIdentity in recycleHandCardEvent.DiscardedCardIdentities.Concat(recycleHandCardEvent.ExcludedCardIdentities))
            {
                if (_cardViewDict.TryGetValue(cardIdentity, out var cardView))
                {
                    _cardViews.Remove(cardView);
                    _cardViewDict.Remove(cardIdentity);

                    _cardViewFactory.RecyclePrefab(cardView as CardView);
                    foreach (var view in _cardViews)
                        view.RemoveLocationOffset(cardIdentity, _focusDuration);
                }
            }

            _RearrangeCardViews();
        }

        public void EnableHandCardsUseCardAction(PlayerExecuteStartEvent playerExecuteStartEvent)
        {
            void OnPointerEnter(Guid identity)
            {
                if (IsDragging.Value) return;
                _currentFocusIdentity.Value = identity.Some();
            }
            void OnDragging(Guid identity, Vector2 position)
            {
                if (_currentDragInfo.Value.Map(d => d.Item1 == identity).ValueOr(false))
                    _currentDragInfo.Value = (identity, position).Some();
            }
            void OnEndDrag(Guid identity, Vector2 position)
            {
                if (_currentDragInfo.Value.Map(d => d.Item1 == identity).ValueOr(false))
                    _currentDragInfo.Value = Option.None<(Guid, Vector2)>();
            }

            var handleSubscriptions = new CompositeDisposable();
            _handleSubscription.Disposable = handleSubscriptions;

            var handCardInfos = playerExecuteStartEvent.HandCardInfo.CardInfos;
            var handCardInfoIndexes = handCardInfos.ToDictionary(
                pair => pair.Key.Identity,
                pair => pair.Value);
            foreach (var cardInfo in handCardInfos.Keys)
            {
                if (_cardViewDict.TryGetValue(cardInfo.Identity, out var cardView))
                {
                    cardView.Render(
                        new ICardView.RuntimeHandCardProperty(
                            CardInfo: cardInfo,
                            OnPointerEnter: OnPointerEnter,
                            OnPointerExit: () => _currentFocusIdentity.Value = Option.None<Guid>(),
                            OnBeginDrag: (identity, position) => _currentDragInfo.Value = (identity, position).Some(),
                            OnDrag: OnDragging,
                            OnEndDrag: OnEndDrag));

                }
            }

            _currentFocusIdentity
                .Scan(
                    seed: (Previous: Option.None<Guid>(), Current: Option.None<Guid>()),
                    accumulator: (acc, current) => (Previous: acc.Current, Current: current)
                )
                .DistinctUntilChanged()
                .Subscribe(pair => _HandleFocusIdentityChange(pair.Previous, pair.Current, handCardInfoIndexes))
                .AddTo(handleSubscriptions);

            _currentDragInfo
                .Scan(
                    seed: (Previous: Option.None<(Guid, Vector2)>(), Current: Option.None<(Guid, Vector2)>()),
                    accumulator: (acc, current) => (Previous: acc.Current, Current: current)
                )
                .Subscribe(pair => _HandleDragInfoChange(pair.Previous, pair.Current, handCardInfoIndexes))
                .AddTo(handleSubscriptions);
        }
        private void _HandleFocusIdentityChange(
            Option<Guid> previousFocusIdentityOpt,
            Option<Guid> currentFocusIdentityOpt,
            IReadOnlyDictionary<Guid, int> handCardInfoIndexes)
        {
            void ApplyLocationOffset(
                Guid focusIdentity,
                Func<int, bool> condition,
                Vector3 offset)
                => handCardInfoIndexes
                    .Where(kvp => condition(kvp.Value))
                    .SelectValue(kvp => _cardViewDict.TryGetValue(kvp.Key, out var cardView) ? cardView : null)
                    .ForEach(cardView => cardView.AddLocationOffset(focusIdentity, offset, _focusDuration));

            if (currentFocusIdentityOpt.TryGetValue(out var focusIdentity) &&
                _gameViewModel.GetCardInfoOrNone(focusIdentity).TryGetValue(out var focusCardInfo) &&
                _cardViewDict.TryGetValue(focusIdentity, out var focusCardView) &&
                handCardInfoIndexes.TryGetValue(focusIdentity, out var focusCardIndex))
            {
                ApplyLocationOffset(
                    focusIdentity: focusIdentity,
                    condition: index => index < focusCardIndex,
                    offset: new Vector3(-_focusOtherOffsetX, 0, 0));
                ApplyLocationOffset(
                    focusIdentity: focusIdentity,
                    condition: index => index > focusCardIndex,
                    offset: new Vector3(_focusOtherOffsetX, 0, 0));

                focusCardView.ShowHandCardFocusContent();
                _focusCardDetailView.ShowFocus(CardDetailProperty.Create(focusCardInfo), focusCardView.RectTransform);
            }
            else
            {
                _TryStopFocus(previousFocusIdentityOpt);
            }
        }
        private void _HandleDragInfoChange(
            Option<(Guid Identity, Vector2 Position)> previousDragInfoOpt,
            Option<(Guid Identity, Vector2 Position)> currentDragInfoOpt,
            IReadOnlyDictionary<Guid, int> handCardInfoIndexes)
        {
            if (!previousDragInfoOpt.HasValue &&
                currentDragInfoOpt.TryGetValue(out var newDragInfo) &&
                _cardViewDict.TryGetValue(newDragInfo.Identity, out var beginDradView) &&
                handCardInfoIndexes.TryGetValue(newDragInfo.Identity, out var sibilingIndex))
            {
                _TryStopFocus(_currentFocusIdentity.Value);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    beginDradView.ParentRectTransform, newDragInfo.Position, beginDradView.Canvas.worldCamera, out Vector2 localPoint);
                _beginDragPosition = beginDradView.RectTransform.anchoredPosition;
                _beginDragWorldPosition = beginDradView.RectTransform.position;
                _dragOffset = _beginDragPosition - localPoint;

                var dragSubscriptions = new CompositeDisposable
                {
                    beginDradView.BeginDrag(sibilingIndex),
                    Disposable.Create(() =>
                    {
                        _beginDragPosition = Vector2.zero;
                        _beginDragWorldPosition = Vector3.zero;
                        _dragOffset = Vector2.zero;
                        _ClearSelectedTarget();
                    })
                };
                _gameViewModel.ObservableCardInfo(newDragInfo.Identity)
                    .MatchSome(infoProperty =>
                        dragSubscriptions.Add(
                            infoProperty.Subscribe(_HandleActiveCardInfoUpdated)));
                _dragSubscription.Disposable = dragSubscriptions;
            }
            else if (previousDragInfoOpt.TryGetValue(out var latestDragInfo) &&
                    !currentDragInfoOpt.HasValue &&
                    _cardViewDict.TryGetValue(latestDragInfo.Identity, out var endDragView))
            {
                _dragSubscription.Disposable = null;

                _gameViewModel.GetCardInfoOrNone(latestDragInfo.Identity)
                    .MatchSome(cardInfo =>
                        _TryUseCardOnEndDrag(cardInfo, latestDragInfo.Position, endDragView));
            }
            else if (currentDragInfoOpt.TryGetValue(out var currentDragInfo) &&
                    previousDragInfoOpt.HasValue &&
                    _cardViewDict.TryGetValue(currentDragInfo.Identity, out var dragView) &&
                    _gameViewModel.GetCardInfoOrNone(currentDragInfo.Identity).TryGetValue(out var dragCardInfo))
            {
                _UpdateDraggingCard(dragCardInfo, currentDragInfo.Position, dragView);
            }
        }

        private void _HandleActiveCardInfoUpdated(CardInfo cardInfo)
        {
            if (_currentDragInfo.Value.TryGetValue(out var dragInfo) &&
                dragInfo.Item1 == cardInfo.Identity &&
                _cardViewDict.TryGetValue(cardInfo.Identity, out var dragView))
            {
                _UpdateDraggingCard(cardInfo, dragInfo.Item2, dragView);
            }
        }

        private void _UpdateDraggingCard(
            CardInfo dragCardInfo,
            Vector2 dragPosition,
            ICardView dragView)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragView.ParentRectTransform,
                dragPosition,
                dragView.Canvas.worldCamera,
                out var localPoint);
            var localDragPoint = localPoint + _dragOffset;

            var (dragTargetStatus, selectViewOpt) = _GetDragCardStatusAndTargetView(
                dragCardInfo,
                dragPosition,
                dragView);
            dragView.Drag(localDragPoint, dragTargetStatus);
            _UpdateSelectedViewAndLine(dragCardInfo, dragView, selectViewOpt);
        }
        private void _TryStopFocus(Option<Guid> focusIdentityOpt)
        {
            if (focusIdentityOpt.TryGetValue(out var focusIdentity) &&
                _cardViewDict.TryGetValue(focusIdentity, out var focusView))
            {
                focusView.HideHandCardFocusContent();
                _focusCardDetailView.HideFocus();

                foreach (var cardView in _cardViews)
                {
                    cardView.RemoveLocationOffset(
                        focusIdentity,
                        // focusing view dont need to play animation of return location
                        cardView == focusView ? 0f : _focusDuration);
                }
            }
        }

        public void DisableAllHandCardsAction()
        {
            _dragSubscription.Disposable = null;
            _handleSubscription.Disposable = null;
            _currentDragInfo.Value = Option.None<(Guid, Vector2)>();
            _currentFocusIdentity.Value = Option.None<Guid>();
        }

        private void OnDestroy()
        {
            _dragSubscription.Dispose();
            _handleSubscription.Dispose();
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
            {
                _ClearSelectedTarget();
                return;
            }

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
                _ClearSelectedTarget
            );
        }

        private void _ClearSelectedTarget()
        {
            _currentSelectedView?.OnDeselect();
            _currentSelectedView = null;
            _customLineRenderer.gameObject.SetActive(false);
        }
        #endregion
    }
}
