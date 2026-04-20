using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Rayark.Mast;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    [SerializeField]
    private ScriptableDataLoader _scriptableDataLoader;

    private SceneLoadManager _sceneLoadManager;
    private Context _context;

    async UniTaskVoid Start()
    {
        DontDestroyOnLoad(this);

        Application.targetFrameRate = 60;

        _sceneLoadManager = new SceneLoadManager();
        _context = new Context(
            _scriptableDataLoader);

        await _Gameloop();
    }

    private async UniTask _Gameloop()
    {
        while (true)
        {
            var menuScene = await _sceneLoadManager.LoadMenuScene();
            await menuScene.Run();

            var restart = false;
            do
            {
                var levelMapScene = await _sceneLoadManager.LoadLevelMapScene();
                var levelMapCommand = await levelMapScene.Run();

                switch (levelMapCommand.ReactionType)
                {
                    case LevelMapReactionType.Fail:
                        return;
                    case LevelMapReactionType.Finish:
                        return;
                    case LevelMapReactionType.Restart:
                        restart = true;
                        break;

                    case LevelMapReactionType.StartGamePlay:
                        
                        var retry = false;
                        do
                        {
                            var gameplayScene = await _sceneLoadManager.LoadGameplayScene();
                            var gameplayResult = await gameplayScene.Run(_context);

                            if (gameplayResult.Result is GameplayLoseResult loseResult)
                            {
                                if (loseResult.ReactionType == LoseReactionType.Retry)
                                {
                                    retry = true;
                                }
                                else if (loseResult.ReactionType == LoseReactionType.Restart)
                                {
                                    restart = true;
                                    break;
                                }
                            }
                        }
                        while (retry);
                
                        break;
                }
            }
            while (restart);
        }
    }
}