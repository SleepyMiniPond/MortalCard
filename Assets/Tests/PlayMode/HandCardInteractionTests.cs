using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
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
