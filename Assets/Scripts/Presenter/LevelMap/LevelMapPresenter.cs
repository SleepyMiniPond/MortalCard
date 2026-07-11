using System.Threading;
using MortalGame.Presentation.Abstractions;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using UnityEngine;

namespace MortalGame.Presenter
{

    public interface ILevelMapPresenter
    {
        UniTask<LevelMapCommand> Run(CancellationToken cancellationToken);
    }

    public class LevelMapPresenter : ILevelMapPresenter
    {
        private readonly ILevelMapView _levelMapView;

        public LevelMapPresenter(ILevelMapView levelMapView)
        {
            _levelMapView = levelMapView;
        }

        public async UniTask<LevelMapCommand> Run(CancellationToken cancellationToken)
        {
            var levelStatus = LevelMapStatus.Walk;
            var reactionType = LevelMapReactionType.Restart;

            var disposable = _levelMapView.RegisterActions(
                onClickLevel: () =>
                {
                    levelStatus = LevelMapStatus.Battle;
                    reactionType = LevelMapReactionType.StartGamePlay;
                });

            try
            {
            while (!IsLevelMapQuit())
            {
                await UniTask.NextFrame(cancellationToken);
            }
            }
            finally
            {
                disposable.Dispose();
            }

            return new LevelMapCommand(reactionType);

            bool IsLevelMapQuit()
            {
                return levelStatus == LevelMapStatus.Leave || levelStatus == LevelMapStatus.Battle;
            }
        }

        private void _OnClickLevel()
        {
            // Handle level click logic here
        }
    }

}
