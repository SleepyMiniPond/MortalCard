using MortalGame.Presentation.Abstractions;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameView;
using UnityEngine;

namespace MortalGame.Presenter
{

    public interface IGameResultWinPresenter
    {
        UniTask<GameplayWinResult> Run();
    }

    public class GameResultWinPresenter : IGameResultWinPresenter
    {
        private readonly IGameResultWinPanel _winPanel;

        public GameResultWinPresenter(IGameResultWinPanel winPanel)
        {
            _winPanel = winPanel;
        }

        public async UniTask<GameplayWinResult> Run()
        {
            var isClose = false;
            while (!isClose)
            {
                await UniTask.NextFrame();
            }

            return new GameplayWinResult();
        }
    }
}
