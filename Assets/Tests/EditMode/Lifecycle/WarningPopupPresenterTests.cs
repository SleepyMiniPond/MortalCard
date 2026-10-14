using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameView;
using MortalGame.Presenter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class WarningPopupPresenterTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects) UnityEngine.Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator Run_DisplaysProvidedMessageAndClosesOnCloseEvent() => UniTask.ToCoroutine(async () =>
        {
            var panel = _CreatePanel();
            var presenter = new WarningPopupPresenter(panel);
            panel.OnOpen = message =>
            {
                Assert.That(message, Is.EqualTo("測試警告文字"));
                Assert.That(panel.CloseCount, Is.Zero);
            };
            var run = presenter.Run("測試警告文字", CancellationToken.None);
            panel.CloseButtons[1].onClick.Invoke();
            await _Finish(run);
            Assert.That(panel.CloseCount, Is.EqualTo(1));
            panel.CloseButtons[0].onClick.Invoke();
            Assert.That(panel.CloseCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator Cancellation_ClosesWarningAndAllowsAnotherMessage() => UniTask.ToCoroutine(async () =>
        {
            var panel = _CreatePanel();
            var presenter = new WarningPopupPresenter(panel);
            using var cancellation = new CancellationTokenSource();
            var run = presenter.Run("取消前的提示", cancellation.Token);
            cancellation.Cancel();
            Assert.That(await run.SuppressCancellationThrow(), Is.True);
            Assert.That(panel.CloseCount, Is.EqualTo(1));
            panel.CloseButtons[0].onClick.Invoke();
            panel.OnOpen = message =>
            {
                Assert.That(panel.CloseCount, Is.EqualTo(1));
                Assert.That(message, Is.EqualTo("新的提示"));
            };
            var nextRun = presenter.Run("新的提示", CancellationToken.None);
            panel.CloseButtons[0].onClick.Invoke();
            await _Finish(nextRun);
            Assert.That(panel.CloseCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator OpenFailure_ClosesWarningAndReleasesOldCallbacks() => UniTask.ToCoroutine(async () =>
        {
            var panel = _CreatePanel();
            panel.OnOpen = _ => throw new InvalidOperationException("測試警告開啟失敗");
            var presenter = new WarningPopupPresenter(panel);
            var failed = false;
            try { await presenter.Run("測試警告", CancellationToken.None); }
            catch (InvalidOperationException) { failed = true; }
            Assert.That(failed, Is.True);
            Assert.That(panel.CloseCount, Is.EqualTo(1));
            panel.CloseButtons[0].onClick.Invoke();
            Assert.That(panel.CloseCount, Is.EqualTo(1));
        });

        private static async UniTask _Finish(UniTask run)
        {
            for (var iteration = 0; iteration < 100 && run.Status == UniTaskStatus.Pending; iteration++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await UniTask.Yield();
            }
            Assert.That(run.Status, Is.Not.EqualTo(UniTaskStatus.Pending), "警告流程應在期限內完成");
            await run;
        }

        private Panel _CreatePanel()
        {
            var buttons = new Button[2];
            for (var index = 0; index < buttons.Length; index++)
            {
                var obj = new GameObject($"WarningClose{index}", typeof(RectTransform), typeof(Button));
                _objects.Add(obj);
                buttons[index] = obj.GetComponent<Button>();
            }
            return new Panel(buttons);
        }

        private sealed class Panel : IWarningPopupPanel
        {
            public Action<string> OnOpen;
            public Button[] CloseButtons { get; }
            public int CloseCount;
            public Panel(Button[] closeButtons) => CloseButtons = closeButtons;
            public void Open(string message)
            {
                OnOpen?.Invoke(message);
            }
            public void Close() => CloseCount++;
        }
    }
}
