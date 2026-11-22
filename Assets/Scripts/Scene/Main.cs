using System;
using MortalGame.Presenter;
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
            await new GameFlow(_context, _sceneLoadManager).Run(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
