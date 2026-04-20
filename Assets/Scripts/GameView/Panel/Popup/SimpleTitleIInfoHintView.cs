using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleTitleInfoHintView : MonoBehaviour
{    
    [SerializeField]
    private TextMeshProUGUI _titleText;

    [SerializeField]
    private TextMeshProUGUI _infoText;

    [SerializeField]
    private RectTransform _hintRectTransform;

    private LocalizeLibrary _localizeLibrary;

    public void Init(LocalizeLibrary localizeLibrary)
    {
        _localizeLibrary = localizeLibrary;
    }

    public void ShowTitleInfo(string title, string info, RectTransform targetRect)
    {
        gameObject.SetActive(true);

        _titleText.text = title;
        _infoText.text = info;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_hintRectTransform);

        _SetAnchorPosition(targetRect);
    }

    private void _SetAnchorPosition(RectTransform targetRect)
    {
        var canvas = _hintRectTransform.GetComponentInParent<Canvas>();
        var rectOnCanvas = canvas.GetRectOnCanvas(targetRect, _hintRectTransform.parent as RectTransform);

        if(rectOnCanvas.center.x > 0)
        {
            _hintRectTransform.anchoredPosition = new Vector2(
                rectOnCanvas.min.x - _hintRectTransform.rect.width / 2,
                rectOnCanvas.max.y - _hintRectTransform.rect.height / 2);
        }
        else
        {
            _hintRectTransform.anchoredPosition = new Vector2(
                rectOnCanvas.max.x + _hintRectTransform.rect.width / 2,
                rectOnCanvas.max.y - _hintRectTransform.rect.height / 2);
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
        _titleText.text = string.Empty;
        _infoText.text = string.Empty;
    }
}