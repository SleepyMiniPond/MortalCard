using System;
using MortalGame.Presentation.Abstractions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MortalGame.GameModel;
namespace MortalGame.GameView
{

    public interface ICardSelectionPanel
    {
        public record Property(
            bool IsConfirmable,
            IEnumerable<SelectionProperty> Selections,
            string Description);
        public record SelectionProperty(
            string Id,
            CardInfo CardInfo,
            Action<Guid, ICardView> OnClick,
            Action<Guid, ICardView> OnLongPress);
        public record UpdateProperty(
            bool IsVisible,
            bool IsConfirmable,
            IEnumerable<CardInfo> SelectedCards,
            string Description);

        Button[] CloseButtons { get; }
        Button ConfirmButton { get; }
        Button VisibleToggleButton { get; }

        void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
        void Open(Property property);
        void RenderUpdate(UpdateProperty property);
        void Close();
    }

    public class CardSelectionPanel : MonoBehaviour, ICardSelectionPanel
    {
        [SerializeField]
        private GameObject _panel;
        [SerializeField]
        private Transform _cardViewParent;
        [SerializeField]
        private CardViewFactory _cardViewFactory;


        [Header("Objects")]
        [SerializeField]
        private GameObject[] _visibleToggleObjects;
        [SerializeField]
        private GameObject _closeObject;

        [Header("Buttons")]
        [SerializeField]
        private Button[] _closeButtons;
        [SerializeField]
        private Button _visibleToggleButton;
        [SerializeField]
        private Button _confirmButton;

        [Header("Text")]
        [SerializeField]
        private TextMeshProUGUI _visibleButtonText;
        [SerializeField]
        private TextMeshProUGUI _confirmButtonText;
        [SerializeField]
        private TextMeshProUGUI _selectionDescriptionText;

        [Header("LocalizeKey")]
        [SerializeField]
        private string _visibleButtonLocalizeKey;
        [SerializeField]
        private string _confirmButtonLocalizeKey;

        public Button[] CloseButtons => _closeButtons;
        public Button ConfirmButton => _confirmButton;
        public Button VisibleToggleButton => _visibleToggleButton;

        private IGameViewModel _gameViewModel;
        private LocalizeLibrary _localizeLibrary;
        private Dictionary<Guid, CardView> _cardViewMap = new();

        public void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary)
        {
            _gameViewModel = gameInfoModel;
            _localizeLibrary = localizeLibrary;

            _visibleButtonText.text = _localizeLibrary.Get(LocalizeInfoType.UI, _visibleButtonLocalizeKey).Info;
            _confirmButtonText.text = _localizeLibrary.Get(LocalizeInfoType.UI, _confirmButtonLocalizeKey).Info;
        }

        public void Open(ICardSelectionPanel.Property property)
        {
            _Clear();

            _panel.SetActive(true);
            foreach (var obj in _visibleToggleObjects)
            {
                obj.SetActive(true);
            }

            _closeObject.SetActive(true);
            _visibleToggleButton.interactable = true;
            _confirmButton.interactable = property.IsConfirmable;

            _selectionDescriptionText.text = property.Description;

            foreach (var selection in property.Selections)
            {
                var cardView = _cardViewFactory.CreatePrefab();
                _cardViewMap.Add(selection.CardInfo.Identity, cardView);
                cardView.transform.SetParent(_cardViewParent, false);
                cardView.Initialize(_gameViewModel, _localizeLibrary);
                cardView.Render(
                    new ICardView.CardClickableProperty(
                        selection.CardInfo,
                        true,
                        selection.OnClick,
                        selection.OnLongPress));
            }
        }
        public void RenderUpdate(ICardSelectionPanel.UpdateProperty property)
        {
            foreach (var obj in _visibleToggleObjects)
            {
                obj.SetActive(property.IsVisible);
            }

            _confirmButton.interactable = property.IsConfirmable;

            _selectionDescriptionText.text = property.Description;

            var selectedCardIdentities = property.SelectedCards
                .Select(cardInfo => cardInfo.Identity)
                .ToHashSet();
            foreach (var kvp in _cardViewMap)
            {
                if (selectedCardIdentities.Contains(kvp.Key))
                {
                    kvp.Value.OnSelect();
                }
                else
                {
                    kvp.Value.OnDeselect();
                }
            }
        }

        public void Close()
        {
            _panel.SetActive(false);
            _Clear();
        }

        private void _Clear()
        {
            foreach (var cardView in _cardViewMap.Values)
            {
                _cardViewFactory.RecyclePrefab(cardView);
            }

            _cardViewMap.Clear();
        }
    }
}
