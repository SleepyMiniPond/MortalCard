#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using MortalGame.Presenter;
using NUnit.Framework;
using Optional;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class FocusCardDetailLifetimeTests
    {
        private GameObject _root;
        private AllyHandCardView _hand;
        private FocusCardDetailView _focus;
        private CardView _focusCard;
        private GameObject _popup;
        private GameViewModel _model;
        private CardInfo _first;
        private CardInfo _second;
        private Receiver _receiver;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("懸停生命週期測試", typeof(RectTransform), typeof(Canvas));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var focusInstance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/FocusCardDetailView.prefab"), _root.transform);
            _focus = focusInstance.GetComponentInChildren<FocusCardDetailView>(true);
            var fields = new SerializedObject(_focus);
            _focusCard = (CardView)fields.FindProperty("_cardView").objectReferenceValue;
            _popup = (GameObject)fields.FindProperty("_panel").objectReferenceValue;
            _popup.SetActive(false);
            var handInstance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/AllyHandCardView.prefab"), _root.transform);
            _hand = handInstance.GetComponentInChildren<AllyHandCardView>(true);
            var library = new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>());
            _model = new GameViewModel();
            _focus.Init(_model, library);
            _receiver = new Receiver(_root.GetComponent<RectTransform>());
            _hand.Init(null, _receiver, _model, new PanelView(_focus), library);
            _first = new CardInfo(Guid.NewGuid(), "focus-test", default, default, Array.Empty<CardTheme>(),
                1, 1.Some(), 1, 1.Some(), new MainSelectionInfo(SelectType.None, default, default),
                Array.Empty<CardBuffInfo>(), Array.Empty<CardProperty>(), Array.Empty<string>());
            _second = _first with { Identity = Guid.NewGuid() };
            _model.UpdateCardInfo(_first);
            _model.UpdateCardInfo(_second);
            _hand.CreateCardView(_first);
            _hand.CreateCardView(_second);
            _hand.EnableHandCardsUseCardAction(new[] { _first, _second }.ToCardCollectionInfo(CardCollectionType.HandCard));
        }

        [TearDown]
        public void TearDown() { if (_root != null) UnityEngine.Object.DestroyImmediate(_root); }

        [Test]
        public void HoverSwitchExitAndDisable_ReleasePreviousFocusAndRestoreCards()
        {
            var first = _Card(_first.Identity);
            var second = _Card(_second.Identity);
            _Pointer(first, true);
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(_first.Identity));
            Assert.That(_Alpha(first), Is.Zero);
            Assert.That(_Offsets(second).ContainsKey(_first.Identity), Is.True);
            _model.UpdateCardInfo(_first with { Cost = 7.Some() });
            Assert.That(_Cost(), Is.EqualTo("7"));
            // 即使沒有先收到上一張的 Exit，也必須釋放舊顯示範圍。
            _Pointer(second, true);
            Assert.That(_Alpha(first), Is.EqualTo(1));
            Assert.That(_Offsets(second).ContainsKey(_first.Identity), Is.False);
            Assert.That(_Alpha(second), Is.Zero);
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(_second.Identity));
            _Pointer(first, false);
            Assert.That(_popup.activeSelf, Is.True, "舊卡片的遲到 Exit 不得關閉新焦點");
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(_second.Identity));
            _Pointer(second, false);
            Assert.That(_popup.activeSelf, Is.False);
            Assert.That(_Alpha(second), Is.EqualTo(1));
            Assert.That(_Offsets(first).ContainsKey(_second.Identity), Is.False);
            _model.UpdateCardInfo(_second with { Cost = 88.Some() });
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(Guid.Empty));
            _Pointer(first, true);
            _hand.DisableAllHandCardsAction();
            Assert.That(_popup.activeSelf, Is.False);
            Assert.That(_Alpha(first), Is.EqualTo(1));
            _hand.DisableAllHandCardsAction();
        }

        [Test]
        public void InputSession_ReportsIdentityAndCoordinatesAndEndsWithoutFakeActions()
        {
            var card = _Card(_first.Identity);
            _hand.DisableAllHandCardsAction();
            using var input = card.OpenHandInput();
            Assert.Throws<InvalidOperationException>(() => card.OpenHandInput());
            var entered = new List<Guid>();
            var exited = new List<Guid>();
            var started = new List<(Guid Identity, Vector2 Position)>();
            var moved = new List<(Guid Identity, Vector2 Position)>();
            var ended = new List<(Guid Identity, Vector2 Position)>();
            var completed = 0;
            using var subscriptions = new CompositeDisposable(
                input.PointerEntered.Subscribe(entered.Add, () => completed++),
                input.PointerExited.Subscribe(exited.Add, () => completed++),
                input.DragStarted.Subscribe(started.Add, () => completed++),
                input.Dragged.Subscribe(moved.Add, () => completed++),
                input.DragEnded.Subscribe(ended.Add, () => completed++));
            var position = new Vector2(123, 234);
            _Pointer(card, true);
            _Pointer(card, false);
            _DragEvent(card, ExecuteEvents.beginDragHandler, position);
            _DragEvent(card, ExecuteEvents.dragHandler, position + Vector2.one);
            _DragEvent(card, ExecuteEvents.endDragHandler, position + Vector2.one * 2);
            Assert.That(entered, Is.EqualTo(new[] { _first.Identity }));
            Assert.That(exited, Is.EqualTo(new[] { _first.Identity }));
            Assert.That(started.Single(), Is.EqualTo((_first.Identity, position)));
            Assert.That(moved.Single(), Is.EqualTo((_first.Identity, position + Vector2.one)));
            Assert.That(ended.Single(), Is.EqualTo((_first.Identity, position + Vector2.one * 2)));
            _model.UpdateCardInfo(_first with { Cost = 8.Some() });
            _Pointer(card, true);
            Assert.That(entered.Count, Is.EqualTo(2), "資訊更新不應重建或終止輸入範圍");
            input.Dispose();
            input.Dispose();
            Assert.That(completed, Is.EqualTo(5));
            _Pointer(card, true);
            _Pointer(card, false);
            _DragEvent(card, ExecuteEvents.endDragHandler, position);
            Assert.That(entered.Count, Is.EqualTo(2));
            Assert.That(exited.Count, Is.EqualTo(1));
            Assert.That(ended.Count, Is.EqualTo(1), "Dispose 不得偽造放開事件");
        }

        [Test]
        public void InputSession_ResetAndModeSwitchEndOldStreamsBeforeReuse()
        {
            var card = _Card(_first.Identity);
            _hand.DisableAllHandCardsAction();
            using var old = card.OpenHandInput();
            var oldCount = 0;
            var oldEnded = false;
            using var oldSubscription = old.PointerEntered.Subscribe(_ => oldCount++, () => oldEnded = true);
            card.Reset();
            Assert.That(oldEnded, Is.True);
            var nextInfo = _first with { Identity = Guid.NewGuid() };
            card.Render(new ICardView.CardSimpleProperty(nextInfo));
            using var next = card.OpenHandInput();
            var newIds = new List<Guid>();
            var nextEnded = false;
            using var nextSubscription = next.PointerEntered.Subscribe(newIds.Add, () => nextEnded = true);
            old.Dispose();
            _Pointer(card, true);
            Assert.That(oldCount, Is.Zero);
            Assert.That(newIds, Is.EqualTo(new[] { nextInfo.Identity }));
            card.RenderSnapshot(_first);
            Assert.That(nextEnded, Is.True);
            _Pointer(card, true);
            Assert.That(newIds.Count, Is.EqualTo(1));
            using var clickableInput = card.OpenHandInput();
            var clickableEnded = false;
            using var clickableSubscription = clickableInput.PointerEntered.Subscribe(_ => { }, () => clickableEnded = true);
            card.Render(new ICardView.CardClickableProperty(_first, true));
            Assert.That(clickableEnded, Is.True);
            using var simpleInput = card.OpenHandInput();
            var simpleEnded = false;
            using var simpleSubscription = simpleInput.PointerEntered.Subscribe(_ => { }, () => simpleEnded = true);
            card.Render(new ICardView.CardSimpleProperty(_first));
            Assert.That(simpleEnded, Is.True);
        }

        [UnityTest]
        public IEnumerator DestroyCard_CompletesInputSessionAndAllowsRepeatedDispose()
        {
            var card = _Card(_first.Identity);
            _hand.DisableAllHandCardsAction();
            using var input = card.OpenHandInput();
            var ended = false;
            var count = 0;
            using var subscription = input.DragEnded.Subscribe(_ => count++, () => ended = true);
            UnityEngine.Object.Destroy(card);
            yield return null;
            Assert.That(ended, Is.True);
            Assert.That(count, Is.Zero);
            Assert.DoesNotThrow(input.Dispose);
        }

        [Test]
        public void RebindHandInput_DoesNotDuplicateObserversOrDropCommands()
        {
            var first = _Card(_first.Identity);
            _Pointer(first, true);
            var hand = new[] { _first, _second }.ToCardCollectionInfo(CardCollectionType.HandCard);
            _hand.EnableHandCardsUseCardAction(hand);
            _hand.EnableHandCardsUseCardAction(hand);
            _Pointer(first, false);
            Assert.That(_popup.activeSelf, Is.False);
            _Pointer(first, true);
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(_first.Identity));
            var center = RectTransformUtility.WorldToScreenPoint(null, _root.transform.position);
            _DragEvent(first, ExecuteEvents.beginDragHandler, center);
            _DragEvent(first, ExecuteEvents.endDragHandler, center);
            Assert.That(_receiver.Commands.Count, Is.EqualTo(1));
            _hand.DisableAllHandCardsAction();
            _Pointer(first, true);
            Assert.That(_popup.activeSelf, Is.False);
            _DragEvent(first, ExecuteEvents.beginDragHandler, center);
            _DragEvent(first, ExecuteEvents.endDragHandler, center);
            Assert.That(_receiver.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void DragAndRecycle_CloseFocusAndAllowNextHover()
        {
            var first = _Card(_first.Identity);
            _Pointer(first, true);
            var button = (Button)_Get(first, "_button");
            var pointer = new PointerEventData(null)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, first.RectTransform.position)
            };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.beginDragHandler);
            Assert.That(_popup.activeSelf, Is.False);
            _hand.DisableAllHandCardsAction();
            _hand.EnableHandCardsUseCardAction(new[] { _first, _second }.ToCardCollectionInfo(CardCollectionType.HandCard));
            _Pointer(first, true);
            _hand.RemoveCardView(new UsedCardEvent(Faction.Ally, _first.Identity, null, CardPlayReason.Active));
            Assert.That(_popup.activeSelf, Is.False);
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(Guid.Empty));
            _Pointer(_Card(_second.Identity), true);
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(_second.Identity));
        }

        [UnityTest]
        public IEnumerator DestroyHand_ClosesOwnedFocusAndRestoresSourceCard()
        {
            var first = _Card(_first.Identity);
            _Pointer(first, true);
            Assert.That(_popup.activeSelf, Is.True);
            UnityEngine.Object.Destroy(_hand);
            yield return null;
            Assert.That(_popup.activeSelf, Is.False);
            Assert.That(_Alpha(first), Is.EqualTo(1));
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(Guid.Empty));
            _model.UpdateCardInfo(_first with { Cost = 91.Some() });
            Assert.That(_focusCard.TargetIdentity, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void DragRelease_UsesEndPositionAndCompletesOnlyOnce()
        {
            var card = _Card(_first.Identity);
            var origin = card.RectTransform.anchoredPosition;
            var center = RectTransformUtility.WorldToScreenPoint(null, _root.transform.position);
            _DragEvent(card, ExecuteEvents.beginDragHandler, center);
            _DragEvent(card, ExecuteEvents.dragHandler, center);
            _DragEvent(card, ExecuteEvents.endDragHandler, new Vector2(-1000, -1000));
            Assert.That(_receiver.Commands, Is.Empty, "不能沿用最後一筆 Drag 的有效位置");
            Assert.That(card.RectTransform.anchoredPosition, Is.EqualTo(origin));
            Assert.That(_Alpha(card), Is.EqualTo(1));
            _DragEvent(card, ExecuteEvents.beginDragHandler, new Vector2(-1000, -1000));
            _DragEvent(card, ExecuteEvents.endDragHandler, center);
            Assert.That(_receiver.Commands.Count, Is.EqualTo(1));
            Assert.That(_receiver.Commands[0].CardIndentity, Is.EqualTo(_first.Identity));
            _DragEvent(card, ExecuteEvents.endDragHandler, center);
            Assert.That(_receiver.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void DragCardUpdates_RefreshHighlightAndReleaseRechecksTargetType()
        {
            var rect = new GameObject("拖曳候選目標", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(_root.transform, false);
            rect.sizeDelta = new Vector2(200, 200);
            var target = new Target(rect, TargetType.EnemyCharacter);
            _receiver.Targets.Add(target);
            var card = _Card(_first.Identity);
            var center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            var arrow = (Component)_Get(_hand, "_customLineRenderer");
            _DragEvent(card, ExecuteEvents.beginDragHandler, center);
            _model.UpdateCardInfo(_first with { MainSelectable = new MainSelectionInfo(SelectType.EnemyCharacter, default, default) });
            Assert.That(target.IsSelected, Is.True);
            Assert.That(arrow.gameObject.activeSelf, Is.True);
            _model.UpdateCardInfo(_first with { MainSelectable = new MainSelectionInfo(SelectType.AllyCharacter, default, default) });
            Assert.That(target.IsSelected, Is.False);
            Assert.That(arrow.gameObject.activeSelf, Is.False);
            _DragEvent(card, ExecuteEvents.endDragHandler, center);
            Assert.That(_receiver.Commands, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DragCancellation_NeverSendsCommandOrAcceptsLateRelease(int reason)
        {
            var card = _Card(_first.Identity);
            var center = RectTransformUtility.WorldToScreenPoint(null, _root.transform.position);
            _DragEvent(card, ExecuteEvents.beginDragHandler, center);
            switch (reason)
            {
                case 0: _hand.DisableAllHandCardsAction(); break;
                case 1: _hand.RemoveCardView(new UsedCardEvent(Faction.Ally, _first.Identity, null, CardPlayReason.Active)); break;
                case 2: UnityEngine.Object.DestroyImmediate(_hand); break;
                case 3: UnityEngine.Object.DestroyImmediate(_root); break;
            }
            if (card != null) _DragEvent(card, ExecuteEvents.endDragHandler, center);
            Assert.That(_receiver.Commands, Is.Empty);
            if (card != null) Assert.That(_Alpha(card), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyFocus_ReleasesStateAndDisposeRemainsSafe()
        {
            var state = new SubscriptionProbe(new FocusCardDetailDisplayState(_first));
            using var session = _focus.Open(state, _Card(_first.Identity).RectTransform);
            Assert.That(state.IsSubscribed, Is.True);
            UnityEngine.Object.Destroy(_focus);
            yield return null;
            Assert.That(state.IsSubscribed, Is.False);
            Assert.That(_popup.activeSelf, Is.False);
            Assert.DoesNotThrow(session.Dispose);
        }

        private CardView _Card(Guid identity) => _hand.SelectableViews.Cast<CardView>().Single(card => card.TargetIdentity == identity);
        private static float _Alpha(CardView card) => ((CanvasGroup)_Get(card, "_canvasGroup")).alpha;
        private static void _DragEvent<T>(CardView card, ExecuteEvents.EventFunction<T> handler, Vector2 position)
            where T : IEventSystemHandler
        {
            var button = (Button)_Get(card, "_button");
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(null) { position = position }, handler);
        }
        private static Dictionary<Guid, List<Vector3>> _Offsets(CardView card)
            => (Dictionary<Guid, List<Vector3>>)_Get(card, "_offsets");
        private string _Cost()
        {
            var text = _Get(_focusCard, "_cost");
            return (string)text.GetType().GetProperty("text").GetValue(text);
        }
        private static void _Pointer(CardView card, bool enter)
        {
            var button = (Button)_Get(card, "_button");
            var pointer = new PointerEventData(null);
            if (enter) ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            else ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        }
        private static object _Get(object owner, string field) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private sealed class PanelView : IAllCardDetailPanelView
        {
            public FocusCardDetailView FocusCardDetailView { get; }
            public PanelView(FocusCardDetailView focus) => FocusCardDetailView = focus;
            public AllCardDetailPanel DetailPanel => null;
            public SingleCardDetailPopupPanel SinglePopupPanel => null;
            public ICardSelectionPanel CardSelectionPanel => null;
        }
        private sealed class Receiver : IGameplayActionReciever
        {
            public readonly List<ISelectableView> Targets = new();
            public readonly List<UseCardCommand> Commands = new();
            public IEnumerable<ISelectableView> SelectableViews => Targets;
            public ISelectableView BasicSelectableView { get; }
            public Receiver(RectTransform rect) => BasicSelectableView = new Target(rect);
            public void RecieveEvent(IGameCommand command) => Commands.Add((UseCardCommand)command);
        }
        private sealed class Target : ISelectableView
        {
            public RectTransform RectTransform { get; }
            public TargetType TargetType { get; }
            public Guid TargetIdentity => Guid.Empty;
            public bool IsSelected { get; private set; }
            public Target(RectTransform rect, TargetType type = TargetType.AllyCard)
            { RectTransform = rect; TargetType = type; }
            public void OnSelect() => IsSelected = true;
            public void OnDeselect() => IsSelected = false;
        }
        private sealed class SubscriptionProbe : IReadOnlyReactiveProperty<FocusCardDetailDisplayState>
        {
            public FocusCardDetailDisplayState Value { get; }
            public bool HasValue => true;
            public bool IsSubscribed { get; private set; }
            public SubscriptionProbe(FocusCardDetailDisplayState value) => Value = value;
            public IDisposable Subscribe(IObserver<FocusCardDetailDisplayState> observer)
            {
                observer.OnNext(Value);
                IsSubscribed = true;
                return Disposable.Create(() => IsSubscribed = false);
            }
        }
    }
}
#endif
