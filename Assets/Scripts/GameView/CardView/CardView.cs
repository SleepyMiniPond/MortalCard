using System;
using MortalGame.Presentation.Abstractions;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;
using MortalGame.UI;
using MortalGame.GameModel;
using Optional;

namespace MortalGame.GameView
{

    public interface IDragableCardView
    {
        public enum DradTargetStatus
        {
            None,
            ValidTarget,
            WithoutTarget,
        }

        IDisposable BeginDrag(int originSiblingIndex);
        void Drag(Vector2 dragPosition, DradTargetStatus dradTargetStatus);
    }

    public interface ICardView : IRecyclable, ISelectableView, IDragableCardView
    {
        public record CardClickableProperty(
            CardInfo CardInfo,
            bool IsClickable,
            Action<Guid, ICardView> OnClickCard = null,
            Action<Guid, ICardView> OnLongPressCard = null);
        public record CardSimpleProperty(
            CardInfo CardInfo);

        Canvas Canvas { get; }
        RectTransform ParentRectTransform { get; }

        void Initialize(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
        IHandCardInputSession OpenHandInput();
        void Render(CardClickableProperty property);
        void Render(CardSimpleProperty property);

        void SetPositionAndRotation(Vector3 position, Quaternion rotation);
        void AddLocationOffset(Guid guid, Vector3 offset, float duration);
        void RemoveLocationOffset(Guid guid, float duration);

        void ShowHandCardFocusContent();
        void HideHandCardFocusContent();
    }

    public class CardView : MonoBehaviour, ICardView
    {
        [BoxGroup("Content")]
        [SerializeField]
        private RectTransform _rectTransform;
        [BoxGroup("Content")]
        [SerializeField]
        private Button _button;
        [BoxGroup("Content")]
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [BoxGroup("Info")]
        [SerializeField]
        private TextMeshProUGUI _title;
        [SerializeField]
        private TextMeshProUGUI _info;
        [SerializeField]
        private TextMeshProUGUI _type;
        [SerializeField]
        private TextMeshProUGUI _cost;
        [SerializeField]
        private IntValueSwitch _costColorSwitch;
        [SerializeField]
        private IntColorMapping _powerColorMapping;

        [TitleGroup("Effect")]
        [SerializeField]
        private GameObject _selectedEffectObj;
        [SerializeField]
        private GameObject _sealedEffectObj;

        public RectTransform RectTransform => _rectTransform;
        public TargetType TargetType => TargetType.AllyCard;
        public Guid TargetIdentity => _cardIdentity;

        public RectTransform ParentRectTransform => transform.parent.GetComponent<RectTransform>();
        public Canvas Canvas => transform.GetComponentInParent<Canvas>();

        private readonly SerialDisposable _interactionSubscription = new();
        private HandInputSession _activeHandInput;
        private readonly SerialDisposable _cardInfoSubscription = new();
        private Vector3 _localPosition;
        private Quaternion _localRotation;
        private Dictionary<Guid, List<Vector3>> _offsets = new Dictionary<Guid, List<Vector3>>();
        private Tween _currentMoveTween;

        private IGameViewModel _gameViewModel;
        private LocalizeLibrary _localizeLibrary;
        private Guid _cardIdentity;

        public void Initialize(
            IGameViewModel gameInfoModel,
            LocalizeLibrary localizeLibrary)
        {
            _gameViewModel = gameInfoModel;
            _localizeLibrary = localizeLibrary;
        }

        public IHandCardInputSession OpenHandInput()
        {
            if (_activeHandInput != null)
                throw new InvalidOperationException("手牌輸入尚未釋放，不能重複綁定。");
            var session = new HandInputSession(this, _cardIdentity);
            _activeHandInput = session;
            try
            {
                _interactionSubscription.Disposable = session;
                return session;
            }
            catch { session.Dispose(); throw; }
        }

