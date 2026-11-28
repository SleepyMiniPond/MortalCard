using System;
using MortalGame.Exceptions;
using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class GameResultLosePresenterTests
    {
        private UnityEngine.SceneManagement.Scene _scene;

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
        }

        [UnityTest]
        public IEnumerator Buttons_ReturnTheirOwnResultAcrossRepeatedOpenings() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            foreach (var reaction in new[] { LoseReactionType.Retry, LoseReactionType.Restart, LoseReactionType.Quit })
            {
                var run = s.Presenter.Run(lifetime.Token).Preserve();
                try
                {
                    Assert.That(s.Popup.activeSelf, Is.True);
                    Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Pending));
                    s.Buttons[reaction].onClick.Invoke();
                    await _Wait(() => run.Status != UniTaskStatus.Pending);
                    Assert.That((await run).ReactionType, Is.EqualTo(reaction));
                    Assert.That(s.Popup.activeSelf, Is.False);
                    s.Buttons[reaction].onClick.Invoke();
                }
                finally
                {
                    if (run.Status == UniTaskStatus.Pending) lifetime.Cancel();
                    await _Stop(run);
                }
            }
        });

        [UnityTest]
        public IEnumerator FirstAcceptedClick_CannotBeOverwrittenByAnotherButton() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            foreach (var reaction in new[] { LoseReactionType.Retry, LoseReactionType.Quit })
            {
                var run = s.Presenter.Run(lifetime.Token).Preserve();
                try
                {
                    s.Buttons[reaction].onClick.Invoke();
                    s.Buttons[LoseReactionType.Restart].onClick.Invoke();
                    await _Wait(() => run.Status != UniTaskStatus.Pending);
                    Assert.That((await run).ReactionType, Is.EqualTo(reaction));
                }
                finally
                {
                    if (run.Status == UniTaskStatus.Pending) lifetime.Cancel();
                    await _Stop(run);
                }
            }
        });

        [UnityTest]
        public IEnumerator Cancellation_ClosesWithoutDefaultQuitResult() => UniTask.ToCoroutine(async () =>
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
        public IEnumerator CancellationAfterClick_DoesNotReturnSelectedResult() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(lifetime.Token).Preserve();
            s.Buttons[LoseReactionType.Retry].onClick.Invoke();
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
        });

        [Test]
        public void Session_MapsAllButtonsAndOldSessionCannotReceiveNewInput()
        {
            var s = _OpenScene();
            using var first = s.Panel.Open();
            var received = new List<LoseReactionType>();
            var completed = false;
            using var subscription = first.ReactionRequested.Subscribe(received.Add, () => completed = true);
            foreach (var reaction in new[] { LoseReactionType.Retry, LoseReactionType.Restart, LoseReactionType.Quit })
                s.Buttons[reaction].onClick.Invoke();
            Assert.That(received, Is.EqualTo(new[] { LoseReactionType.Retry, LoseReactionType.Restart, LoseReactionType.Quit }));
            var error = Assert.Throws<SessionAlreadyOpenException>(() => s.Panel.Open());
            Assert.That(error.OwnerType, Is.EqualTo(typeof(GameResultLosePanel)));
            first.Dispose();
            Assert.That(completed, Is.True);
            Assert.That(received.Count, Is.EqualTo(3), "Dispose 不應產生額外操作");
            using var second = s.Panel.Open();
            var newResults = new List<LoseReactionType>();
            using var nextSubscription = second.ReactionRequested.Subscribe(newResults.Add);
            first.Dispose();
            s.Buttons[LoseReactionType.Retry].onClick.Invoke();
            Assert.That(received.Count, Is.EqualTo(3));
            Assert.That(newResults, Is.EqualTo(new[] { LoseReactionType.Retry }));
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
                .SelectMany(go => go.GetComponentsInChildren<GameResultLosePanel>(true)).Single();
            var serialized = new SerializedObject(panel);
            var popup = (GameObject)serialized.FindProperty("_panel").objectReferenceValue;
            var buttons = new Dictionary<LoseReactionType, Button>
            {
                [LoseReactionType.Retry] = (Button)serialized.FindProperty("_retryButton").objectReferenceValue,
                [LoseReactionType.Restart] = (Button)serialized.FindProperty("_restartButton").objectReferenceValue,
                [LoseReactionType.Quit] = (Button)serialized.FindProperty("_quitButton").objectReferenceValue
            };
            Assert.That(popup.activeSelf, Is.False);
            Assert.That(buttons.Values.All(button => button != null), Is.True, "既有失敗按鈕必須完整接線");
            return new Setup(panel, popup, buttons, new GameResultLosePresenter(panel));
        }

        private static async UniTask _Wait(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await UniTask.Yield();
            }
            Assert.That(condition(), Is.True, "失敗操作應在期限內完成");
        }
        private static async UniTask _Stop(UniTask<GameplayLoseResult> run)
        {
            await _Wait(() => run.Status != UniTaskStatus.Pending);
            await run.SuppressCancellationThrow();
        }
        private record Setup(GameResultLosePanel Panel, GameObject Popup,
            Dictionary<LoseReactionType, Button> Buttons, GameResultLosePresenter Presenter);
    }
}
