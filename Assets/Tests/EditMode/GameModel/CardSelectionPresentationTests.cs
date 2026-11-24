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
using TMPro;
using UniRx;

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
                Assert.That(p.Value.Prompt.StepIndex, Is.EqualTo(s.Panel.OpenCount));
                Assert.That(p.Value.Prompt.StepCount, Is.EqualTo(3));
                s.Panel.Click(s.Card.Identity);
                s.Panel.ConfirmButton.onClick.Invoke();
            };
            var result = (CardSelectionPresentationResult.Completed)await _RunSelection(s, draft, CancellationToken.None);
            Assert.That(result.Draft.SelectedGroups.Count, Is.EqualTo(3));
            Assert.That(s.Panel.OpenCount, Is.EqualTo(2));
        });

        [TestCase(CardSelectionCountRule.Optional)]
        [TestCase(CardSelectionCountRule.RequiredExact)]
        public void PanelPrompt_UsesConfiguredKeysAndBothIdsThenUpdatesCount(CardSelectionCountRule rule)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/CardSelectionPanel.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            _objects.Add(instance);
            var panel = instance.GetComponent<CardSelectionPanel>();
            var serializedPanel = new SerializedObject(panel);
            var settings = serializedPanel.FindProperty("_textSettings");
            foreach (var pair in new Dictionary<string, string>
            {
                ["PromptKeyFormat"] = "SelectionPrompt.{0}.{1}",
                ["DefaultPromptKey"] = "SelectionDefault",
                ["StepFormatKey"] = "SelectionStep",
                ["OptionalCountFormatKey"] = "SelectionOptional",
                ["RequiredCountFormatKey"] = "SelectionRequired"
            }) settings.FindPropertyRelative(pair.Key).stringValue = pair.Value;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            var description = (TextMeshProUGUI)serializedPanel
                .FindProperty("_selectionDescriptionText").objectReferenceValue;
            var texts = new Dictionary<string, LocalizeInfoData>
            {
                ["Visible"] = new("收合"),
                ["Confirm"] = new("確認"),
                ["SelectionPrompt.card.first"] = new("選擇要保留的卡片"),
                ["SelectionPrompt.other-card.second"] = new("其他卡片的提示"),
                ["SelectionDefault"] = new("預設選卡提示"),
                ["SelectionStep"] = new("第 {0} 步，共 {1} 步"),
                ["SelectionOptional"] = new("目前 {0} 張，上限 {1} 張"),
                ["SelectionRequired"] = new("目前 {0} 張，必選 {1} 張")
            };
            var library = new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>
                { [LocalizeInfoType.UI] = texts });
            panel.Init(new GameViewModel(), library);
            var prompt = new CardSelectionPromptInfo("card", "first", 0, 2, 0, 1, rule);
            var setup = _Create(rule);
            var cardInfo = setup.Card.ToInfo(setup.Model);
            using var state = new ReactiveProperty<CardSelectionDisplayState>(new(
                Array.AsReadOnly(new[] { cardInfo }), true, false, Array.Empty<Guid>(), prompt));
            panel.Open(state);
            var cardView = instance.GetComponentInChildren<CardView>(true);
            var contentRoot = cardView.transform.parent;
            var selectedEffect = (GameObject)new SerializedObject(cardView)
                .FindProperty("_selectedEffectObj").objectReferenceValue;
            var countText = rule == CardSelectionCountRule.Optional ? "上限" : "必選";
            Assert.That(description.text, Is.EqualTo(
                $"選擇要保留的卡片\n第 1 步，共 2 步\n目前 0 張，{countText} 1 張"));
            Assert.That(panel.ConfirmButton.interactable, Is.False);
            Assert.That(selectedEffect.activeSelf, Is.False);
            state.Value = state.Value with
            {
                IsConfirmable = true,
                SelectedCardIdentities = Array.AsReadOnly(new[] { cardInfo.Identity }),
                Prompt = prompt with { SelectionId = "second", StepIndex = 1, SelectedCount = 1 }
            };
            Assert.That(description.text, Is.EqualTo(
                $"預設選卡提示\n第 2 步，共 2 步\n目前 1 張，{countText} 1 張"));
            Assert.That(panel.ConfirmButton.interactable, Is.True);
            Assert.That(selectedEffect.activeSelf, Is.True);
            Assert.That(instance.GetComponentInChildren<CardView>(true), Is.SameAs(cardView));

            var secondCard = cardInfo with { Identity = Guid.NewGuid() };
            state.Value = state.Value with { Candidates = Array.AsReadOnly(new[] { cardInfo, secondCard }) };
            var secondView = contentRoot.GetComponentsInChildren<CardView>(true)
                .Single(view => view.TargetIdentity == secondCard.Identity);
            state.Value = state.Value with { Candidates = Array.AsReadOnly(new[] { secondCard, cardInfo }) };
            Assert.That(secondView.transform.GetSiblingIndex(), Is.Zero);
            Assert.That(cardView.transform.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(contentRoot.GetComponentsInChildren<CardView>(true).Length, Is.EqualTo(2));

            state.Value = state.Value with
            {
                Candidates = Array.AsReadOnly(new[] { cardInfo with { Cost = Optional.Option.Some(9) } })
            };
            Assert.That(contentRoot.GetComponentsInChildren<CardView>(true), Is.EqualTo(new[] { cardView }));
            Assert.That(secondView.transform.parent, Is.Not.SameAs(contentRoot), "移除的候選卡片應回到回收區");
            var cost = (TextMeshProUGUI)new SerializedObject(cardView).FindProperty("_cost").objectReferenceValue;
            Assert.That(cost.text, Is.EqualTo("9"));
            panel.Close();
        }

        [Test]
        public void PanelBinding_ReopenAndCloseReleaseSubscriptions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/CardSelectionPanel.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            _objects.Add(instance);
            var panel = instance.GetComponent<CardSelectionPanel>();
            panel.Init(new GameViewModel(), new LocalizeLibrary(
                new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()));
            var initial = new CardSelectionDisplayState(Array.Empty<CardInfo>(), true, false, Array.Empty<Guid>(),
                new CardSelectionPromptInfo("card", "first", 0, 1, 0, 1, CardSelectionCountRule.Optional));
            using var first = new TrackedState(initial);
            using var second = new TrackedState(initial with { IsConfirmable = true });
            panel.Open(first);
            Assert.That(first.SubscriptionCount, Is.EqualTo(1));
            Assert.That(panel.ConfirmButton.interactable, Is.False);
            panel.Open(second);
            Assert.That(first.SubscriptionCount, Is.Zero);
            Assert.That(second.SubscriptionCount, Is.EqualTo(1));
            Assert.That(panel.ConfirmButton.interactable, Is.True);
            first.Value = initial with { IsVisible = false };
            Assert.That(panel.ConfirmButton.interactable, Is.True, "舊來源不得覆寫新綁定");

            panel.Close();
            Assert.That(second.SubscriptionCount, Is.Zero);
            second.Value = initial;
            Assert.That(panel.ConfirmButton.interactable, Is.True, "關窗後不得繼續更新元件");
            panel.Open(second);
            Assert.That(second.SubscriptionCount, Is.EqualTo(1));
            Assert.That(panel.ConfirmButton.interactable, Is.False, "重開立即套用來源目前值");
            panel.Close();
            Assert.That(second.SubscriptionCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PromptInfo_PreservesSelectionContextAndUpdatesCount() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            s.Panel.OnReady = p =>
            {
                var initialState = p.Value;
                Assert.That(initialState.Prompt, Is.EqualTo(new CardSelectionPromptInfo(
                    s.Data.ID, "first", 0, 1, 0, 1, CardSelectionCountRule.RequiredExact)));
                Assert.That(initialState.Candidates.Single().Identity, Is.EqualTo(s.Card.Identity));
                s.Panel.Click(Guid.NewGuid());
                Assert.That(s.Panel.Update, Is.Null, "非候選身份不得加入選取");
                s.Panel.Click(s.Card.Identity);
                Assert.That(s.Panel.Update.Prompt, Is.EqualTo(initialState.Prompt with { SelectedCount = 1 }));
                Assert.That(s.Panel.Update.IsConfirmable, Is.True);
                Assert.That(s.Panel.Update.SelectedCardIdentities, Is.EqualTo(new[] { s.Card.Identity }));
                Assert.That(initialState.SelectedCardIdentities, Is.Empty, "舊快照不得隨後續選取改變");
                Assert.That(initialState.IsConfirmable, Is.False);
                s.Panel.ConfirmButton.onClick.Invoke();
            };
            Assert.That(await _RunSelection(s, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Completed>());
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
                    s.Panel.Click(s.Card.Identity);
                    s.Panel.ConfirmButton.onClick.Invoke();
                    s.Panel.CloseButtons[0].onClick.Invoke();
                }
                else s.Panel.CloseButtons[0].onClick.Invoke();
            };
            Assert.That(await _RunSelection(s, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Cancelled>());
            Assert.That(s.Draft.SelectedGroups, Is.Empty);
            s.Panel.OnReady = p =>
            {
                Assert.That(p.Value.Prompt.StepIndex, Is.Zero);
                Assert.That(p.Value.Prompt.StepCount, Is.EqualTo(2));
                s.Panel.CloseButtons[0].onClick.Invoke();
            };
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
            s.Panel.Click(s.Card.Identity);
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
                Assert.That(p.Value.Prompt.StepIndex, Is.EqualTo(s.Panel.OpenCount - 1));
                Assert.That(p.Value.Prompt.StepCount, Is.EqualTo(2));
                Assert.That(p.Value.IsConfirmable, Is.False);
                s.Panel.ConfirmButton.onClick.Invoke();
                s.Panel.Click(s.Card.Identity);
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
            s.Panel.OnReady = p => { Assert.That(p.Value.IsConfirmable, Is.True); s.Panel.ConfirmButton.onClick.Invoke(); };
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
            cts.Cancel();
            var cancelled = false;
            try { await task; } catch (OperationCanceledException) { cancelled = true; }
            Assert.That(cancelled, Is.True);
            Assert.That(s.Panel.CloseCount, Is.EqualTo(1));
            s.Panel.Click(s.Card.Identity);
            s.Panel.ConfirmButton.onClick.Invoke();
            s.Panel.VisibleToggleButton.onClick.Invoke();
            Assert.That(s.Panel.Update, Is.Null);
        });

        [UnityTest]
        public IEnumerator DetailCancellation_ClosesBothPanels() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            using var cts = new CancellationTokenSource();
            s.Panel.OnReady = _ => s.Panel.LongPress(s.Card.Identity);
            s.Detail.OnRun = () =>
            {
                s.Panel.Click(s.Card.Identity);
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
        private sealed class TrackedState : IReadOnlyReactiveProperty<CardSelectionDisplayState>, IDisposable
        {
            private readonly ReactiveProperty<CardSelectionDisplayState> _source;
            public int SubscriptionCount { get; private set; }
            public bool HasValue => _source.HasValue;
            public CardSelectionDisplayState Value { get => _source.Value; set => _source.Value = value; }
            public TrackedState(CardSelectionDisplayState value) => _source = new(value);
            public IDisposable Subscribe(IObserver<CardSelectionDisplayState> observer)
            {
                var subscription = _source.Subscribe(observer);
                SubscriptionCount++;
                return Disposable.Create(() =>
                {
                    subscription.Dispose();
                    SubscriptionCount--;
                });
            }
            public void Dispose() => _source.Dispose();
        }
        private sealed class Panel : ICardSelectionPanel
        {
            public Button[] CloseButtons { get; }
            public Button ConfirmButton { get; }
            public Button VisibleToggleButton { get; }
            public Action<IReadOnlyReactiveProperty<CardSelectionDisplayState>> OnOpen;
            public Action<IReadOnlyReactiveProperty<CardSelectionDisplayState>> OnReady;
            private IReadOnlyReactiveProperty<CardSelectionDisplayState> _pendingResponse;
            private readonly Subject<Guid> _cardClicked = new();
            private readonly Subject<Guid> _cardLongPressed = new();
            public IObservable<Guid> CardClicked => _cardClicked;
            public IObservable<Guid> CardLongPressed => _cardLongPressed;
            public void Click(Guid identity) => _cardClicked.OnNext(identity);
            public void LongPress(Guid identity) => _cardLongPressed.OnNext(identity);
            public CardSelectionDisplayState Update;
            private IDisposable _stateSubscription;
            public int OpenCount, CloseCount, UpdateCount;
            public Panel(Button[] closeButtons, Button confirmButton, Button visibleToggleButton)
            {
                CloseButtons = closeButtons;
                ConfirmButton = confirmButton;
                VisibleToggleButton = visibleToggleButton;
            }
            public void Init(IGameViewModel model, LocalizeLibrary library) { }
            public void Open(IReadOnlyReactiveProperty<CardSelectionDisplayState> state)
            {
                _stateSubscription?.Dispose();
                _stateSubscription = state.Skip(1).Subscribe(value =>
                {
                    Update = value;
                    UpdateCount++;
                });
                _pendingResponse = state;
                OpenCount++;
                OnOpen?.Invoke(state);
            }
            public void Respond()
            {
                if (_pendingResponse == null) return;
                var property = _pendingResponse;
                _pendingResponse = null;
                OnReady?.Invoke(property);
            }
            public void Close()
            {
                _stateSubscription?.Dispose();
                _stateSubscription = null;
                CloseCount++;
            }
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
