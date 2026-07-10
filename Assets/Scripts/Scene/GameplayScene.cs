using Cysharp.Threading.Tasks;
using MortalGame.Presenter;
using MortalGame.GameModel;
using UnityEngine;
using MortalGame.GameView;

namespace MortalGame.Scene
{

public class GameplayScene : MonoBehaviour
{
    [SerializeField]
    private GameplayView _gameplayView;
    [SerializeField]
    private GameResultWinPanel _gameResultWinPanel;
    [SerializeField]
    private GameResultLosePanel _gameResultLosePanel;

    public async UniTask<GameplayResultCommand> Run(Context context)
    {
        var battleBuilder = new BattleBuidler(context);

        var gameStageSetting = battleBuilder.ConstructBattle();
        var gameContextManager = battleBuilder.ConstructGameContextManager(gameStageSetting.RandomSeed);
        var gameplayPresenter = new GameplayPresenter(
            _gameplayView,
            _gameResultWinPanel,
            _gameResultLosePanel,
            gameStageSetting,
            gameContextManager);

        var result = await gameplayPresenter.Run();

        return result; 
    }
}


}
