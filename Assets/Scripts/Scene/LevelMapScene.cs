using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.Presenter;
using MortalGame.GameModel;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.Scene
{

    public class LevelMapScene : MonoBehaviour
    {
        [SerializeField] private CanvasScaler _canvasScaler;

        [SerializeField] private LevelMapView _levelMapView;

        public async UniTask<LevelMapCommand> Run(CancellationToken cancellationToken)
        {
            using var sceneCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                this.GetCancellationTokenOnDestroy());
            var presenter = new LevelMapPresenter(_levelMapView);

            var levelMapCommand = await presenter.Run(sceneCancellation.Token);
            return levelMapCommand;
        }
    }

}
