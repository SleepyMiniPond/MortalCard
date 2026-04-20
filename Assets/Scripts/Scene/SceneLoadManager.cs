using System.Collections;
using Cysharp.Threading.Tasks;
using Rayark.Mast;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager
{
    public const string MenuSceneName = "Menu";
    public const string LevelMapSceneName = "LevelMap";
    public const string GameplaySceneName = "Gameplay";
    public const string LoadingSceneName = "Loading";

    public async UniTask<MenuScene> LoadMenuScene()
    {
        await SceneManager.LoadSceneAsync(MenuSceneName);

        var menuScene = Object.FindFirstObjectByType<MenuScene>();

        return menuScene;
    }

    public async UniTask<LevelMapScene> LoadLevelMapScene()
    {
        await SceneManager.LoadSceneAsync(LevelMapSceneName);

        var levelMapScene = Object.FindFirstObjectByType<LevelMapScene>();

        return levelMapScene;
    }

    public async UniTask<GameplayScene> LoadGameplayScene()
    {
        await SceneManager.LoadSceneAsync(GameplaySceneName);

        var gameplayScene = Object.FindFirstObjectByType<GameplayScene>();

        return gameplayScene;
    }

    public async UniTask<LoadingScene> LoadLoadingScene()
    {
        await SceneManager.LoadSceneAsync(LoadingSceneName);

        var loadingScene = Object.FindFirstObjectByType<LoadingScene>();

        return loadingScene;
    }
}
