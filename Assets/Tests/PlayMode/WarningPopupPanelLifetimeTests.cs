#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using NUnit.Framework;
using UniRx;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class WarningPopupPanelLifetimeTests
    {
        [UnityTest]
        public IEnumerator DestroyOpenPanel_ReleasesStateAndCompletesEvents() => UniTask.ToCoroutine(async () =>
        {
            var scene = default(UnityEngine.SceneManagement.Scene);
            try
            {
                await EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Gameplay.unity",
                    new LoadSceneParameters(LoadSceneMode.Additive)).ToUniTask();
                scene = SceneManager.GetSceneByPath("Assets/Scenes/Gameplay.unity");
                var panel = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<WarningPopupPanel>(true)).Single();
                var state = new SubscriptionProbe();
                using var session = panel.Open(state);
                var ended = false;
                var count = 0;
                using var subscription = session.CloseRequested.Subscribe(_ => count++, () => ended = true);
                Assert.That(state.IsSubscribed, Is.True);
                await UniTask.NextFrame();
                UnityEngine.Object.Destroy(panel);
                await UniTask.NextFrame();
                Assert.That(state.IsSubscribed, Is.False);
                Assert.That(ended, Is.True);
                Assert.That(count, Is.Zero, "銷毀不應偽造關閉操作");
                Assert.DoesNotThrow(session.Dispose);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) await SceneManager.UnloadSceneAsync(scene).ToUniTask();
            }
        });

        private sealed class SubscriptionProbe : IReadOnlyReactiveProperty<WarningPopupDisplayState>
        {
            public WarningPopupDisplayState Value { get; } = new("銷毀測試");
            public bool HasValue => true;
            public bool IsSubscribed { get; private set; }

            public IDisposable Subscribe(IObserver<WarningPopupDisplayState> observer)
            {
                observer.OnNext(Value);
                IsSubscribed = true;
                return Disposable.Create(() => IsSubscribed = false);
            }
        }
    }
}
#endif
