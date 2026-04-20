using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ILevelMapPresenter
{
    UniTask<LevelMapCommand> Run();
}

public class LevelMapPresenter : ILevelMapPresenter
{
    private readonly ILevelMapView _levelMapView;

    public LevelMapPresenter(ILevelMapView levelMapView)
    {
        _levelMapView = levelMapView;
    }

    public async UniTask<LevelMapCommand> Run()
    {
        var levelStatus = LevelMapStatus.Walk;
        var reactionType = LevelMapReactionType.Restart;

        var disposable = _levelMapView.RegisterActions(
            onClickLevel: () => {
                levelStatus = LevelMapStatus.Battle;
                reactionType = LevelMapReactionType.StartGamePlay;
            });

        while (!IsLevelMapQuit())
        {
            await UniTask.NextFrame();
        }

        disposable.Dispose();
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
