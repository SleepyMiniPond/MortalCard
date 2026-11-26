using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presenter;
using NUnit.Framework;
using UniRx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class GameResultWinPresenterTests
    {
        private UnityEngine.SceneManagement.Scene _scene;

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
        }

        [UnityTest]
        public IEnumerator Continue_CompletesOnceAndAllowsAnotherWin() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(lifetime.Token).Preserve();
            try
            {
                Assert.That(s.Popup.activeSelf, Is.True);
                Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Pending));
                s.Button.onClick.Invoke();
                s.Button.onClick.Invoke();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                Assert.That(await run, Is.TypeOf<GameplayWinResult>());
                Assert.That(s.Popup.activeSelf, Is.False);
                s.Button.onClick.Invoke();

                run = s.Presenter.Run(lifetime.Token).Preserve();
                Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Pending), "舊輸入不得直接完成下次開窗");
                s.Button.onClick.Invoke();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                Assert.That(await run, Is.TypeOf<GameplayWinResult>());
                Assert.That(s.Popup.activeSelf, Is.False);
            }
            finally { lifetime.Cancel(); await _Stop(run); }
        });

        [UnityTest]
        public IEnumerator Cancellation_ClosesWithoutReturningWin() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(lifetime.Token).Preserve();
            lifetime.Cancel();
            await _Wait(() => run.Status != UniTaskStatus.Pending);
            var canceled = await run.SuppressCancellationThrow();
            Assert.That(canceled.IsCanceled, Is.True);
            Assert.That(canceled.Result, Is.Null);
            Assert.That(s.Popup.activeSelf, Is.False);
            using var reopened = s.Panel.Open();
            Assert.That(s.Popup.activeSelf, Is.True);
        });

        [UnityTest]
        public IEnumerator CancellationAfterClick_BeforeCompletionDoesNotReturnWin() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(lifetime.Token).Preserve();
            s.Button.onClick.Invoke();
            lifetime.Cancel();
            await _Wait(() => run.Status != UniTaskStatus.Pending);
            var canceled = await run.SuppressCancellationThrow();
            Assert.That(canceled.IsCanceled, Is.True);
            Assert.That(canceled.Result, Is.Null);
            Assert.That(s.Popup.activeSelf, Is.False);
        });

        [UnityTest]
        public IEnumerator AlreadyCancelled_DoesNotOpenPanel() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            lifetime.Cancel();
            var canceled = await s.Presenter.Run(lifetime.Token).SuppressCancellationThrow();
            Assert.That(canceled.IsCanceled, Is.True);
            Assert.That(canceled.Result, Is.Null);
            Assert.That(s.Popup.activeSelf, Is.False);
            using var session = s.Panel.Open();
        });

        [Test]
        public void SceneBindingAndSession_DisposeEndsEventsAndDoesNotAffectNextWindow()
        {
            var s = _OpenScene();
            Assert.That(s.Button.transform.IsChildOf(s.Popup.transform), Is.True);
            Assert.That(s.Button.GetComponentInChildren<TMPro.TMP_Text>(true).text, Is.EqualTo("繼續"));
            using var first = s.Panel.Open();
            var count = 0;
            var completed = false;
            using var subscription = first.ContinueRequested.Subscribe(_ => count++, () => completed = true);
            Assert.Throws<InvalidOperationException>(() => s.Panel.Open());
            first.Dispose();
            Assert.That(completed, Is.True);
            Assert.That(count, Is.Zero, "釋放不應偽造繼續操作");
            using var second = s.Panel.Open();
            var nextCount = 0;
            using var nextSubscription = second.ContinueRequested.Subscribe(_ => nextCount++);
            first.Dispose();
            s.Button.onClick.Invoke();
            Assert.That(count, Is.Zero);
            Assert.That(nextCount, Is.EqualTo(1));
            Assert.That(s.Popup.activeSelf, Is.True);
            second.Dispose();
            second.Dispose();
            Assert.That(s.Popup.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator OpenFailure_PropagatesAndAllowsReopen() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            var serialized = new SerializedObject(s.Panel);
            serialized.FindProperty("_panel").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var failed = false;
            try { await s.Presenter.Run(CancellationToken.None); }
            catch (UnassignedReferenceException) { failed = true; }
            Assert.That(failed, Is.True);
            serialized.FindProperty("_panel").objectReferenceValue = s.Popup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            using var reopened = s.Panel.Open();
            Assert.That(s.Popup.activeSelf, Is.True);
        });

        private Setup _OpenScene()
        {
            _scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Additive);
            var panel = _scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<GameResultWinPanel>(true)).Single();
            var serialized = new SerializedObject(panel);
            var popup = (GameObject)serialized.FindProperty("_panel").objectReferenceValue;
            var button = (Button)serialized.FindProperty("_continueButton").objectReferenceValue;
            Assert.That(popup.activeSelf, Is.False, "正式場景的勝利面板預設應隱藏");
            Assert.That(button, Is.Not.Null, "正式場景必須接好繼續按鈕");
            return new Setup(panel, popup, button, new GameResultWinPresenter(panel));
        }

        private static async UniTask _Wait(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await UniTask.Yield();
            }
            Assert.That(condition(), Is.True, "勝利流程應在期限內完成");
        }
        private static async UniTask _Stop(UniTask<GameplayWinResult> run)
        {
            await _Wait(() => run.Status != UniTaskStatus.Pending);
            await run.SuppressCancellationThrow();
        }

        private record Setup(GameResultWinPanel Panel, GameObject Popup, Button Button, GameResultWinPresenter Presenter);
    }
}
