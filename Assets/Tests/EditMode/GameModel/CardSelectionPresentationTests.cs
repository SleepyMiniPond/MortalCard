using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using MortalGame.Presenter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class CardSelectionPresentationTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects) UnityEngine.Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator VisibilityToggle_RerunHandlesEachClickOnceAndStopsAfterClose() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.Optional);
            s.Panel.OnReady = _ =>
            {
                var previousUpdates = s.Panel.UpdateCount;
                s.Panel.VisibleToggleButton.onClick.Invoke();
                Assert.That(s.Panel.Update.IsVisible, Is.False);
                Assert.That(s.Panel.UpdateCount, Is.EqualTo(previousUpdates + 1));
                s.Panel.VisibleToggleButton.onClick.Invoke();
                Assert.That(s.Panel.Update.IsVisible, Is.True);
                Assert.That(s.Panel.UpdateCount, Is.EqualTo(previousUpdates + 2));
                s.Panel.ConfirmButton.onClick.Invoke();
            };
            Assert.That(await _RunSelection(s, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Completed>());
            s.Panel.VisibleToggleButton.onClick.Invoke();
            Assert.That(s.Panel.UpdateCount, Is.EqualTo(2));
            Assert.That(await _RunSelection(s, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Completed>());
            s.Panel.VisibleToggleButton.onClick.Invoke();
            Assert.That(s.Panel.UpdateCount, Is.EqualTo(4));
            Assert.That(s.Panel.CloseCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator ConfirmedPrefix_StartsAtRemainingStepAndCompletesConfiguredCount() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            foreach (var id in new[] { "second", "third" })
                s.Data.SubSelects.Add(new ExistCardSelectionGroup
                {
                    Id = id, CardCandidates = new SubSelectedCardCollection { SelectionId = "first" },
                    SelectCount = new ConstInteger { Value = 1 }, CountRule = CardSelectionCountRule.RequiredExact
                });
            var draft = s.Draft.WithSelection("first", new ExistCardSubSelectionAction(new[] { s.Card.Identity }));
            s.Panel.OnReady = p =>
            {
                Assert.That(p.Description, Does.Contain($"步驟 {s.Panel.OpenCount + 1}/3"));
                p.Selections.Single().OnClick(s.Card.Identity, null);
                s.Panel.ConfirmButton.onClick.Invoke();
            };
            var result = (CardSelectionPresentationResult.Completed)await _RunSelection(s, draft, CancellationToken.None);
            Assert.That(result.Draft.SelectedGroups.Count, Is.EqualTo(3));
            Assert.That(s.Panel.OpenCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator Prompt_UsesBothIdsAndFallsBackForOtherGroups() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.Optional);
            s.Texts[$"CardSelection.{s.Data.ID}.first"] = new("選擇要保留的卡片");
            s.Texts["CardSelection.other-card.second"] = new("其他卡片的提示");
            s.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", CardCandidates = new FixedCards(s.Card),
                SelectCount = new ConstInteger { Value = 1 }, CountRule = CardSelectionCountRule.Optional
            });
            s.Panel.OnReady = p =>
            {
                Assert.That(p.Description.Split('\n')[0], Is.EqualTo(
                    s.Panel.OpenCount == 1 ? "選擇要保留的卡片" : "選擇卡片"));
                s.Panel.ConfirmButton.onClick.Invoke();
            };
            Assert.That(await _RunSelection(s, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Completed>());
            Assert.That(s.Panel.OpenCount, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator LaterCancel_DiscardsPrefixAndNextAttemptStartsFresh() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            s.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", CardCandidates = new SubSelectedCardCollection { SelectionId = "first" },
                SelectCount = new ConstInteger { Value = 1 }, CountRule = CardSelectionCountRule.RequiredExact
            });
            s.Panel.OnReady = p =>
            {
                if (s.Panel.OpenCount == 1)
                {
                    p.Selections.Single().OnClick(s.Card.Identity, null);
                    s.Panel.ConfirmButton.onClick.Invoke();
                    s.Panel.CloseButtons[0].onClick.Invoke();
                }
                else s.Panel.CloseButtons[0].onClick.Invoke();
            };
            Assert.That(await _RunSelection(s, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Cancelled>());
            Assert.That(s.Draft.SelectedGroups, Is.Empty);
            s.Panel.OnReady = p => { Assert.That(p.Description, Does.Contain("步驟 1/2")); s.Panel.CloseButtons[0].onClick.Invoke(); };
            Assert.That(await _RunSelection(s, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Cancelled>());
            Assert.That(s.Model.PopAllEvents(), Is.Empty);
        });

        [UnityTest]
        public IEnumerator OpenFailure_ClosesPanelAndInvalidatesCallbacks() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.Optional);
            s.Panel.OnOpen = _ => throw new InvalidOperationException("測試面板開啟失敗");
            var failed = false;
            try { await _RunSelection(s, s.Draft, CancellationToken.None); }
            catch (InvalidOperationException) { failed = true; }
            Assert.That(failed, Is.True);
            Assert.That(s.Panel.CloseCount, Is.EqualTo(1));
            s.Panel.Property.Selections.Single().OnClick(s.Card.Identity, null);
            Assert.That(s.Panel.Update, Is.Null);
        });

        [UnityTest]
        public IEnumerator RequiredClose_CancelsWholeDraftAndLeavesModelUntouched() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            s.Panel.OnReady = p => s.Panel.CloseButtons[1].onClick.Invoke();
            var result = await _RunSelection(s, s.Draft, CancellationToken.None);
            Assert.That(result, Is.TypeOf<CardSelectionPresentationResult.Cancelled>());
            Assert.That(s.Panel.CloseCount, Is.EqualTo(1));
            Assert.That(s.Model.GetCard(s.Card.Identity).HasValue, Is.True);
            Assert.That(s.Model.PopAllEvents(), Is.Empty);
            Assert.That(((IGameplayModel)s.Model).ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        });

        [UnityTest]
        public IEnumerator SequentialConfirm_QueriesSecondStepWithConfirmedPrefix() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            s.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", CardCandidates = new SubSelectedCardCollection { SelectionId = "first" },
                SelectCount = new ConstInteger { Value = 1 }, CountRule = CardSelectionCountRule.RequiredExact
            });
            s.Panel.OnReady = p =>
            {
                Assert.That(((IGameplayModel)s.Model).ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
                Assert.That(p.Description, Does.Contain($"步驟 {s.Panel.OpenCount}/2"));
                Assert.That(p.IsConfirmable, Is.False);
                s.Panel.ConfirmButton.onClick.Invoke();
                p.Selections.Single().OnClick(s.Card.Identity, null);
                Assert.That(s.Panel.Update.IsConfirmable, Is.True);
                s.Panel.ConfirmButton.onClick.Invoke();
            };
            var result = (CardSelectionPresentationResult.Completed)await _RunSelection(s, s.Draft, CancellationToken.None);
            Assert.That(((ExistCardSubSelectionAction)result.Draft.SelectedGroups["second"]).CardIdentity.ToArray(),
                Is.EqualTo(((ExistCardSubSelectionAction)result.Draft.SelectedGroups["first"]).CardIdentity.ToArray()));
            Assert.That(s.Panel.OpenCount, Is.EqualTo(2));
            Assert.That(s.Panel.CloseCount, Is.EqualTo(2));
            Assert.That(s.Model.PopAllEvents(), Is.Empty);
        });

        [UnityTest]
        public IEnumerator OptionalZeroConfirm_IsCompletedRatherThanCancelled() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.Optional);
            s.Panel.OnReady = p => { Assert.That(p.IsConfirmable, Is.True); s.Panel.ConfirmButton.onClick.Invoke(); };
            var result = (CardSelectionPresentationResult.Completed)await _RunSelection(s, s.Draft, CancellationToken.None);
            Assert.That(result.Draft.SelectedGroups.ContainsKey("first"), Is.True);
            Assert.That(((ExistCardSubSelectionAction)result.Draft.SelectedGroups["first"]).CardIdentity, Is.Empty);
        });

        [UnityTest]
        public IEnumerator ZeroMaximum_SkipsPanelAndRecordsEmptyStep() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            ((ExistCardSelectionGroup)s.Data.SubSelects[0]).SelectCount = new ConstInteger { Value = 0 };
            var result = (CardSelectionPresentationResult.Completed)await _RunSelection(s, s.Draft, CancellationToken.None);
            Assert.That(s.Panel.OpenCount, Is.Zero);
            Assert.That(((ExistCardSubSelectionAction)result.Draft.SelectedGroups["first"]).CardIdentity, Is.Empty);
        });

        [UnityTest]
        public IEnumerator LaterShortage_RejectsWholeOperation() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.Optional);
            s.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", CardCandidates = new SubSelectedCardCollection { SelectionId = "first" },
                SelectCount = new ConstInteger { Value = 1 }, CountRule = CardSelectionCountRule.RequiredExact
            });
            s.Panel.OnReady = p => s.Panel.ConfirmButton.onClick.Invoke();
            var result = (CardSelectionPresentationResult.Rejected)await _RunSelection(s, s.Draft, CancellationToken.None);
            Assert.That(result.Failure.Reason, Is.EqualTo(CardSelectionFailureReason.InsufficientCandidates));
            Assert.That(s.Panel.OpenCount, Is.EqualTo(1));
            Assert.That(s.Model.PopAllEvents(), Is.Empty);
        });

        [UnityTest]
        public IEnumerator CancellationDuringWait_ClosesPanelAndInvalidatesCallbacks() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            using var cts = new CancellationTokenSource();
            var task = s.Flow.RunSubSelection(s.Model, s.Draft, cts.Token);
            var previous = s.Panel.Property;
            cts.Cancel();
            var cancelled = false;
            try { await task; } catch (OperationCanceledException) { cancelled = true; }
            Assert.That(cancelled, Is.True);
            Assert.That(s.Panel.CloseCount, Is.EqualTo(1));
            previous.Selections.Single().OnClick(s.Card.Identity, null);
            s.Panel.ConfirmButton.onClick.Invoke();
            s.Panel.VisibleToggleButton.onClick.Invoke();
            Assert.That(s.Panel.Update, Is.Null);
        });

        [UnityTest]
        public IEnumerator DetailCancellation_ClosesBothPanels() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            using var cts = new CancellationTokenSource();
            s.Panel.OnReady = p => p.Selections.Single().OnLongPress(s.Card.Identity, null);
            s.Detail.OnRun = () =>
            {
                s.Panel.Property.Selections.Single().OnClick(s.Card.Identity, null);
                s.Panel.ConfirmButton.onClick.Invoke();
                s.Panel.CloseButtons[0].onClick.Invoke();
                s.Panel.VisibleToggleButton.onClick.Invoke();
                Assert.That(s.Panel.Update, Is.Null, "詳情開啟期間不得變更選取");
                cts.Cancel();
            };
            var cancelled = false;
            try { await _RunSelection(s, s.Draft, cts.Token); }
            catch (OperationCanceledException) { cancelled = true; }
            Assert.That(cancelled, Is.True);
            Assert.That(s.Detail.Closed, Is.True);
            Assert.That(s.Panel.CloseCount, Is.EqualTo(1));
        });

        [Test]
        public void AutomaticSelection_UsesPreviousGroupForSecondStep()
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            s.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", CardCandidates = new SubSelectedCardCollection { SelectionId = "first" },
                SelectCount = new ConstInteger { Value = 1 }, CountRule = CardSelectionCountRule.RequiredExact
            });
            Assert.That(SelectTargetLogic.SelectTargets(s.Model, s.Card,
                ((IGameplayModel)s.Model).GameStatus.Ally).TryGetValue(out var result), Is.True);
            var first = (ExistCardSubSelectionAction)result.SubSelectionActions["first"];
            var second = (ExistCardSubSelectionAction)result.SubSelectionActions["second"];
            Assert.That(second.CardIdentity.ToArray(), Is.EqualTo(first.CardIdentity.ToArray()));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        public void AutomaticEntrypoints_UseSequentialSelectionOrSkipWholeAttempt(int entry, bool shortage)
        {
            var data = CardTestBuilder.CreateCardData("two-step");
            var targetData = CardTestBuilder.CreateCardData("target");
            var built = new GameplayManagerTestBuilder().WithCard(data).WithCard(targetData).Build();
            var owner = entry == 1 ? (IPlayerEntity)built.Ally : built.Enemy;
            var targetOwner = entry == 2 ? built.Ally : owner;
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, data.ID);
            var target = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, targetData.ID);
            owner.CardManager.HandCard.AddCard(card);
            targetOwner.CardManager.HandCard.AddCard(target);
            data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "first", CardCandidates = new FixedCards(target),
                SelectCount = new ConstInteger { Value = 1 }, CountRule = CardSelectionCountRule.RequiredExact
            });
            data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", CardCandidates = new SubSelectedCardCollection { SelectionId = "first" },
                SelectCount = new ConstInteger { Value = shortage ? 2 : 1 }, CountRule = CardSelectionCountRule.RequiredExact
            });
            data.Effects.Add(new DiscardCardEffect { TargetCards = new SubSelectedCardCollection { SelectionId = "second" } });
            IReadOnlyCollection<IGameEvent> events;
            if (entry == 0)
            {
                built.Enemy.SelectedCards.TryAddCard(card);
                typeof(GameplayManager).GetField("_gameActions", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(built.Manager, new UniTaskAwaitableQueue<IGameAction>());
                typeof(GameplayManager).GetMethod("_EnemyExecute", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(built.Manager, Array.Empty<object>());
                events = built.Manager.PopAllEvents();
            }
            else
            {
                var context = new TriggerContext(built.Manager, new PlayerTrigger(built.Ally),
                    new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));
                ICardEffect effect = entry == 1
                    ? new PlayCardEffect { TargetCards = new FixedCards(card) }
                    : new InvokeCardEffects { TargetCards = new FixedCards(card) };
                events = built.Manager.RunEffectBatch(new[] { new CardEffectQueueItem(context, effect) }).Events;
            }
            Assert.That(targetOwner.CardManager.Graveyard.Cards.Contains(target), Is.EqualTo(!shortage));
            Assert.That(owner.CardManager.HandCard.Cards.Contains(card), Is.EqualTo(shortage || entry == 2));
            Assert.That(events.OfType<UsedCardEvent>().Count(), Is.EqualTo(!shortage && entry != 2 ? 1 : 0));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        private static async UniTask<CardSelectionPresentationResult> _RunSelection(
            Setup setup, CardSelectionDraft draft, CancellationToken token)
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            var run = setup.Flow.RunSubSelection(setup.Model, draft, lifetime.Token).Preserve();
            try
            {
                for (var iteration = 0; iteration < 100 && run.Status == UniTaskStatus.Pending; iteration++)
                {
                    setup.Panel.Respond();
                    // EditMode 主動推進 PlayerLoop，讓正式流程的 NextFrame 能完成。
                    EditorApplication.QueuePlayerLoopUpdate();
                    await UniTask.Yield();
                }
                Assert.That(run.Status, Is.Not.EqualTo(UniTaskStatus.Pending), "選取流程應在期限內完成");
                return await run;
            }
            finally
            {
                lifetime.Cancel();
                if (run.Status == UniTaskStatus.Pending) await run.SuppressCancellationThrow();
            }
        }

        private Button _CreateButton(string name)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Button));
            _objects.Add(obj);
            return obj.GetComponent<Button>();
        }

        private Setup _Create(CardSelectionCountRule rule)
        {
            var data = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "first", SelectCount = new ConstInteger { Value = 1 },
                CardCandidates = new FixedCards(card), CountRule = rule
            });
            var panel = new Panel(new[] { _CreateButton("Close"), _CreateButton("Cancel") },
                _CreateButton("Confirm"), _CreateButton("VisibleToggle"));
            var detail = new Detail();
            var texts = new Dictionary<string, LocalizeInfoData>
            {
                ["CardSelection_Default"] = new("選擇卡片"),
                ["CardSelection_Step"] = new("步驟 {0}/{1}"),
                ["CardSelection_Optional"] = new("已選 {0}/{1} 張，最多選 {1} 張"),
                ["CardSelection_Required"] = new("已選 {0}/{1} 張，必須選滿"),
                ["CardSelection_Rejected"] = new("請重新操作"),
                ["CardSelection_InsufficientCandidates"] = new("候選不足")
            };
            var library = new LocalizeLibrary(new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>
                {
                    [LocalizeInfoType.UI] = texts
                });
            return new Setup(built.Manager, data, card, CardSelectionDraft.Create(card, built.Ally, MainSelectionAction.Empty),
                new CardSelectionFlow(new SubSelectionPresenter(null, library, detail, panel)), panel, detail, texts);
        }

        private record Setup(GameplayManager Model, StandardCardData Data, ICardEntity Card,
            CardSelectionDraft Draft, CardSelectionFlow Flow, Panel Panel, Detail Detail,
            Dictionary<string, LocalizeInfoData> Texts);
        private sealed class FixedCards : ITargetCardCollectionValue
        {
            private readonly ICardEntity _card;
            public FixedCards(ICardEntity card) => _card = card;
            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext context) => new[] { _card };
        }
        private sealed class Panel : ICardSelectionPanel
        {
            public Button[] CloseButtons { get; }
            public Button ConfirmButton { get; }
            public Button VisibleToggleButton { get; }
            public Action<ICardSelectionPanel.Property> OnOpen;
            public Action<ICardSelectionPanel.Property> OnReady;
            private ICardSelectionPanel.Property _pendingResponse;
            public ICardSelectionPanel.Property Property;
            public ICardSelectionPanel.UpdateProperty Update;
            public int OpenCount, CloseCount, UpdateCount;
            public Panel(Button[] closeButtons, Button confirmButton, Button visibleToggleButton)
            {
                CloseButtons = closeButtons;
                ConfirmButton = confirmButton;
                VisibleToggleButton = visibleToggleButton;
            }
            public void Init(IGameViewModel model, LocalizeLibrary library) { }
            public void Open(ICardSelectionPanel.Property property)
            {
                Property = property with { Selections = property.Selections.ToArray() };
                _pendingResponse = Property;
                OpenCount++;
                OnOpen?.Invoke(Property);
            }
            public void Respond()
            {
                if (_pendingResponse == null) return;
                var property = _pendingResponse;
                _pendingResponse = null;
                OnReady?.Invoke(property);
            }
            public void RenderUpdate(ICardSelectionPanel.UpdateProperty property)
            {
                Update = property;
                UpdateCount++;
            }
            public void Close() => CloseCount++;
        }
        private sealed class Detail : ISingleCardDetailPopupPanel
        {
            public Action OnRun;
            public bool Closed;
            public void Init(IGameViewModel model, LocalizeLibrary library) { }
            public async UniTask Run(CardDetailProperty property, CancellationToken token)
            {
                try { OnRun?.Invoke(); await UniTask.NextFrame(token); }
                finally { Closed = true; }
            }
        }
    }
}
