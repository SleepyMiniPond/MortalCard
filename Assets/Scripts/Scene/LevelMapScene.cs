using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LevelMapScene : MonoBehaviour
{
    [SerializeField] private CanvasScaler _canvasScaler;

    [SerializeField] private LevelMapView _levelMapView;

    public async UniTask<LevelMapCommand> Run()
    {
        var presenter = new LevelMapPresenter(_levelMapView);

        var levelMapCommand = await presenter.Run();
        return levelMapCommand;
    }
}