        private sealed class HandInputSession : IHandCardInputSession
        {
            private readonly CardView _owner;
            private readonly Subject<Unit> _ended = new();
            private bool _isDisposed;
            public IObservable<Guid> PointerEntered { get; }
            public IObservable<Guid> PointerExited { get; }
            public IObservable<(Guid Identity, Vector2 Position)> DragStarted { get; }
            public IObservable<(Guid Identity, Vector2 Position)> Dragged { get; }
            public IObservable<(Guid Identity, Vector2 Position)> DragEnded { get; }

            public HandInputSession(CardView owner, Guid identity)
            {
                _owner = owner;
                // 捕捉這次綁定的身份，避免物件池重用後舊事件轉成新卡片操作。
                PointerEntered = owner._button.OnPointerEnterAsObservable().Select(_ => identity).TakeUntil(_ended);
                PointerExited = owner._button.OnPointerExitAsObservable().Select(_ => identity).TakeUntil(_ended);
                DragStarted = owner._button.OnBeginDragAsObservable().Select(data => (identity, data.position)).TakeUntil(_ended);
                Dragged = owner._button.OnDragAsObservable().Select(data => (identity, data.position)).TakeUntil(_ended);
                DragEnded = owner._button.OnEndDragAsObservable().Select(data => (identity, data.position)).TakeUntil(_ended);
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                try { _ended.OnNext(Unit.Default); }
                finally
                {
                    _ended.Dispose();
                    if (ReferenceEquals(_owner._activeHandInput, this)) _owner._activeHandInput = null;
                }
            }
        }
        public void Render(ICardView.CardClickableProperty property)
        {
            var interactionSubscriptions = new CompositeDisposable();
            _interactionSubscription.Disposable = interactionSubscriptions;
            _ObserveCardInfo(property.CardInfo);
            _button.interactable = property.IsClickable;
            if (property.IsClickable)
            {
                _button.OnClickOrLongPressAsObservable()
                    .Subscribe(pressType =>
                    {
                        switch (pressType)
                        {
                            case ObservableButtonExtensions.PressType.Click:
                                property.OnClickCard?.Invoke(_cardIdentity, this);
                                break;
                            case ObservableButtonExtensions.PressType.LongPress:
                                property.OnLongPressCard?.Invoke(_cardIdentity, this);
                                break;
                        }
                    })
                    .AddTo(interactionSubscriptions);
            }
        }
        public void Render(ICardView.CardSimpleProperty property)
        {
            _interactionSubscription.Disposable = null;
            _ObserveCardInfo(property.CardInfo);
        }

        // 完整顯示狀態已由外層訂閱時，只呈現快照，避免建立第二個卡片資料來源。
        public void RenderSnapshot(CardInfo cardInfo)
        {
            _interactionSubscription.Disposable = null;
            _cardInfoSubscription.Disposable = null;
            _Render(cardInfo);
        }

        private void _ObserveCardInfo(CardInfo cardInfo)
        {
            _cardInfoSubscription.Disposable = null;
            _Render(cardInfo);

            _gameViewModel.ObservableCardInfo(cardInfo.Identity)
                .MatchSome(reactiveProp =>
                {
                    _cardInfoSubscription.Disposable = reactiveProp.Subscribe(info =>
                    {
                        _Render(info);
                    });
                });
        }

        private void _Render(CardInfo cardInfo)
        {
            var cardLocalizeData = _localizeLibrary.Get(LocalizeTitleInfoType.Card, cardInfo.CardDataID);
            var templateValue = cardInfo.GetTemplateValues();

            _cardIdentity = cardInfo.Identity;
            _cost.text = cardInfo.Cost
                .Map(cost => cost.ToString())
                .ValueOr(string.Empty);
            _costColorSwitch.Value = cardInfo.Cost
                .Map(cost =>
                    cost > cardInfo.OriginCost ? 1 :
                    cost < cardInfo.OriginCost ? 2 : 0)
                .ValueOr(0);

            var originPowerString = templateValue[CardInfo.KEY_POWER];
            templateValue[CardInfo.KEY_POWER] = cardInfo.Power
                .Map(power =>
                    power > cardInfo.OriginPower ? $"<color=#{_powerColorMapping.GetHtmlColor(1)}>{originPowerString}</color>" :
                    power < cardInfo.OriginPower ? $"<color=#{_powerColorMapping.GetHtmlColor(2)}>{originPowerString}</color>" :
                    originPowerString)
                .ValueOr(string.Empty);

            _title.text = cardInfo.DescriptionFragmentsOnly ? string.Empty : cardLocalizeData.Title;
            _info.text = CardDescriptionText.Format(cardInfo, _localizeLibrary, templateValue);
            _type.text = _localizeLibrary.Get(LocalizeTitleInfoType.KeyWord, cardInfo.Type.ToString()).Title;

            _sealedEffectObj.SetActive(cardInfo.Properties.Contains(CardProperty.Sealed));
        }

