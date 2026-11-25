using System;
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
using TMPro;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class AllCardDetailPresentationTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects) UnityEngine.Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        [Test]
        public void PanelState_UpdatesContentAndOrderWithoutRecreatingRetainedCards()
        {
            var s = _Create();
            using var state = new ReactiveProperty<AllCardDetailDisplayState>(new(
                CardCollectionType.Deck, Array.AsReadOnly(new[] { s.First, s.Second })));
            using var session = s.Panel.Open(state);
            var original = _Cards(s.Panel);
            Assert.That(original.Select(card => card.TargetIdentity), Is.EqualTo(new[] { s.First.Identity, s.Second.Identity }));

            state.Value = state.Value with { Cards = Array.AsReadOnly(new[] { s.Second, s.First }) };
            Assert.That(_Cards(s.Panel), Is.EqualTo(new[] { original[1], original[0] }));
            state.Value = new AllCardDetailDisplayState(CardCollectionType.HandCard,
                Array.AsReadOnly(new[] { s.First }));
            Assert.That(_Cards(s.Panel), Is.EqualTo(new[] { original[0] }));
            Assert.That(original[1].transform.parent, Is.Not.SameAs(_Content(s.Panel)));
            state.Value = new AllCardDetailDisplayState(CardCollectionType.HandCard,
                Array.AsReadOnly(new[] { s.First with { Cost = Optional.Option.Some(9) }, s.Third }));
            Assert.That(_Cards(s.Panel)[0], Is.SameAs(original[0]));
            Assert.That(_Cards(s.Panel).Select(card => card.TargetIdentity), Is.EqualTo(new[] { s.First.Identity, s.Third.Identity }));
            // 註冊中的單卡仍由 GameViewModel 提供最新資訊。
            s.ViewModel.UpdateCardInfo(s.First with { Cost = Optional.Option.Some(9) });
            Assert.That(_Text(original[0], "_cost").text, Is.EqualTo("9"));

            session.Dispose();
            Assert.That(_Cards(s.Panel), Is.Empty);
            Assert.That(_PanelObject(s.Panel).activeSelf, Is.False);
            state.Value = state.Value with { Cards = Array.AsReadOnly(new[] { s.Second }) };
            Assert.That(_Cards(s.Panel), Is.Empty, "Session 釋放後不得繼續建立卡片");
        }

        [Test]
        public void PanelSession_RoutesActionsAndOldSessionCannotReceiveNewInput()
        {
            var s = _Create();
            using var state = new ReactiveProperty<AllCardDetailDisplayState>(new(
                CardCollectionType.Deck, Array.AsReadOnly(new[] { s.First })));
            using var first = s.Panel.Open(state);
            var requested = new List<CardCollectionType>();
            var clicked = new List<Guid>();
            var closeCount = 0;
            var completed = false;
            using var tabs = first.CollectionRequested.Subscribe(requested.Add, () => completed = true);
            using var cards = first.CardClicked.Subscribe(clicked.Add);
            using var close = first.CloseRequested.Subscribe(_ => closeCount++);
            _Button(s.Panel, "_deckButton").onClick.Invoke();
            _Button(s.Panel, "_handCardButton").onClick.Invoke();
            _Button(s.Panel, "_graveyardButton").onClick.Invoke();
            _Click(_Cards(s.Panel).Single());
            Assert.That(requested, Is.EqualTo(new[] { CardCollectionType.Deck, CardCollectionType.HandCard, CardCollectionType.Graveyard }));
            Assert.That(clicked, Is.EqualTo(new[] { s.First.Identity }));
            Assert.Throws<InvalidOperationException>(() => s.Panel.Open(state));

            first.Dispose();
            Assert.That(completed, Is.True);
            Assert.That(closeCount, Is.Zero, "Dispose 不應偽造玩家關閉操作");
            using var second = s.Panel.Open(state);
            var secondRequests = 0;
            using var newTabs = second.CollectionRequested.Subscribe(_ => secondRequests++);
            _Button(s.Panel, "_handCardButton").onClick.Invoke();
            first.Dispose();
            Assert.That(requested.Count, Is.EqualTo(3));
            Assert.That(secondRequests, Is.EqualTo(1));
            Assert.That(_PanelObject(s.Panel).activeSelf, Is.True);
        }

        [Test]
        public void PanelSession_OpenFailureAllowsNextOpen()
        {
            var s = _Create();
            using var state = new ReactiveProperty<AllCardDetailDisplayState>(new(CardCollectionType.Deck, null));
            Assert.Throws<ArgumentNullException>(() => s.Panel.Open(state));
            Assert.That(_PanelObject(s.Panel).activeSelf, Is.False);
            state.Value = state.Value with { Cards = Array.AsReadOnly(new[] { s.First }) };
            using var session = s.Panel.Open(state);
            Assert.That(_Cards(s.Panel).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Presenter_UpdatesCurrentCollectionAndStopsObservingPreviousCollection() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            s.ViewModel.UpdateCardCollectionInfo(Faction.Enemy, new[] { s.Third }.ToCardCollectionInfo(CardCollectionType.Deck));
            s.ViewModel.UpdateCardCollectionInfo(Faction.Enemy, new[] { s.Second }.ToCardCollectionInfo(CardCollectionType.HandCard));
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(Faction.Enemy, CardCollectionType.Deck, lifetime.Token).Preserve();
            try
            {
                Assert.That(_Cards(s.Panel).Single().TargetIdentity, Is.EqualTo(s.Third.Identity));
                var thirdView = _Cards(s.Panel).Single();
                s.ViewModel.UpdateCardCollectionInfo(Faction.Enemy,
                    new[] { s.First, s.Third }.ToCardCollectionInfo(CardCollectionType.Deck));
                Assert.That(_Cards(s.Panel).Select(card => card.TargetIdentity),
                    Is.EquivalentTo(new[] { s.First.Identity, s.Third.Identity }));
                Assert.That(_Cards(s.Panel).Single(card => card.TargetIdentity == s.Third.Identity), Is.SameAs(thirdView));
                _Button(s.Panel, "_handCardButton").onClick.Invoke();
                Assert.That(_Cards(s.Panel).Single().TargetIdentity, Is.EqualTo(s.Second.Identity));
                var handView = _Cards(s.Panel).Single();
                s.ViewModel.UpdateCardCollectionInfo(Faction.Enemy,
                    new[] { s.First }.ToCardCollectionInfo(CardCollectionType.Deck));
                s.ViewModel.UpdateCardCollectionInfo(Faction.Ally,
                    new[] { s.Third }.ToCardCollectionInfo(CardCollectionType.HandCard));
                Assert.That(_Cards(s.Panel), Is.EqualTo(new[] { handView }), "舊卡區或其他陣營更新不得覆寫目前頁籤");
                s.ViewModel.UpdateCardCollectionInfo(Faction.Enemy,
                    new[] { s.Second, s.First }.ToCardCollectionInfo(CardCollectionType.HandCard));
                Assert.That(_Cards(s.Panel).Select(card => card.TargetIdentity),
                    Is.EquivalentTo(new[] { s.First.Identity, s.Second.Identity }));
                Assert.That(_Cards(s.Panel).Single(card => card.TargetIdentity == s.Second.Identity), Is.SameAs(handView));
                _Button(s.Panel, "_deckButton").onClick.Invoke();
                Assert.That(_Cards(s.Panel).Single().TargetIdentity, Is.EqualTo(s.First.Identity));
                _Button(s.Panel, "_graveyardButton").onClick.Invoke();
                Assert.That(_Cards(s.Panel), Is.Empty);
                s.ViewModel.UpdateCardCollectionInfo(Faction.Enemy,
                    new[] { s.Third }.ToCardCollectionInfo(CardCollectionType.Graveyard));
                Assert.That(_Cards(s.Panel).Single().TargetIdentity, Is.EqualTo(s.Third.Identity));
                _CloseButton(s.Panel).onClick.Invoke();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                await run;
                Assert.That(_PanelObject(s.Panel).activeSelf, Is.False);
                s.ViewModel.UpdateCardCollectionInfo(Faction.Enemy,
                    new[] { s.First }.ToCardCollectionInfo(CardCollectionType.Graveyard));
                Assert.That(_Cards(s.Panel), Is.Empty, "流程結束後的卡區更新不得重建已關閉畫面");
                run = s.Presenter.Run(Faction.Enemy, CardCollectionType.Graveyard, lifetime.Token).Preserve();
                Assert.That(_Cards(s.Panel).Single().TargetIdentity, Is.EqualTo(s.First.Identity));
                _CloseButton(s.Panel).onClick.Invoke();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                await run;
            }
            finally { lifetime.Cancel(); await _Stop(run); }
        });

        [UnityTest]
        public IEnumerator Presenter_DetailUsesLatestInfoAndBlocksParentUntilChildCloses() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var lifetime = new CancellationTokenSource();
            var run = s.Presenter.Run(Faction.Ally, CardCollectionType.Deck, lifetime.Token).Preserve();
            try
            {
                var originalCard = _Cards(s.Panel).Single();
                s.ViewModel.UpdateCardInfo(s.First with { Cost = Optional.Option.Some(9) });
                Assert.That(_Cards(s.Panel).Single(), Is.SameAs(originalCard));
                Assert.That(_Text(originalCard, "_cost").text, Is.EqualTo("9"));
                _Click(originalCard);
                Assert.That(_PanelObject(s.Single).activeSelf, Is.True);
                var detailCard = (CardView)new SerializedObject(s.Single).FindProperty("_cardView").objectReferenceValue;
                Assert.That(_Text(detailCard, "_cost").text, Is.EqualTo("9"));
                _Button(s.Panel, "_handCardButton").onClick.Invoke();
                _CloseButton(s.Panel).onClick.Invoke();
                s.ViewModel.UpdateCardCollectionInfo(Faction.Ally,
                    new[] { s.First with { Cost = Optional.Option.Some(9) }, s.Third }
                        .ToCardCollectionInfo(CardCollectionType.Deck));
                Assert.That(_Cards(s.Panel).Select(card => card.TargetIdentity),
                    Is.EquivalentTo(new[] { s.First.Identity, s.Third.Identity }), "詳情開啟期間仍須同步目前卡區");
                Assert.That(_Cards(s.Panel).Single(card => card.TargetIdentity == s.First.Identity), Is.SameAs(originalCard));
                Assert.That(_PanelObject(s.Single).activeSelf, Is.True);
                Assert.That(run.Status, Is.EqualTo(UniTaskStatus.Pending));

                _CloseButton(s.Single).onClick.Invoke();
                await _Wait(() => !_PanelObject(s.Single).activeSelf);
                _Button(s.Panel, "_handCardButton").onClick.Invoke();
                Assert.That(_Cards(s.Panel).Single().TargetIdentity, Is.EqualTo(s.Second.Identity));
                _CloseButton(s.Panel).onClick.Invoke();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                await run;
            }
            finally { lifetime.Cancel(); await _Stop(run); }
        });

        [UnityTest]
        public IEnumerator Presenter_CancelDuringDetailClosesBothPanelsAndAllowsReopen() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var firstLifetime = new CancellationTokenSource();
            var first = s.Presenter.Run(Faction.Ally, CardCollectionType.Deck, firstLifetime.Token).Preserve();
            try
            {
                _Click(_Cards(s.Panel).Single());
                Assert.That(_PanelObject(s.Single).activeSelf, Is.True);
            }
            finally { firstLifetime.Cancel(); await _Stop(first); }
            Assert.That(await first.SuppressCancellationThrow(), Is.True);
            Assert.That(_PanelObject(s.Single).activeSelf, Is.False);
            Assert.That(_PanelObject(s.Panel).activeSelf, Is.False);
            Assert.That(_Cards(s.Panel), Is.Empty);

            using var nextLifetime = new CancellationTokenSource();
            var next = s.Presenter.Run(Faction.Ally, CardCollectionType.HandCard, nextLifetime.Token).Preserve();
            try
            {
                Assert.That(_Cards(s.Panel).Single().TargetIdentity, Is.EqualTo(s.Second.Identity));
                _CloseButton(s.Panel).onClick.Invoke();
                await _Wait(() => next.Status != UniTaskStatus.Pending);
                await next;
            }
            finally { nextLifetime.Cancel(); await _Stop(next); }
        });

        private Setup _Create()
        {
            var data = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var entity = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(entity);
            var first = entity.ToInfo(built.Manager);
            var second = first with { Identity = Guid.NewGuid() };
            var third = first with { Identity = Guid.NewGuid() };
            var viewModel = new GameViewModel();
            viewModel.UpdateCardCollectionInfo(Faction.Ally, new[] { first }.ToCardCollectionInfo(CardCollectionType.Deck));
            viewModel.UpdateCardCollectionInfo(Faction.Ally, new[] { second }.ToCardCollectionInfo(CardCollectionType.HandCard));
            viewModel.UpdateCardCollectionInfo(Faction.Ally, new[] { third }.ToCardCollectionInfo(CardCollectionType.Graveyard));
            var library = new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>());
            var panel = _Instantiate<AllCardDetailPanel>("DetailCardInfoPanel");
            var single = _Instantiate<SingleCardDetailPopupPanel>("SingleCardDetailPopup");
            single.Init(viewModel, library);
            _PanelObject(panel).SetActive(false);
            _PanelObject(single).SetActive(false);
            return new Setup(panel, single, viewModel,
                new AllCardDetailPresenter(new PanelView(panel, single), viewModel, library), first, second, third);
        }

        private T _Instantiate<T>(string name) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/UI/OverlayCanvas/{name}.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            _objects.Add(instance);
            return instance.GetComponentInChildren<T>(true);
        }

        private static Transform _Content(AllCardDetailPanel panel)
            => (Transform)new SerializedObject(panel).FindProperty("_cardViewParent").objectReferenceValue;
        private static CardView[] _Cards(AllCardDetailPanel panel)
            => _Content(panel).GetComponentsInChildren<CardView>(true).OrderBy(card => card.transform.GetSiblingIndex()).ToArray();
        private static GameObject _PanelObject(Component panel)
            => (GameObject)new SerializedObject(panel).FindProperty("_panel").objectReferenceValue;
        private static Button _Button(Component panel, string field)
            => (Button)new SerializedObject(panel).FindProperty(field).objectReferenceValue;
        private static Button _CloseButton(Component panel)
            => (Button)new SerializedObject(panel).FindProperty("_closeButtons").GetArrayElementAtIndex(0).objectReferenceValue;
        private static TextMeshProUGUI _Text(CardView card, string field)
            => (TextMeshProUGUI)new SerializedObject(card).FindProperty(field).objectReferenceValue;
        private static void _Click(CardView card)
        {
            var button = _Button(card, "_button");
            var pointer = new PointerEventData(null);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        }
        private static async UniTask _Wait(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await UniTask.Yield();
            }
            Assert.That(condition(), Is.True, "流程應在期限內完成");
        }
        private static async UniTask _Stop(UniTask run)
        {
            await _Wait(() => run.Status != UniTaskStatus.Pending);
            await run.SuppressCancellationThrow();
        }

        private record Setup(AllCardDetailPanel Panel, SingleCardDetailPopupPanel Single, GameViewModel ViewModel,
            AllCardDetailPresenter Presenter, CardInfo First, CardInfo Second, CardInfo Third);
        private record PanelView(AllCardDetailPanel DetailPanel, SingleCardDetailPopupPanel SinglePopupPanel) : IAllCardDetailPanelView
        {
            public FocusCardDetailView FocusCardDetailView => null;
            public ICardSelectionPanel CardSelectionPanel => null;
        }
    }
}
