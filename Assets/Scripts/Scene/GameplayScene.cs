using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.Presenter;
using MortalGame.GameModel;
using UnityEngine;
using MortalGame.GameView;

namespace MortalGame.Scene
{

    public class GameplayScene : MonoBehaviour, IGameplayScene
    {
        [SerializeField]
        private GameplayView _gameplayView;
        [SerializeField]
        private GameResultWinPanel _gameResultWinPanel;
        [SerializeField]
        private GameResultLosePanel _gameResultLosePanel;

        public async UniTask<GameplayResultCommand> Run(
            Context context,
            GameStageSetting gameStageSetting,
            CancellationToken cancellationToken)
        {
            using var sceneCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                this.GetCancellationTokenOnDestroy());
            // 臨時兩關標示的唯一入口，正式地圖導入後與 TemporaryBattleRoute 一起移除。
            _gameplayView.ShowTemporaryStageLabel(TemporaryBattleRoute.GetStageNumber(gameStageSetting.StageID), TemporaryBattleRoute.StageCount);
            var battleBuilder = new BattleBuidler(context);

            var gameContextManager = battleBuilder.ConstructGameContextManager(gameStageSetting.RandomSeed);
            var gameplayPresenter = new GameplayPresenter(
                _gameplayView,
                _gameResultWinPanel,
                _gameResultLosePanel,
                gameStageSetting,
                gameContextManager);

            var result = await gameplayPresenter.Run(sceneCancellation.Token);

            return result;
        }
    }


}