        public void OnSelect()
        {
            _selectedEffectObj.SetActive(true);
        }
        public void OnDeselect()
        {
            _selectedEffectObj.SetActive(false);
        }

        public void ShowHandCardFocusContent()
        {
            _canvasGroup.alpha = 0f;
        }
        public void HideHandCardFocusContent()
        {
            _canvasGroup.alpha = 1f;
        }

        public IDisposable BeginDrag(int originSiblingIndex)
        {
            RectTransform.rotation = Quaternion.identity;
            var originAnchoredPosition = RectTransform.anchoredPosition;
            _canvasGroup.alpha = 1f;
            transform.SetAsLastSibling();

            return Disposable.Create(() =>
            {
                RectTransform.rotation = _localRotation;
                RectTransform.anchoredPosition = originAnchoredPosition;
                _canvasGroup.alpha = 1f;
                transform.SetSiblingIndex(originSiblingIndex);
            });
        }
        public void Drag(Vector2 dragPosition, IDragableCardView.DradTargetStatus dradTargetStatus)
        {
            RectTransform.anchoredPosition = dragPosition;
            _canvasGroup.alpha = dradTargetStatus switch
            {
                IDragableCardView.DradTargetStatus.None => 0.5f,
                IDragableCardView.DradTargetStatus.ValidTarget => 0f,
                IDragableCardView.DradTargetStatus.WithoutTarget => 1f,
                _ => 1f,
            };
        }

        public void Reset()
        {
            _cardIdentity = Guid.Empty;
            _canvasGroup.alpha = 1f;
            _offsets.Clear();
            _localPosition = Vector3.zero;
            _localRotation = Quaternion.identity;
            transform.localRotation = _localRotation;
            _UpdateLocalPosition();
            _cardInfoSubscription.Disposable = null;
            _button.interactable = false;
            _interactionSubscription.Disposable = null;
            OnDeselect();
            HideHandCardFocusContent();
        }

        private void OnDestroy()
        {
            _cardInfoSubscription.Dispose();
            _interactionSubscription.Dispose();
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _localPosition = position;
            _localRotation = rotation;
            _UpdateLocalPosition();
            transform.localRotation = _localRotation;
        }

        public void AddLocationOffset(Guid guid, Vector3 offset, float duration)
        {
            if (!_offsets.ContainsKey(guid))
            {
                _offsets.Add(guid, new List<Vector3>());
            }
            _offsets[guid].Add(offset);
            _UpdateLocalPosition(duration);
        }
        public void RemoveLocationOffset(Guid guid, float duration)
        {
            if (_offsets.ContainsKey(guid))
            {
                _offsets.Remove(guid);
            }
            _UpdateLocalPosition(duration);
        }

        private void _UpdateLocalPosition(float duration = 0f)
        {
            var offset = Vector3.zero;
            foreach (var offsets in _offsets.Values)
            {
                foreach (var o in offsets)
                {
                    offset += o;
                }
            }

            if (_currentMoveTween != null && _currentMoveTween.IsActive())
            {
                _currentMoveTween.Kill();
            }

            if (duration == 0f)
            {
                transform.localPosition = _localPosition + offset;
            }
            else
            {
                _currentMoveTween = transform
                    .DOLocalMove(_localPosition + offset, 0.5f)
                    .SetEase(Ease.OutExpo)
                    .OnComplete(() => _currentMoveTween.Kill());
            }
        }
    }
}
