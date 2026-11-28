using System;
using MortalGame.Exceptions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using MortalGame.Presenter;
using NUnit.Framework;
using Optional;
using TMPro;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class SingleCardDetailPresentationTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown() { if (_root != null) UnityEngine.Object.DestroyImmediate(_root); }

        [UnityTest]
        public IEnumerator Presenter_UsesLatestBattleCardAndUpdatesCardAndHintsTogether() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            s.Model.UpdateCardInfo(s.Card with { Cost = Option.Some(7) });
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(s.Card, lifetime.Token).Preserve();
            try
            {
                Assert.That(_Cost(s).text, Is.EqualTo("7"), "開窗應讀取最新值，不使用傳入舊快照");
                s.Model.UpdateCardInfo(s.Card with { Cost = Option.Some(9), Keywords = new[] { "detail-keyword" } });
                Assert.That(_Cost(s).text, Is.EqualTo("9"));
                Assert.That(_HintContent(s, "_cardKeywordHint").activeSelf, Is.True);
                s.Model.UpdateCardInfo(s.Card with { Cost = Option.Some(2) });
                Assert.That(_Cost(s).text, Is.EqualTo("2"));
                Assert.That(_HintContent(s, "_cardKeywordHint").activeSelf, Is.False);
                _CloseButton(s).onClick.Invoke();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                await run;
                Assert.That(s.Popup.activeSelf, Is.False);
                s.Model.UpdateCardInfo(s.Card with { Cost = Option.Some(11), Keywords = new[] { "detail-keyword" } });
                Assert.That(_Cost(s).text, Is.EqualTo("2"), "關閉後卡面不得繼續訂閱資料");
                Assert.That(_HintContent(s, "_cardKeywordHint").activeSelf, Is.False);
            }
            finally { lifetime.Cancel(); await run.SuppressCancellationThrow(); }
        });

        [UnityTest]
        public IEnumerator Preview_WithoutSourceKeepsProvidedInfoAndClosesNormally() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            var preview = s.Card with { Identity = Guid.NewGuid(), Cost = Option.Some(4) };
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(preview, lifetime.Token).Preserve();
            try
            {
                s.Model.UpdateCardInfo(s.Card with { Cost = Option.Some(99) });
                Assert.That(_Cost(s).text, Is.EqualTo("4"));
                Assert.That(s.CardView.TargetIdentity, Is.EqualTo(preview.Identity));
                _CloseButton(s).onClick.Invoke();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                await run;
            }
            finally { lifetime.Cancel(); await run.SuppressCancellationThrow(); }
        });

        [Test]
        public void Session_RendersOnlyStateAndIsolatesReopenAndReleasesHints()
        {
            var s = _Create();
            using var state = new ReactiveProperty<SingleCardDetailDisplayState>(new(s.Card));
            using var first = s.Panel.Open(state);
            s.Model.UpdateCardInfo(s.Card with { Cost = Option.Some(88) });
            Assert.That(_Cost(s).text, Is.EqualTo(s.Card.Cost.ValueOr(0).ToString()), "View 只能呈現 State");
            state.Value = new(s.Card with { Cost = Option.Some(6), Keywords = new[] { "detail-keyword" } });
            Assert.That(_Cost(s).text, Is.EqualTo("6"));
            var content = _HintContent(s, "_cardKeywordHint");
            Assert.That(content.activeSelf, Is.True);
            var count = 0;
            var ended = false;
            using var subscription = first.CloseRequested.Subscribe(_ => count++, () => ended = true);
            var error = Assert.Throws<SessionAlreadyOpenException>(() => s.Panel.Open(state));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(SingleCardDetailPopupPanel)));
            first.Dispose();
            first.Dispose();
            Assert.That(ended, Is.True);
            Assert.That(count, Is.Zero);
            Assert.That(s.CardView.TargetIdentity, Is.EqualTo(Guid.Empty));
            Assert.That(content.activeSelf, Is.False);
            using var nextState = new ReactiveProperty<SingleCardDetailDisplayState>(new(s.Card with { Cost = Option.Some(3) }));
            using var second = s.Panel.Open(nextState);
            var nextCount = 0;
            using var nextSubscription = second.CloseRequested.Subscribe(_ => nextCount++);
            first.Dispose();
            state.Value = new(s.Card with { Cost = Option.Some(77) });
            Assert.That(_Cost(s).text, Is.EqualTo("3"));
            _CloseButton(s).onClick.Invoke();
            Assert.That(count, Is.Zero);
            Assert.That(nextCount, Is.EqualTo(1));
            Assert.That(s.Popup.activeSelf, Is.True, "操作事件由 Presenter 決定何時結束");
        }

        [UnityTest]
        public IEnumerator Cancellation_ClosesHintsAndAllowsReopenAndAlreadyCancelledDoesNotOpen() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(s.Card with { Identity = Guid.NewGuid(), Keywords = new[] { "detail-keyword" } }, lifetime.Token).Preserve();
            lifetime.Cancel();
            await _Wait(() => run.Status != UniTaskStatus.Pending);
            Assert.That(await run.SuppressCancellationThrow(), Is.True);
            Assert.That(s.Popup.activeSelf, Is.False);
            Assert.That(_HintContent(s, "_cardKeywordHint").activeSelf, Is.False);
            Assert.That(await s.Presenter.Run(s.Card, lifetime.Token).SuppressCancellationThrow(), Is.True);
            Assert.That(s.Popup.activeSelf, Is.False);
            using var state = new ReactiveProperty<SingleCardDetailDisplayState>(new(s.Card));
            using var reopened = s.Panel.Open(state);
            Assert.That(s.Popup.activeSelf, Is.True);
        });

        [Test]
        public void OpenFailure_CleansPartialRenderAndAllowsReopen()
        {
            var s = _Create();
            using var state = new ReactiveProperty<SingleCardDetailDisplayState>(null);
            Assert.Throws<NullReferenceException>(() => s.Panel.Open(state));
            Assert.That(s.Popup.activeSelf, Is.False);
            Assert.That(s.CardView.TargetIdentity, Is.EqualTo(Guid.Empty));
            state.Value = new(s.Card);
            using var reopened = s.Panel.Open(state);
            Assert.That(s.CardView.TargetIdentity, Is.EqualTo(s.Card.Identity));
        }

        private Setup _Create()
        {
            _root = new GameObject("單卡詳情測試", typeof(RectTransform), typeof(Canvas));
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/SingleCardDetailPopup.prefab"), _root.transform);
            var panel = instance.GetComponentInChildren<SingleCardDetailPopupPanel>(true);
            var data = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var info = card.ToInfo(built.Manager) with { Keywords = Array.Empty<string>() };
            var model = new GameViewModel();
            var library = new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>());
            var serialized = new SerializedObject(panel);
            var popup = (GameObject)serialized.FindProperty("_panel").objectReferenceValue;
            popup.SetActive(false);
            return new Setup(panel, popup, (CardView)serialized.FindProperty("_cardView").objectReferenceValue,
                model, new SingleCardDetailPresenter(panel, model, library), info);
        }

        private static TMP_Text _Cost(Setup s)
            => (TMP_Text)new SerializedObject(s.CardView).FindProperty("_cost").objectReferenceValue;
        private static Button _CloseButton(Setup s)
            => (Button)new SerializedObject(s.Panel).FindProperty("_closeButtons").GetArrayElementAtIndex(0).objectReferenceValue;
        private static GameObject _HintContent(Setup s, string field)
        {
            var hint = new SerializedObject(s.Panel).FindProperty(field).objectReferenceValue;
            return ((RectTransform)new SerializedObject(hint).FindProperty("_content").objectReferenceValue).gameObject;
        }
        private static async UniTask _Wait(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await UniTask.Yield();
            }
            Assert.That(condition(), Is.True, "單卡詳情流程應在期限內完成");
        }
        private record Setup(SingleCardDetailPopupPanel Panel, GameObject Popup, CardView CardView,
            GameViewModel Model, SingleCardDetailPresenter Presenter, CardInfo Card);
    }
}
