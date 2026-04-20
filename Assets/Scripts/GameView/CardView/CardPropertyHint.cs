using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class CardPropertyHint : MonoBehaviour
{
    public record ViewData(
        IReadOnlyCollection<InfoCellViewData> InfoDatas);
    public record InfoCellViewData(
        LocalizeTitleInfoType Type,
        string LocalizeId,
        IReadOnlyDictionary<string, string> TemplateValues);

    [SerializeField]
    private CardBuffInfoViewFactory _cardBuffInfoViewFactory;
    [SerializeField]
    private GameKeyWordInfoViewFactory _gameKeyWordInfoViewFactory;
    [SerializeField]
    private RectTransform _layoutParent;
    [SerializeField]
    private RectTransform _content;

    private LocalizeLibrary _localizeLibrary;
    private List<CardBuffInfoView> _propertyViews = new List<CardBuffInfoView>();

    public void Init(LocalizeLibrary localizeLibrary)
    {
        _localizeLibrary = localizeLibrary;
    }

    public void ShowHint(ViewData viewData, RectTransform referenceRect)
    {
        if(viewData.InfoDatas.Count == 0)
        {
            HideHint();
            return;
        }

        _content.gameObject.SetActive(true);
        foreach(var infoData in viewData.InfoDatas)
        {
            var cardBuffInfoView = _cardBuffInfoViewFactory.CreatePrefab();
            cardBuffInfoView.transform.SetParent(_layoutParent, false);
            _propertyViews.Add(cardBuffInfoView);

            cardBuffInfoView.SetInfo(infoData, _localizeLibrary);
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutParent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        
        var canvas = _content.GetComponentInParent<Canvas>();
        var rectOnCanvas = canvas.GetRectOnCanvas(referenceRect, _content.parent as RectTransform);

        var anchorY = _content.rect.height > rectOnCanvas.height ?
            rectOnCanvas.min.y + _content.rect.height / 2 : 
            rectOnCanvas.max.y - _content.rect.height / 2;
        _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, anchorY);
    }

    public void HideHint()
    {
        foreach (var propertyView in _propertyViews)
        {
            _cardBuffInfoViewFactory.RecyclePrefab(propertyView);
        }
        _propertyViews.Clear();
        _content.gameObject.SetActive(false);
    }
}
