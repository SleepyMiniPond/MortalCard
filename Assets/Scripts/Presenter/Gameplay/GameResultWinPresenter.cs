using System.Threading;
using MortalGame.Presentation.Abstractions;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameView;
using UnityEngine;

namespace MortalGame.Presenter
{

    public interface IGameResultWinPresenter
    {
        UniTask<GameplayWinResult> Run(CancellationToken cancellationToken);
    }

    public class GameResultWinPresenter : IGameResultWinPresenter
    {
        private readonly IGameResultWinPanel _winPanel;

        public GameResultWinPresenter(IGameResultWinPanel winPanel)
        {
            _winPanel = winPanel;
        }

        public async UniTask<GameplayWinResult> Run(CancellationToken cancellationToken)
        {
            var isClose = false;
            _winPanel.Open();
            try
            {
                while (!isClose)
                {
                    await UniTask.NextFrame(cancellationToken);
                }
            }
            finally
            {
                _winPanel.Close();
            }

            return new GameplayWinResult();
        }
    }
}
