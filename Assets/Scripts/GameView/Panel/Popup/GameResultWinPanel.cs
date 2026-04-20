using UnityEngine;

public interface IGameResultWinPanel
{
}

public class GameResultWinPanel : MonoBehaviour, IGameResultWinPanel
{
    [SerializeField] private GameObject _panel;

    public void Open()
    {
        _panel.SetActive(true);
    }
    public void Close()
    {
        _panel.SetActive(false);
    }
}
