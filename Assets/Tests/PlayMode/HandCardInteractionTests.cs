using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using MortalGame.Presenter;
using NUnit.Framework;
using Optional;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class HandCardInteractionTests
    {
        private GameObject _root;
        private AllyHandCardView _hand;
        private CardView _dragCard;
        private Receiver _receiver;
        private Vector2 _center;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("手牌互動測試", typeof(RectTransform), typeof(Canvas));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _hand = _root.AddComponent<AllyHandCardView>();
            _dragCard = _CreateCard();
            _receiver = new Receiver { BasicSelectableView = new Target(_CreateRect(), TargetType.AllyCard) };
            _Set(_hand, "_reciever", _receiver);
            Canvas.ForceUpdateCanvases();
            _center = RectTransformUtility.WorldToScreenPoint(null, _dragCard.RectTransform.position);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_root);

        [TestCase(false)]
        [TestCase(true)]
        public void EnemyCreatedHandCard_IsDisplayedOnlyAfterSelectionAndCanBeSelectedAgain(bool used)
        {
            var view = _root.AddComponent<GameplayView>();
            var enemy = _root.AddComponent<EnemySelectedCardView>();
            var factory = _root.AddComponent<AiCardViewFactory>();
            var templateRect = _CreateRect();
            var template = templateRect.gameObject.AddComponent<AiCardView>();
            _Set(template, "_rectTransform", templateRect);
            var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            foreach (var field in new[] { "_title", "_info", "_cost", "_power" })
            {
                var rect = _CreateRect(); rect.SetParent(templateRect, false);
                _Set(template, field, rect.gameObject.AddComponent(textType));
            }
            var factoryType = typeof(PrefabFactory<AiCardView>);
            factoryType.GetField("_prefab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(factory, template);
            factoryType.GetField("_recycleRoot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(factory, _CreateRect());
            var model = new GameViewModel();
            _Set(enemy, "_cardViewFactory", factory);
            _Set(enemy, "_cardViewParent", _CreateRect());
            _Set(enemy, "_handCardArea", _CreateRect());
            _Set(enemy, "_gameViewModel", model);
            _Set(enemy, "_localizeLibrary", new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()));
            _Set(view, "_gameViewModel", model);
            _Set(view, "_enemySelectedCardView", enemy);
            var info = _Info(SelectType.None);
            var manager = new CardManagerInfo(new Dictionary<CardCollectionType, ImmutableArray<Guid>>
            { [CardCollectionType.HandCard] = ImmutableArray.Create(info.Identity) });

            view.Render(new[] { new AddCardEvent(Faction.Enemy, info, CardCollectionType.HandCard, manager) }, _receiver);
            Assert.That(enemy.SelectableViews, Is.Empty, "敵方新手牌尚未預選，不應建立預選畫面。");
            var selected = new EnemySelectCardEvent(info, ImmutableArray.Create(info.Identity));
            view.Render(new[] { selected }, _receiver);
            Assert.That(enemy.SelectableViews.Count(), Is.EqualTo(1));

            if (used) view.Render(new[] { new UsedCardEvent(Faction.Enemy, info.Identity, manager, CardPlayReason.Active) }, _receiver);
            else view.Render(new[] { new EnemyUnselectedCardEvent(ImmutableArray.Create(info.Identity)) }, _receiver);
            Assert.That(enemy.SelectableViews, Is.Empty);
            Assert.DoesNotThrow(() => view.Render(new[] { selected }, _receiver));
            Assert.That(enemy.SelectableViews.Count(), Is.EqualTo(1));
        }

        [Test]
        public void RepeatedHintRefresh_ReplacesPreviousRowsAndHideRecyclesThem()
        {
            var hint = _root.AddComponent<CardPropertyHint>();
            var factory = _root.AddComponent<CardBuffInfoViewFactory>();
            var template = _CreateRect().gameObject.AddComponent<CardBuffInfoView>();
            var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            var title = _CreateRect(); title.SetParent(template.transform, false);
            var body = _CreateRect(); body.SetParent(template.transform, false);
            _Set(template, "_cardBuffTitleText", title.gameObject.AddComponent(textType));
            _Set(template, "_cardBuffInfoText", body.gameObject.AddComponent(textType));
            var factoryType = typeof(PrefabFactory<CardBuffInfoView>);
            factoryType.GetField("_prefab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(factory, template);
            factoryType.GetField("_recycleRoot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(factory, _CreateRect());
            var content = _CreateRect();
            var rows = _CreateRect(); rows.SetParent(content, false);
            _Set(hint, "_cardBuffInfoViewFactory", factory);
            _Set(hint, "_content", content);
            _Set(hint, "_layoutParent", rows);
            hint.Init(new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>
                { [LocalizeTitleInfoType.KeyWord] = new Dictionary<string, LocalizeTitleInfoData> { ["Preserved"] = new("保留", "回合結束保留") } },
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()));
            var data = new CardPropertyHint.ViewData(new[] { new CardPropertyHint.InfoCellViewData(
                LocalizeTitleInfoType.KeyWord, "Preserved", new Dictionary<string, string>()) });

            hint.ShowHint(data, _dragCard.RectTransform);
            hint.ShowHint(data, _dragCard.RectTransform);
            hint.ShowHint(data, _dragCard.RectTransform);

            Assert.That(rows.childCount, Is.EqualTo(1));
            hint.HideHint();
            Assert.That(rows.childCount, Is.Zero);
            Assert.That(content.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ExecuteEnd_DisablesSubmitAndDisposesHandSubscriptions()
        {
            var view = _root.AddComponent<GameplayView>();
            var submit = _root.AddComponent<SubmitView>();
            var button = _root.AddComponent<Button>();
            button.interactable = true;
            _Set(submit, "_submitButton", button);
            _Set(view, "_submitView", submit);
            _Set(view, "_allyHandCardView", _hand);
            _Set(view, "_gameViewModel", new GameViewModel());
            var handle = _Get(_hand, "_handleSubscription");
            var drag = _Get(_hand, "_dragSubscription");
            var handleLifetime = new Disposal();
            var dragLifetime = new Disposal();
            handle.GetType().GetProperty("Disposable").SetValue(handle, handleLifetime);
            drag.GetType().GetProperty("Disposable").SetValue(drag, dragLifetime);

            view.Render(new[] { new PlayerExecuteEndEvent(Faction.Ally,
                new CardManagerInfo(ImmutableDictionary<CardCollectionType, ImmutableArray<Guid>>.Empty)) }, _receiver);

            Assert.That(button.interactable, Is.False);
            Assert.That(handleLifetime.IsDisposed, Is.True);
            Assert.That(dragLifetime.IsDisposed, Is.True);
        }

        [Test]
        public void SubmitInteractable_LiveButtonCanBeDisabledAndEnabled()
        {
            var submit = _root.AddComponent<SubmitView>();
            var button = _root.AddComponent<Button>();
            _Set(submit, "_submitButton", button);

            submit.SetInteractable(false);
            Assert.IsFalse(button.interactable);
            submit.SetInteractable(true);
            Assert.IsTrue(button.interactable);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DisableAfterControlsDestroyed_CompletesRemainingCleanup(int destroyedTarget)
        {
            var view = _root.AddComponent<GameplayView>();
            var submit = _root.AddComponent<SubmitView>();
            var button = _root.AddComponent<Button>();
            _Set(submit, "_submitButton", button);
            _Set(view, "_submitView", submit);
            _Set(view, "_allyHandCardView", _hand);
            _Set(view, "_gameViewModel", new GameViewModel());
            var handleLifetime = new Disposal();
            var dragLifetime = new Disposal();
            _Get(_hand, "_handleSubscription").GetType().GetProperty("Disposable")
                .SetValue(_Get(_hand, "_handleSubscription"), handleLifetime);
            _Get(_hand, "_dragSubscription").GetType().GetProperty("Disposable")
                .SetValue(_Get(_hand, "_dragSubscription"), dragLifetime);

            // 分別模擬按鈕、SubmitView、手牌 View 或整個戰鬥 View 先被銷毀。
            switch (destroyedTarget)
            {
                case 0: UnityEngine.Object.DestroyImmediate(button); break;
                case 1: UnityEngine.Object.DestroyImmediate(submit); break;
                case 2: UnityEngine.Object.DestroyImmediate(_hand); break;
                case 3: UnityEngine.Object.DestroyImmediate(_root); break;
            }

            Assert.DoesNotThrow(() => view.DisableAllInteraction());
            Assert.DoesNotThrow(() => view.DisableAllInteraction());
            Assert.IsTrue(handleLifetime.IsDisposed);
            Assert.IsTrue(dragLifetime.IsDisposed);
            if (button != null && submit != null)
                Assert.IsFalse(button.interactable);
        }

        [Test]
        public void Release_SkipsSelfAndWrongType_UsesFirstMatchingTarget()
        {
            var wrong = new Target(_CreateRect(), TargetType.AllyCharacter);
            var first = new Target(_CreateRect(), TargetType.EnemyCharacter);
            var second = new Target(_CreateRect(), TargetType.EnemyCharacter);
            _receiver.Targets.AddRange(new ISelectableView[] { _dragCard, wrong, first, second });

            Assert.That(_Release(_Info(SelectType.EnemyCharacter), _center), Is.True);
            var command = _receiver.Commands.Single();
            Assert.That(command.SelectionTarget.ValueOr(default(ISelectionTarget)), Is.SameAs(first));

            _receiver.Commands.Clear();
            _receiver.Targets.Clear();
            _receiver.Targets.Add(_dragCard);
            Assert.That(_Release(_Info(SelectType.AllyCard), _center), Is.False);
            Assert.That(_receiver.Commands, Is.Empty);
        }

        [Test]
        public void Release_RechecksTargetAfterPreview()
        {
            _receiver.Targets.Add(new Target(_CreateRect(), TargetType.EnemyCharacter));
            var info = _Info(SelectType.EnemyCharacter);
            var preview = ((IDragableCardView.DradTargetStatus, Option<ISelectableView>))
                _Call("_GetDragCardStatusAndTargetView", info, _center, _dragCard);
            Assert.That(preview.Item1, Is.EqualTo(IDragableCardView.DradTargetStatus.ValidTarget));

            _receiver.Targets.Clear();

            Assert.That(_Release(info, _center), Is.False);
            Assert.That(_receiver.Commands, Is.Empty);
        }

        [Test]
        public void Release_UsesChangedSelectionType()
        {
            _receiver.Targets.Add(new Target(_CreateRect(), TargetType.EnemyCharacter));
            var info = _Info(SelectType.EnemyCharacter);
            _Call("_GetDragCardStatusAndTargetView", info, _center, _dragCard);
            var changed = info with { MainSelectable = _Info(SelectType.AllyCharacter).MainSelectable };

            Assert.That(_Release(changed, _center), Is.False);
            Assert.That(_receiver.Commands, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Release_WithoutTarget_RequiresBasicArea(bool inside)
        {
            var position = inside ? _center : new Vector2(-1000, -1000);
            Assert.That(_Release(_Info(SelectType.None), position), Is.EqualTo(inside));
            Assert.That(_receiver.Commands.Count, Is.EqualTo(inside ? 1 : 0));
            if (inside)
                Assert.That(_receiver.Commands.Single().SelectionTarget.HasValue, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RemoveCard_AllEntryPointsRecycleAndRemoveIdentity(int entry)
        {
            var factory = _root.AddComponent<CardViewFactory>();
            var poolRoot = _CreateRect();
            typeof(PrefabFactory<CardView>).GetField("_recycleRoot", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(factory, poolRoot);
            _Set(_hand, "_cardViewFactory", factory);
            var identity = Guid.NewGuid();
            var cards = (List<ICardView>)_Get(_hand, "_cardViews");
            var index = (Dictionary<Guid, ICardView>)_Get(_hand, "_cardViewDict");
            cards.Add(_dragCard);
            index.Add(identity, _dragCard);

            if (entry == 0)
                _hand.RemoveCardView(new UsedCardEvent(Faction.Ally, identity, null, CardPlayReason.Active));
            else if (entry == 1)
                _hand.RemoveCardView(new MoveCardEvent(Faction.Ally, identity,
                    CardCollectionType.HandCard, CardCollectionType.Graveyard, null));
            else
                _hand.RecycleHandCards(new DiscardHandCardEvent(Faction.Ally,
                    ImmutableArray.Create(identity), ImmutableArray.Create(identity, Guid.NewGuid()), null));

            Assert.That(_hand.SelectableViews, Is.Empty);
            Assert.That(index, Is.Empty);
            Assert.That(_dragCard.transform.parent, Is.SameAs(poolRoot));
            Assert.That(factory.CreatePrefab(), Is.SameAs(_dragCard));
            // 重複或已失效的移除不可再次回收同一個 View。
            _hand.RemoveCardView(new UsedCardEvent(Faction.Ally, identity, null, CardPlayReason.Active));
            var pool = (Stack<CardView>)typeof(PrefabFactory<CardView>)
                .GetField("_pool", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
            Assert.That(pool, Is.Empty);
        }

        private CardView _CreateCard()
        {
            var rect = _CreateRect();
            var card = rect.gameObject.AddComponent<CardView>();
            _Set(card, "_rectTransform", rect);
            _Set(card, "_canvasGroup", rect.gameObject.AddComponent<CanvasGroup>());
            _Set(card, "_button", rect.gameObject.AddComponent<Button>());
            _Set(card, "_selectedEffectObj", _CreateRect().gameObject);
            return card;
        }

        private RectTransform _CreateRect()
        {
            var rect = new GameObject("測試卡片或目標", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(_root.transform, false);
            rect.sizeDelta = new Vector2(200, 200);
            return rect;
        }

        private static CardInfo _Info(SelectType type) => new CardInfo(
            Guid.NewGuid(), "test", default, default, Array.Empty<CardTheme>(), 0, 0.Some(), 0, 0.Some(),
            new MainSelectionInfo(type, default, default), Array.Empty<CardBuffInfo>(),
            Array.Empty<CardProperty>(), Array.Empty<string>());

        private bool _Release(CardInfo info, Vector2 position) =>
            (bool)_Call("_TryUseCardOnEndDrag", info, position, _dragCard);

        private object _Call(string method, params object[] args) => typeof(AllyHandCardView)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_hand, args);

        private static object _Get(object owner, string field) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

        private static void _Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

        private sealed class Receiver : IGameplayActionReciever
        {
            public readonly List<ISelectableView> Targets = new();
            public readonly List<UseCardCommand> Commands = new();
            public IEnumerable<ISelectableView> SelectableViews => Targets;
            public ISelectableView BasicSelectableView { get; set; }
            public void RecieveEvent(IGameCommand command) => Commands.Add((UseCardCommand)command);
        }

        private sealed class Disposal : IDisposable
        {
            public bool IsDisposed;
            public void Dispose() => IsDisposed = true;
        }

        private sealed class Target : ISelectableView
        {
            public RectTransform RectTransform { get; }
            public TargetType TargetType { get; }
            public Guid TargetIdentity { get; } = Guid.NewGuid();
            public Target(RectTransform rect, TargetType type) { RectTransform = rect; TargetType = type; }
            public void OnSelect() { }
            public void OnDeselect() { }
        }
    }
}
