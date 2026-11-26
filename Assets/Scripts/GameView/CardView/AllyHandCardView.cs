using System;
using MortalGame.Presentation.Abstractions;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;
using UniRx;
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
        public IEnumerable<ISelectableView> SelectableViews => _cardViews;

        private readonly SerialDisposable _handleSubscription = new();
        private readonly SerialDisposable _dragSubscription = new();
        private readonly SerialDisposable _focusSubscription = new();
        private readonly ReactiveProperty<Option<Guid>> _currentFocusIdentity = new(Option.None<Guid>());
        private HandCardDragSession CurrentDrag => _dragSubscription.Disposable as HandCardDragSession;
        private bool IsDragging => CurrentDrag != null;

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
            if (_TryRecycleCardView(usedCardEvent.UsedCardIdentity))
                _RearrangeCardViews();
        }

        public void RemoveCardView(MoveCardEvent moveCardEvent)
        {
            if (_TryRecycleCardView(moveCardEvent.CardIdentity))
                _RearrangeCardViews();
        }

        public void RecycleHandCards(DiscardHandCardEvent recycleHandCardEvent)
        {
            foreach (var cardIdentity in recycleHandCardEvent.DiscardedCardIdentities.Concat(recycleHandCardEvent.ExcludedCardIdentities))
                _TryRecycleCardView(cardIdentity);

            _RearrangeCardViews();
        }

        private bool _TryRecycleCardView(Guid identity)
        {
            if (!_cardViewDict.TryGetValue(identity, out var cardView))
                return false;

            if (_currentFocusIdentity.Value.Map(current => current == identity).ValueOr(false))
            {
                _StopFocus();
                _currentFocusIdentity.Value = Option.None<Guid>();
            }

            if (CurrentDrag?.Identity == identity) _StopDrag();

            _cardViews.Remove(cardView);
            _cardViewDict.Remove(identity);
            _cardViewFactory.RecyclePrefab(cardView as CardView);
            foreach (var view in _cardViews)
                view.RemoveLocationOffset(identity, _focusDuration);

            return true;
        }

        public void EnableHandCardsUseCardAction(CardCollectionInfo handCardInfo)
        {
            var handleSubscriptions = new CompositeDisposable();
            _handleSubscription.Disposable = handleSubscriptions;

            var handCardInfos = handCardInfo.CardInfos;
            var handCardInfoIndexes = handCardInfos.ToDictionary(
                pair => pair.Key.Identity,
                pair => pair.Value);
            foreach (var cardInfo in handCardInfos.Keys)
            {
                if (_cardViewDict.TryGetValue(cardInfo.Identity, out var cardView))
                {
                    var input = cardView.OpenHandInput().AddTo(handleSubscriptions);
                    input.PointerEntered.Subscribe(OnPointerEnter).AddTo(handleSubscriptions);
                    input.PointerExited.Subscribe(identity =>
                    {
                        if (IsActive() && _currentFocusIdentity.Value.Map(current => current == identity).ValueOr(false))
                            _currentFocusIdentity.Value = Option.None<Guid>();
                    }).AddTo(handleSubscriptions);
                    input.DragStarted.Subscribe(pointer =>
                    {
                        if (IsActive()) _BeginDrag(pointer.Identity, pointer.Position, handCardInfoIndexes);
                    }).AddTo(handleSubscriptions);
                    input.Dragged.Subscribe(pointer => OnDragging(pointer.Identity, pointer.Position)).AddTo(handleSubscriptions);
                    input.DragEnded.Subscribe(pointer => OnEndDrag(pointer.Identity, pointer.Position)).AddTo(handleSubscriptions);
                }
            }

            _currentFocusIdentity
                .Subscribe(identity => _HandleFocusIdentityChange(identity, handCardInfoIndexes))
                .AddTo(handleSubscriptions);

            handleSubscriptions.Add(Disposable.Create(_StopDrag));
            bool IsActive() => ReferenceEquals(_handleSubscription.Disposable, handleSubscriptions);
            void OnPointerEnter(Guid identity)
            {
                if (!IsActive() || IsDragging) return;
                _currentFocusIdentity.Value = identity.Some();
            }
            void OnDragging(Guid identity, Vector2 position)
            {
                if (IsActive() && CurrentDrag?.Identity == identity) CurrentDrag.Update(position);
            }
            void OnEndDrag(Guid identity, Vector2 position)
            {
                if (!IsActive() || CurrentDrag?.Identity != identity) return;
                Option<UseCardCommand> command;
                try { command = CurrentDrag.Release(position); }
                finally { _StopDrag(); }
                command.MatchSome(value => _reciever.RecieveEvent(value));
            }
        }
        private void _HandleFocusIdentityChange(
            Option<Guid> currentFocusIdentityOpt,
            IReadOnlyDictionary<Guid, int> handCardInfoIndexes)
        {
            _StopFocus();
            if (currentFocusIdentityOpt.TryGetValue(out var focusIdentity) &&
                _gameViewModel.ObservableCardInfo(focusIdentity).TryGetValue(out var focusCardSource) &&
                _cardViewDict.TryGetValue(focusIdentity, out var focusCardView) &&
                handCardInfoIndexes.ContainsKey(focusIdentity))
            {
                _focusSubscription.Disposable = new HandCardFocusSession(
                    focusIdentity, focusCardView, focusCardSource, _focusCardDetailView,
                    _cardViews, handCardInfoIndexes, _focusOtherOffsetX, _focusDuration);
            }
        }
        private void _BeginDrag(Guid identity, Vector2 position, IReadOnlyDictionary<Guid, int> indexes)
        {
            if (IsDragging || !_cardViewDict.TryGetValue(identity, out var card)
                || !indexes.TryGetValue(identity, out var index)
                || !_gameViewModel.ObservableCardInfo(identity).TryGetValue(out var source)) return;
            _StopFocus();
            _dragSubscription.Disposable = new HandCardDragSession(
                identity, card, index, position, source, _reciever, _customLineRenderer);
        }

        private void _StopDrag() => _dragSubscription.Disposable = null;
        private void _StopFocus() => _focusSubscription.Disposable = null;

        public void DisableAllHandCardsAction()
        {
            _dragSubscription.Disposable = null;
            _handleSubscription.Disposable = null;
            _StopFocus();
            _currentFocusIdentity.Value = Option.None<Guid>();
        }

        private void OnDestroy()
        {
            _dragSubscription.Dispose();
            _handleSubscription.Dispose();
            _focusSubscription.Dispose();
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

    }
}
