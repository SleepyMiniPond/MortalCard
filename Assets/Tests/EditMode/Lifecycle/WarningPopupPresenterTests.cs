using System;
using MortalGame.Exceptions;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using MortalGame.Presenter;
using NUnit.Framework;
using TMPro;
using UniRx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class WarningPopupPresenterTests
    {
        private UnityEngine.SceneManagement.Scene _scene;

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
        }

        [UnityTest]
        public IEnumerator Run_DisplaysMessageAndEachCloseButtonAllowsReopen() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            foreach (var button in s.Buttons)
            {
                using var lifetime = new CancellationTokenSource();
                var run = s.Presenter.Run("測試警告文字", lifetime.Token).Preserve();
                try
                {
                    Assert.That(s.Text.text, Is.EqualTo("測試警告文字"));
                    Assert.That(s.Popup.activeSelf, Is.True);
                    Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Pending));
                    button.onClick.Invoke();
                    button.onClick.Invoke();
                    await _Wait(() => run.Status != UniTaskStatus.Pending);
                    await run;
                    Assert.That(s.Popup.activeSelf, Is.False);
                    Assert.That(s.Text.text, Is.Empty);
                    button.onClick.Invoke();
                }
                finally { lifetime.Cancel(); await run.SuppressCancellationThrow(); }
            }
        });

        [UnityTest]
        public IEnumerator Cancellation_ClosesAndAllowsAnotherMessage() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run("取消前的提示", lifetime.Token).Preserve();
            lifetime.Cancel();
            await _Wait(() => run.Status != UniTaskStatus.Pending);
            Assert.That(await run.SuppressCancellationThrow(), Is.True);
            Assert.That(s.Popup.activeSelf, Is.False);
            Assert.That(s.Text.text, Is.Empty);
            using var nextLifetime = new CancellationTokenSource();
            var next = s.Presenter.Run("新的提示", nextLifetime.Token).Preserve();
            try
            {
                Assert.That(s.Text.text, Is.EqualTo("新的提示"));
                Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Pending));
                s.Buttons.Last().onClick.Invoke();
                await _Wait(() => next.Status != UniTaskStatus.Pending);
                await next;
            }
            finally { nextLifetime.Cancel(); await next.SuppressCancellationThrow(); }
        });

        [UnityTest]
        public IEnumerator AlreadyCancelled_DoesNotOpen() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            using var lifetime = new CancellationTokenSource();
            lifetime.Cancel();
            Assert.That(await s.Presenter.Run("不應顯示", lifetime.Token).SuppressCancellationThrow(), Is.True);
            Assert.That(s.Popup.activeSelf, Is.False);
        });

        [Test]
        public void StateAndSession_UpdateUntilDisposedAndIsolateReopenedWindow()
        {
            var s = _OpenScene();
            using var state = new ReactiveProperty<WarningPopupDisplayState>(new("初始訊息"));
            using var first = s.Panel.Open(state);
            var count = 0;
            var ended = false;
            using var subscription = first.CloseRequested.Subscribe(_ => count++, () => ended = true);
            Assert.That(s.Text.text, Is.EqualTo("初始訊息"));
            state.Value = new("更新訊息");
            Assert.That(s.Text.text, Is.EqualTo("更新訊息"));
            foreach (var button in s.Buttons) button.onClick.Invoke();
            Assert.That(count, Is.EqualTo(s.Buttons.Length));
            var error = Assert.Throws<SessionAlreadyOpenException>(() => s.Panel.Open(state));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(WarningPopupPanel)));
            Assert.That(error.Message, Does.Contain(typeof(WarningPopupPanel).FullName));
            first.Dispose();
            Assert.That(ended, Is.True);
            Assert.That(count, Is.EqualTo(s.Buttons.Length), "釋放不應偽造關閉操作");
            state.Value = new("關閉後的舊訊息");
            Assert.That(s.Text.text, Is.Empty);

            using var nextState = new ReactiveProperty<WarningPopupDisplayState>(new("新視窗"));
            using var second = s.Panel.Open(nextState);
            var nextCount = 0;
            using var nextSubscription = second.CloseRequested.Subscribe(_ => nextCount++);
            first.Dispose();
            state.Value = new("不能覆寫新視窗");
            Assert.That(s.Text.text, Is.EqualTo("新視窗"));
            s.Buttons[0].onClick.Invoke();
            Assert.That(nextCount, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(s.Buttons.Length));
            second.Dispose();
            second.Dispose();
            nextState.Value = new("釋放後不應顯示");
            Assert.That(s.Text.text, Is.Empty);
            Assert.That(s.Popup.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator OpenFailure_ReleasesBindingAndAllowsReopen() => UniTask.ToCoroutine(async () =>
        {
            var s = _OpenScene();
            var serialized = new SerializedObject(s.Panel);
            serialized.FindProperty("_panel").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var failed = false;
            try { await s.Presenter.Run("測試失敗", CancellationToken.None); }
            catch (UnassignedReferenceException) { failed = true; }
            Assert.That(failed, Is.True);
            serialized.FindProperty("_panel").objectReferenceValue = s.Popup;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 視窗已顯示後讓綁定失敗，驗證部分建立的資源也會回收。
            using var state = new ReactiveProperty<WarningPopupDisplayState>(null);
            Assert.Throws<NullReferenceException>(() => s.Panel.Open(state));
            Assert.That(s.Popup.activeSelf, Is.False);
            state.Value = new("修復後的訊息");
            using var reopened = s.Panel.Open(state);
            Assert.That(s.Text.text, Is.EqualTo("修復後的訊息"));
        });

        private Setup _OpenScene()
        {
            _scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Additive);
            var panel = _scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<WarningPopupPanel>(true)).Single();
            var serialized = new SerializedObject(panel);
            var popup = (GameObject)serialized.FindProperty("_panel").objectReferenceValue;
            var text = (TMP_Text)serialized.FindProperty("_messageText").objectReferenceValue;
            var buttons = serialized.FindProperty("_closeButtons");
            var closeButtons = Enumerable.Range(0, buttons.arraySize)
                .Select(i => (Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Assert.That(popup.activeSelf, Is.False);
            Assert.That(closeButtons.Length, Is.GreaterThan(0));
            Assert.That(closeButtons.All(button => button != null), Is.True);
            return new Setup(panel, popup, text, closeButtons, new WarningPopupPresenter(panel));
        }

        private static async UniTask _Wait(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await UniTask.Yield();
            }
            Assert.That(condition(), Is.True, "警告流程應在期限內完成");
        }

        private record Setup(WarningPopupPanel Panel, GameObject Popup, TMP_Text Text,
            Button[] Buttons, WarningPopupPresenter Presenter);
    }
}
