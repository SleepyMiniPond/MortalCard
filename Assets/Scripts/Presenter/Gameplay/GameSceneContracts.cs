using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;

namespace MortalGame.Presenter
{
    /// <summary>
    /// 只負責載入並取得場景入口，不執行場景互動或決定流程去向。
    /// </summary>
    public interface ISceneLoader
    {
        UniTask<IMenuScene> LoadMenuScene(CancellationToken cancellationToken);
        UniTask<ILevelMapScene> LoadLevelMapScene(CancellationToken cancellationToken);
        UniTask<IGameplayScene> LoadGameplayScene(CancellationToken cancellationToken);
    }

    public interface IMenuScene
    {
        UniTask Run(CancellationToken cancellationToken);
    }

    public interface ILevelMapScene
    {
        UniTask<LevelMapCommand> Run(CancellationToken cancellationToken);
    }

    public interface IGameplayScene
    {
        UniTask<GameplayResultCommand> Run(
            Context context, GameStageSetting setting, CancellationToken cancellationToken);
    }
}
