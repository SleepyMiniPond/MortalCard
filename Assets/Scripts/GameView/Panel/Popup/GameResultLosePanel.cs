using UnityEngine;
using UnityEngine.UI;

public interface IGameResultLosePanel
{
    void Open();
    void Close();

    Button RetryButton { get; }
    Button RestartButton { get; }
    Button QuitButton { get; }
}

public class GameResultLosePanel : MonoBehaviour, IGameResultLosePanel
{
    [SerializeField] private GameObject _panel;

    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;

    public Button RetryButton => _retryButton;
    public Button RestartButton => _restartButton;
    public Button QuitButton => _quitButton;

    public void Open()
    {
        _panel.SetActive(true);
    }
    public void Close()
    {
        _panel.SetActive(false);
    }

}
