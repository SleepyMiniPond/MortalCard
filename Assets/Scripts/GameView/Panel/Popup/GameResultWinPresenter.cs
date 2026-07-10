using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortalGame.GameView
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
