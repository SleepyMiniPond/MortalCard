using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MortalGame.Scene
{

    public class SceneLoadManager
    {
        public const string MenuSceneName = "Menu";
        public const string LevelMapSceneName = "LevelMap";
        public const string GameplaySceneName = "Gameplay";
        public const string LoadingSceneName = "Loading";

        public async UniTask<MenuScene> LoadMenuScene(CancellationToken cancellationToken)
        {
            await SceneManager.LoadSceneAsync(MenuSceneName).ToUniTask(cancellationToken: cancellationToken);

            var menuScene = Object.FindFirstObjectByType<MenuScene>();

            return menuScene;
        }

        public async UniTask<LevelMapScene> LoadLevelMapScene(CancellationToken cancellationToken)
        {
            await SceneManager.LoadSceneAsync(LevelMapSceneName).ToUniTask(cancellationToken: cancellationToken);

            var levelMapScene = Object.FindFirstObjectByType<LevelMapScene>();

            return levelMapScene;
        }

        public async UniTask<GameplayScene> LoadGameplayScene(CancellationToken cancellationToken)
        {
            await SceneManager.LoadSceneAsync(GameplaySceneName).ToUniTask(cancellationToken: cancellationToken);

            var gameplayScene = Object.FindFirstObjectByType<GameplayScene>();

            return gameplayScene;
        }

        public async UniTask<LoadingScene> LoadLoadingScene(CancellationToken cancellationToken)
        {
            await SceneManager.LoadSceneAsync(LoadingSceneName).ToUniTask(cancellationToken: cancellationToken);

            var loadingScene = Object.FindFirstObjectByType<LoadingScene>();

            return loadingScene;
        }
    }

}
