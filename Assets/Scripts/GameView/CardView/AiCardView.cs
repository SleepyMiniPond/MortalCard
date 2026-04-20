using System;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public interface IAiCardView : IRecyclable, ISelectableView
{
    void SetCardInfo(CardInfo cardInfo, LocalizeLibrary localizeLibrary);
    void SetPositionAndRotation(Vector3 position, Quaternion rotation);
}

public class AiCardView : MonoBehaviour, IAiCardView
{
    [SerializeField]
    private RectTransform _rectTransform;
    [SerializeField]
    private TextMeshProUGUI _title;
    [SerializeField]
    private TextMeshProUGUI _info;
    [SerializeField]
    private TextMeshProUGUI _cost;
    [SerializeField]
    private TextMeshProUGUI _power;


    private CompositeDisposable _disposables = new CompositeDisposable();

    public RectTransform RectTransform => _rectTransform;
    public TargetType TargetType => TargetType.EnemyCard;
    public Guid TargetIdentity => _cardIdentity;

    private Guid _cardIdentity;

    public void SetCardInfo(CardInfo cardInfo, LocalizeLibrary localizeLibrary)
    {
        var cardLocalizeData = localizeLibrary.Get(LocalizeTitleInfoType.Card, cardInfo.CardDataID);
        var templateValue = cardInfo.GetTemplateValues();

        _cardIdentity = cardInfo.Identity;
        _title.text = cardLocalizeData.Title;
        _info.text = cardLocalizeData.Info.ReplaceTemplateKeys(templateValue);
        _cost.text = cardInfo.Cost.ToString();
        _power.text = cardInfo.Power.ToString();
    }
    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        transform.SetLocalPositionAndRotation(position, rotation);
    }

    public void Reset()
    {
        _disposables.Clear();
        _cardIdentity = Guid.Empty;
    }

    public void OnSelect()
    {
    }
    public void OnDeselect()
    {
    }
}
