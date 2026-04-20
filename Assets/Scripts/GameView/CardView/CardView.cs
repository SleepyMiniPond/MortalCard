using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

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
    public record RuntimeHandCardProperty(
        CardInfo CardInfo,
        Action<CardInfo> OnPointerEnter = null,
        Action<CardInfo> OnPointerExit = null,
        Action<CardInfo, Vector2> OnBeginDrag = null,
        Action<CardInfo, Vector2> OnDrag = null,
        Action<CardInfo, Vector2> OnEndDrag = null);
    public record CardClickableProperty(
        CardInfo CardInfo,
        bool IsClickable,
        Action<CardInfo, ICardView> OnClickCard = null,
        Action<CardInfo, ICardView> OnLongPressCard = null);
    public record CardSimpleProperty(
        CardInfo CardInfo);

    Canvas Canvas { get; }
    RectTransform ParentRectTransform { get; }

    void Initialize(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
    void Render(RuntimeHandCardProperty property);
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

    private CompositeDisposable _disposables = new();
    private IDisposable _cardInfoSubscription;
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

    public void Render(ICardView.RuntimeHandCardProperty property)
    {
        _disposables.Clear();
        _gameViewModel.ObservableCardInfo(property.CardInfo.Identity)
            .MatchSome(reactiveProp =>
            {
                _cardInfoSubscription?.Dispose();
                _cardInfoSubscription = reactiveProp
                    .Subscribe(info => _Render(info));
            });

        _button.OnPointerEnterAsObservable()
            .Subscribe(_ => property.OnPointerEnter?.Invoke(property.CardInfo))
            .AddTo(_disposables);
        _button.OnPointerExitAsObservable()
            .Subscribe(_ => property.OnPointerExit?.Invoke(property.CardInfo))
            .AddTo(_disposables);
        _button.OnBeginDragAsObservable()
            .Subscribe(eventData => property.OnBeginDrag?.Invoke(property.CardInfo, eventData.position))
            .AddTo(_disposables);
        _button.OnDragAsObservable()
            .Subscribe(eventData => property.OnDrag?.Invoke(property.CardInfo, eventData.position))
            .AddTo(_disposables);
        _button.OnEndDragAsObservable()
            .Subscribe(eventData => property.OnEndDrag?.Invoke(property.CardInfo, eventData.position))
            .AddTo(_disposables);

        _Render(property.CardInfo);
    }
    public void Render(ICardView.CardClickableProperty property)
    {
        _disposables.Clear();
        _button.interactable = property.IsClickable;
        if (property.IsClickable)
        {
            _button.OnClickOrLongPressAsObservable()
                .Subscribe(pressType =>
                {
                    switch (pressType)
                    {
                        case ObservableButtonExtensions.PressType.Click:
                            property.OnClickCard?.Invoke(property.CardInfo, this);
                            break;
                        case ObservableButtonExtensions.PressType.LongPress:
                            property.OnLongPressCard?.Invoke(property.CardInfo, this);
                            break;
                    }
                })
                .AddTo(_disposables);
        }
        
        _Render(property.CardInfo);
    }
    public void Render(ICardView.CardSimpleProperty property)
    {
        _disposables.Clear();
        _Render(property.CardInfo);
    }

    private void _Render(CardInfo cardInfo)
    {
        var cardLocalizeData = _localizeLibrary.Get(LocalizeTitleInfoType.Card, cardInfo.CardDataID);
        var templateValue = cardInfo.GetTemplateValues();

        _cardIdentity = cardInfo.Identity;
        _cost.text = cardInfo.Cost.ToString();
        _costColorSwitch.Value =
            cardInfo.Cost > cardInfo.OriginCost ? 1 :
            cardInfo.Cost < cardInfo.OriginCost ? 2 : 0;

        var originPowerString = templateValue[CardInfo.KEY_POWER];
        templateValue[CardInfo.KEY_POWER] = 
            cardInfo.Power > cardInfo.OriginPower ? $"<color=#{_powerColorMapping.GetHtmlColor(1)}>{originPowerString}</color>" :
            cardInfo.Power < cardInfo.OriginPower ? $"<color=#{_powerColorMapping.GetHtmlColor(2)}>{originPowerString}</color>" :
            originPowerString;

        _title.text = cardLocalizeData.Title;
        _info.text = cardLocalizeData.Info.ReplaceTemplateKeys(templateValue);
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
        _cardInfoSubscription?.Dispose();
        _button.interactable = false;
        _disposables.Clear();
        OnDeselect();
        HideHandCardFocusContent();
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
        if(!_offsets.ContainsKey(guid))
        {
            _offsets.Add(guid, new List<Vector3>());
        }
        _offsets[guid].Add(offset);
        _UpdateLocalPosition(duration);
    }
    public void RemoveLocationOffset(Guid guid, float duration)
    {
        if(_offsets.ContainsKey(guid))
        {
            _offsets.Remove(guid);
        }
        _UpdateLocalPosition(duration);
    }

    private void _UpdateLocalPosition(float duration = 0f)
    {
        var offset = Vector3.zero;
        foreach(var offsets in _offsets.Values)
        {
            foreach(var o in offsets)
            {
                offset += o;
            }
        }

        if(_currentMoveTween != null && _currentMoveTween.IsActive())
        {
            _currentMoveTween.Kill();
        }

        if(duration == 0f)
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
