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
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class CardSelectionPresentationTests
    {
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
            s.Panel.OnOpen = p =>
            {
                Assert.That(p.Description, Does.Contain($"步驟 {s.Panel.OpenCount + 1}/3"));
                p.Selections.Single().OnClick(s.Card.Identity, null);
                p.OnConfirm();
            };
            var result = (CardSelectionPresentationResult.Completed)await s.Presenter.RunSubSelection(s.Model, draft, CancellationToken.None);
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
            s.Panel.OnOpen = p =>
            {
                Assert.That(p.Description.Split('\n')[0], Is.EqualTo(
                    s.Panel.OpenCount == 1 ? "選擇要保留的卡片" : "選擇卡片"));
                p.OnConfirm();
            };
            Assert.That(await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None),
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
            s.Panel.OnOpen = p =>
            {
                if (s.Panel.OpenCount == 1)
                {
                    p.Selections.Single().OnClick(s.Card.Identity, null);
                    p.OnConfirm();
                    p.OnClose();
                }
                else p.OnClose();
            };
            Assert.That(await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Cancelled>());
            Assert.That(s.Draft.SelectedGroups, Is.Empty);
            s.Panel.OnOpen = p => { Assert.That(p.Description, Does.Contain("步驟 1/2")); p.OnClose(); };
            Assert.That(await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None),
                Is.TypeOf<CardSelectionPresentationResult.Cancelled>());
            Assert.That(s.Model.PopAllEvents(), Is.Empty);
        });

        [UnityTest]
        public IEnumerator OpenFailure_ClosesPanelAndInvalidatesCallbacks() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.Optional);
            s.Panel.OnOpen = _ => throw new InvalidOperationException("測試面板開啟失敗");
            var failed = false;
            try { await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None); }
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
            s.Panel.OnOpen = p => p.OnClose();
            var result = await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None);
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
            s.Panel.OnOpen = p =>
            {
                Assert.That(((IGameplayModel)s.Model).ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
                Assert.That(p.Description, Does.Contain($"步驟 {s.Panel.OpenCount}/2"));
                Assert.That(p.IsConfirmable, Is.False);
                p.OnConfirm();
                p.Selections.Single().OnClick(s.Card.Identity, null);
                Assert.That(s.Panel.Update.IsConfirmable, Is.True);
                p.OnConfirm();
            };
            var result = (CardSelectionPresentationResult.Completed)await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None);
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
            s.Panel.OnOpen = p => { Assert.That(p.IsConfirmable, Is.True); p.OnConfirm(); };
            var result = (CardSelectionPresentationResult.Completed)await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None);
            Assert.That(result.Draft.SelectedGroups.ContainsKey("first"), Is.True);
            Assert.That(((ExistCardSubSelectionAction)result.Draft.SelectedGroups["first"]).CardIdentity, Is.Empty);
        });

        [UnityTest]
        public IEnumerator ZeroMaximum_SkipsPanelAndRecordsEmptyStep() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            ((ExistCardSelectionGroup)s.Data.SubSelects[0]).SelectCount = new ConstInteger { Value = 0 };
            var result = (CardSelectionPresentationResult.Completed)await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None);
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
            s.Panel.OnOpen = p => p.OnConfirm();
            var result = (CardSelectionPresentationResult.Rejected)await s.Presenter.RunSubSelection(s.Model, s.Draft, CancellationToken.None);
            Assert.That(result.Failure.Reason, Is.EqualTo(CardSelectionFailureReason.InsufficientCandidates));
            Assert.That(s.Panel.OpenCount, Is.EqualTo(1));
            Assert.That(s.Model.PopAllEvents(), Is.Empty);
        });

        [UnityTest]
        public IEnumerator CancellationDuringWait_ClosesPanelAndInvalidatesCallbacks() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            using var cts = new CancellationTokenSource();
            var task = s.Presenter.RunSubSelection(s.Model, s.Draft, cts.Token);
            var previous = s.Panel.Property;
            cts.Cancel();
            var cancelled = false;
            try { await task; } catch (OperationCanceledException) { cancelled = true; }
            Assert.That(cancelled, Is.True);
            Assert.That(s.Panel.CloseCount, Is.EqualTo(1));
            previous.Selections.Single().OnClick(s.Card.Identity, null);
            previous.OnConfirm();
            Assert.That(s.Panel.Update, Is.Null);
        });

        [UnityTest]
        public IEnumerator DetailCancellation_ClosesBothPanels() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.RequiredExact);
            using var cts = new CancellationTokenSource();
            s.Panel.OnOpen = p => p.Selections.Single().OnLongPress(s.Card.Identity, null);
            s.Detail.OnRun = () =>
            {
                s.Panel.Property.Selections.Single().OnClick(s.Card.Identity, null);
                s.Panel.Property.OnConfirm();
                s.Panel.Property.OnClose();
                Assert.That(s.Panel.Update, Is.Null, "詳情開啟期間不得變更選取");
                cts.Cancel();
            };
            var cancelled = false;
            try { await s.Presenter.RunSubSelection(s.Model, s.Draft, cts.Token); }
            catch (OperationCanceledException) { cancelled = true; }
            Assert.That(cancelled, Is.True);
            Assert.That(s.Detail.Closed, Is.True);
            Assert.That(s.Panel.CloseCount, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator FailureNotice_RequiresCloseAndDisablesConfirm() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create(CardSelectionCountRule.Optional);
            s.Panel.OnOpen = p =>
            {
                Assert.That(p.Selections, Is.Empty);
                Assert.That(p.IsConfirmable, Is.False);
                Assert.That(p.CanToggleVisibility, Is.False);
                p.OnClose();
            };
            await s.Presenter.ShowFailure(CancellationToken.None);
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

        private static Setup _Create(CardSelectionCountRule rule)
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
            var panel = new Panel();
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
                new SubSelectionPresenter(null, library, detail, panel), panel, detail, texts);
        }

        private record Setup(GameplayManager Model, StandardCardData Data, ICardEntity Card,
            CardSelectionDraft Draft, SubSelectionPresenter Presenter, Panel Panel, Detail Detail,
            Dictionary<string, LocalizeInfoData> Texts);
        private sealed class FixedCards : ITargetCardCollectionValue
        {
            private readonly ICardEntity _card;
            public FixedCards(ICardEntity card) => _card = card;
            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext context) => new[] { _card };
        }
        private sealed class Panel : ICardSelectionPanel
        {
            public Action<ICardSelectionPanel.Property> OnOpen;
            public ICardSelectionPanel.Property Property;
            public ICardSelectionPanel.UpdateProperty Update;
            public int OpenCount, CloseCount;
            public void Init(IGameViewModel model, LocalizeLibrary library) { }
            public void Open(ICardSelectionPanel.Property property)
            { Property = property with { Selections = property.Selections.ToArray() }; OpenCount++; OnOpen?.Invoke(Property); }
            public void RenderUpdate(ICardSelectionPanel.UpdateProperty property) => Update = property;
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
