using System;
using System.Threading;
using MortalGame.Presenter;
using MortalGame.GameModel;
using MortalGame.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

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

        var cancellationToken = this.GetCancellationTokenOnDestroy();
        try
        {
            await _Gameloop(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async UniTask _Gameloop(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var menuScene = await _sceneLoadManager.LoadMenuScene(cancellationToken);
            await menuScene.Run(cancellationToken);

            var restart = false;
            do
            {
                var levelMapScene = await _sceneLoadManager.LoadLevelMapScene(cancellationToken);
                var levelMapCommand = await levelMapScene.Run(cancellationToken);

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
                            var gameplayScene = await _sceneLoadManager.LoadGameplayScene(cancellationToken);
                            var gameplayResult = await gameplayScene.Run(_context, cancellationToken);

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
