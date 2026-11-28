using System;
using MortalGame.Exceptions;
using System.Collections.Generic;
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

namespace MortalGame.Tests
{
    public sealed class FocusCardDetailPresentationTests
    {
        private GameObject _root;
        private FocusCardDetailView _view;
        private GameObject _popup;
        private CardView _card;
        private RectTransform _target;
        private GameViewModel _model;
        private CardInfo _info;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("懸停詳情測試", typeof(RectTransform), typeof(Canvas));
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/FocusCardDetailView.prefab"), _root.transform);
            _view = instance.GetComponentInChildren<FocusCardDetailView>(true);
            var serialized = new SerializedObject(_view);
            _popup = (GameObject)serialized.FindProperty("_panel").objectReferenceValue;
            _card = (CardView)serialized.FindProperty("_cardView").objectReferenceValue;
            _popup.SetActive(false);
            _target = new GameObject("懸停卡片位置", typeof(RectTransform)).GetComponent<RectTransform>();
            _target.SetParent(_root.transform, false);
            _target.anchoredPosition = new Vector2(120, -100);
            _target.sizeDelta = new Vector2(200, 300);
            _model = new GameViewModel();
            _view.Init(_model, new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()));
            var built = new GameplayManagerTestBuilder().WithCard(CardTestBuilder.CreateCardData()).Build();
            var entity = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(entity);
            _info = entity.ToInfo(built.Manager) with { Keywords = Array.Empty<string>() };
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_root);

        [Test]
        public void State_UpdatesCardAndHintsAndPositionsWithoutObservingModel()
        {
            using var state = new ReactiveProperty<FocusCardDetailDisplayState>(new(_info with { Cost = Option.Some(3) }));
            using var session = _view.Open(state, _target);
            var content = (RectTransform)new SerializedObject(_view).FindProperty("_content").objectReferenceValue;
            Assert.That(_popup.activeSelf, Is.True);
            Assert.That(content.anchoredPosition.x, Is.EqualTo(_target.anchoredPosition.x).Within(0.01f));
            _model.UpdateCardInfo(_info with { Cost = Option.Some(99) });
            Assert.That(_Cost().text, Is.EqualTo("3"), "Focus 只能呈現傳入 State");
            state.Value = new(_info with { Cost = Option.Some(6), Keywords = new[] { "focus-keyword" } });
            Assert.That(_Cost().text, Is.EqualTo("6"));
            Assert.That(_HintContent().activeSelf, Is.True);
            state.Value = new(_info with { Cost = Option.Some(4) });
            Assert.That(_HintContent().activeSelf, Is.False);
            Assert.That(_Cost().text, Is.EqualTo("4"));
        }

        [Test]
        public void Dispose_ReleasesBindingAndOldSessionCannotCloseNewFocus()
        {
            using var state = new ReactiveProperty<FocusCardDetailDisplayState>(new(_info with { Keywords = new[] { "focus-keyword" } }));
            using var first = _view.Open(state, _target);
            var error = Assert.Throws<SessionAlreadyOpenException>(() => _view.Open(state, _target));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(FocusCardDetailView)));
            first.Dispose();
            first.Dispose();
            Assert.That(_popup.activeSelf, Is.False);
            Assert.That(_HintContent().activeSelf, Is.False);
            Assert.That(_card.TargetIdentity, Is.EqualTo(Guid.Empty));
            state.Value = new(_info with { Cost = Option.Some(77) });
            Assert.That(_card.TargetIdentity, Is.EqualTo(Guid.Empty));
            using var nextState = new ReactiveProperty<FocusCardDetailDisplayState>(new(_info with { Identity = Guid.NewGuid(), Cost = Option.Some(5) }));
            using var second = _view.Open(nextState, _target);
            first.Dispose();
            state.Value = new(_info with { Cost = Option.Some(88) });
            Assert.That(_popup.activeSelf, Is.True);
            Assert.That(_card.TargetIdentity, Is.EqualTo(nextState.Value.Card.Identity));
            Assert.That(_Cost().text, Is.EqualTo("5"));
        }

        [Test]
        public void Reopen_PreservesAuthoredCardPositionAndKeepsCardInsideContent()
        {
            using var state = new ReactiveProperty<FocusCardDetailDisplayState>(new(_info));
            var content = (RectTransform)new SerializedObject(_view).FindProperty("_content").objectReferenceValue;
            var originalPosition = _card.RectTransform.localPosition;
            var originalAnchored = _card.RectTransform.anchoredPosition;
            Assert.That(originalPosition.y, Is.GreaterThan(0), "實際 prefab 的卡面中心位於底部原點上方");
            for (var i = 0; i < 3; i++)
            {
                using (var session = _view.Open(state, _target))
                {
                    Assert.That(_card.RectTransform.localPosition, Is.EqualTo(originalPosition));
                    Assert.That(_card.RectTransform.anchoredPosition, Is.EqualTo(originalAnchored));
                    // 檢查卡面本體；子物件的裝飾邊框可超出卡面 5 個單位。
                    var corners = new Vector3[4];
                    _card.RectTransform.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var local = content.InverseTransformPoint(corner);
                        Assert.That(local.y, Is.GreaterThanOrEqualTo(content.rect.yMin - 0.01f));
                        Assert.That(local.y, Is.LessThanOrEqualTo(content.rect.yMax + 0.01f));
                    }
                }
                Assert.That(_card.RectTransform.localPosition, Is.EqualTo(originalPosition));
                Assert.That(_card.TargetIdentity, Is.EqualTo(Guid.Empty), "保留版面也必須完成卡片清理");
            }
        }

        [Test]
        public void FailedOpen_CleansPartialWindowAndAllowsReopen()
        {
            using var state = new ReactiveProperty<FocusCardDetailDisplayState>(null);
            Assert.Throws<NullReferenceException>(() => _view.Open(state, _target));
            Assert.That(_popup.activeSelf, Is.False);
            Assert.That(_card.TargetIdentity, Is.EqualTo(Guid.Empty));
            state.Value = new(_info);
            using var session = _view.Open(state, _target);
            Assert.That(_popup.activeSelf, Is.True);
        }

        private TMP_Text _Cost() => (TMP_Text)new SerializedObject(_card).FindProperty("_cost").objectReferenceValue;
        private GameObject _HintContent()
        {
            var hint = new SerializedObject(_view).FindProperty("_cardKeywordHint").objectReferenceValue;
            return ((RectTransform)new SerializedObject(hint).FindProperty("_content").objectReferenceValue).gameObject;
        }
    }
}
