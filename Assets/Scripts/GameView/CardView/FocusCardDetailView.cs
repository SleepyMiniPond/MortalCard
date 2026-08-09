using Sirenix.OdinInspector;
using MortalGame.Presentation.Abstractions;
using MortalGame.GameData;
using UniRx;
using UnityEngine;
using MortalGame.UI;

namespace MortalGame.GameView
{

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
        private readonly SerialDisposable _cardInfoSubscription = new();

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
            _RenderHints(property);

            _cardInfoSubscription.Disposable = null;
            _gameViewModel.ObservableCardInfo(property.CardProperty.CardInfo.Identity)
                .MatchSome(infoProperty =>
                {
                    _cardInfoSubscription.Disposable = infoProperty
                        .Skip(1)
                        .Subscribe(cardInfo => _RenderHints(CardDetailProperty.Create(cardInfo)));
                });
        }

        public void HideFocus()
        {
            _cardInfoSubscription.Disposable = null;
            _cardBuffHint.HideHint();
            _cardKeywordHint.HideHint();

            _panel.SetActive(false);
        }

        private void _RenderHints(CardDetailProperty property)
        {
            _cardBuffHint.ShowHint(property.CardBuffHint, _cardView.RectTransform);
            _cardKeywordHint.ShowHint(property.CardKeywordHint, _cardView.RectTransform);
        }

        private void OnDestroy()
        {
            _cardInfoSubscription.Dispose();
        }
    }
}
