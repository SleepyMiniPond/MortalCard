using Sirenix.OdinInspector;
using UnityEngine;

public class FocusCardDetailView : MonoBehaviour
{
    [SerializeField]
    private GameObject _panel;
    [SerializeField]
    private RectTransform _content;
    [SerializeField]
    private CardView _cardView;
    [SerializeField]
    private CardPropertyHint _cardBuffHint;
    [SerializeField]
    private CardPropertyHint _cardKeywordHint;

    private IGameViewModel _gameViewModel;
    private LocalizeLibrary _localizeLibrary;

    public void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary)
    {
        _gameViewModel = gameInfoModel;
        _localizeLibrary = localizeLibrary;
        _cardBuffHint.Init(localizeLibrary);
        _cardKeywordHint.Init(localizeLibrary);
        _cardView.Initialize(gameInfoModel, localizeLibrary);
    }

    public void ShowFocus(CardDetailProperty property, RectTransform targetRect)
    {
        _panel.SetActive(true);

        var canvas = _content.GetComponentInParent<Canvas>();
        var rectOnCanvas = canvas.GetRectOnCanvas(targetRect, _content.parent as RectTransform);

        _content.anchoredPosition = new Vector2(rectOnCanvas.center.x, _content.anchoredPosition.y);

        _cardView.Render(property.CardProperty);
        _cardBuffHint.ShowHint(property.CardBuffHint, _cardView.RectTransform);
        _cardKeywordHint.ShowHint(property.CardKeywordHint, _cardView.RectTransform);
    }

    public void HideFocus()
    {
        _cardBuffHint.HideHint();
        _cardKeywordHint.HideHint();

        _panel.SetActive(false);
    }
}
