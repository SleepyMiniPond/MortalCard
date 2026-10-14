using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.GameView
{
    public interface IWarningPopupPanel
    {
        Button[] CloseButtons { get; }
        void Open(string message);
        void Close();
    }

    public class WarningPopupPanel : MonoBehaviour, IWarningPopupPanel
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button[] _closeButtons;

        public Button[] CloseButtons => _closeButtons;

        public void Open(string message)
        {
            _messageText.text = message;
            _panel.SetActive(true);
        }

        public void Close()
        {
            _panel.SetActive(false);
            _messageText.text = string.Empty;
        }
    }
}
