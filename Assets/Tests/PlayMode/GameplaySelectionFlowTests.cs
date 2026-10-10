using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using MortalGame.Presenter;
using NUnit.Framework;
using Optional;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class GameplaySelectionFlowTests
    {
        private record OpenDetailEvent : IUniTaskPresenter.Event;
        private record CloseEvent : IUniTaskPresenter.Event;
        private sealed class PresenterLifetime : IDisposable
        {
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        [UnityTest, Timeout(5000)]
        public IEnumerator PanelPresenter_WaitsForDetailThenAcceptsClose() => UniTask.ToCoroutine(async () =>
        {
            var presenter = new UniTaskPresenter();
            var lifetime = new PresenterLifetime();
            var detailClosed = new UniTaskCompletionSource();
            var handled = 0;
            var run = presenter.Run(lifetime, () => true, CancellationToken.None, Handle,
                onStarted: () => presenter.TryEnqueueNextEvent(new OpenDetailEvent()));
            await UniTask.NextFrame();
            presenter.TryEnqueueNextEvent(new CloseEvent());
            Assert.That(handled, Is.EqualTo(1));
            detailClosed.TrySetResult();
            await UniTask.NextFrame();
            presenter.TryEnqueueNextEvent(new CloseEvent());
            await run;
            Assert.That(handled, Is.EqualTo(2));
            Assert.That(lifetime.Disposed, Is.True);

            async UniTask<IUniTaskPresenter.Event> Handle(IUniTaskPresenter.Event evt)
            {
                handled++;
                if (evt is CloseEvent) return new IUniTaskPresenter.Halt();
                presenter.TryEnqueueNextEvent(new CloseEvent());
                await detailClosed.Task;
                return new IUniTaskPresenter.None();
            }
        });

        [UnityTest]
        public IEnumerator Run_DisablesInteractionBeforeStartingWorkers() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var uiSawDisabled = false;
            typeof(GameplayPresenter).GetField("_uiPresenter", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(s.Presenter, new UIWorker(() => uiSawDisabled = s.View.Disabled));
            Exception cancelled = null;
            try { await s.Presenter.Run(cts.Token); }
            catch (OperationCanceledException error) { cancelled = error; }
            Assert.That(cancelled, Is.Not.Null);
            Assert.That(uiSawDisabled, Is.True, "啟動工作前須關閉互動，等待開始事件啟用");
            Assert.That(s.View.Events, Is.Empty);
            Assert.That(s.View.Disabled, Is.True);
        });

        [UnityTest]
        public IEnumerator BattleEnd_RejectsInputAndDoesNotRestoreFromCancellationOrDelayedEvents() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                await _Wait(() => s.Selection.RunCount == 1);
                typeof(GameplayManager).GetField("_battleResult", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(s.Model, Option.Some(new BattleResult(true, Array.Empty<CardInstanceChangeSet>())));
                _AddEvent(s, new PlayerExecuteStartEvent(Faction.Ally, default, default));
                _AddEvent(s, new CardPlayRejectedEvent(Faction.Ally, s.Card.Identity));
                await _Wait(() => s.View.Events.OfType<CardPlayRejectedEvent>().Any());
                Assert.That(s.View.Disabled, Is.True);
                Assert.That(s.Selection.FailureCount, Is.Zero);
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Cancelled());
                await _Wait(() => _Pending(s.Presenter).Count == 0);
                Assert.That(s.View.RestoreCount, Is.Zero);
                s.Presenter.RecieveEvent(new TurnSubmitCommand(Faction.Ally));
                Assert.That(_Pending(s.Presenter).Count, Is.Zero);
                Assert.That(s.View.Disabled, Is.True);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); await _Cancel(presenterTask); }
        });

        [UnityTest]
        public IEnumerator FourFlows_BattleCompletionWaitsForAllCleanup() => UniTask.ToCoroutine(async () =>
        {
            using var lifetime = new CancellationTokenSource();
            var battle = new UniTaskCompletionSource();
            var cleaned = 0;
            var tasks = new[]
            {
                _Supervise(() => battle.Task, lifetime, "戰鬥", true),
                _Supervise(WaitAndClean, lifetime, "玩家操作"),
                _Supervise(WaitAndClean, lifetime, "事件呈現"),
                _Supervise(WaitAndClean, lifetime, "UI")
            };
            battle.TrySetResult();
            var errors = await UniTask.WhenAll(tasks);
            Assert.That(errors.All(error => error == null), Is.True);
            Assert.That(cleaned, Is.EqualTo(3), "須等三條流程的非同步清理完成");

            async UniTask WaitAndClean()
            {
                try { await UniTask.WaitUntilCanceled(lifetime.Token); }
                finally { await UniTask.NextFrame(); cleaned++; }
            }
        });

        [UnityTest]
        public IEnumerator FourFlows_FailureWaitsForCleanupAndPreservesOriginalError() => UniTask.ToCoroutine(async () =>
        {
            using var lifetime = new CancellationTokenSource();
            var failure = new InvalidOperationException("測試 UI 失敗");
            var cleaned = 0;
            var errors = await UniTask.WhenAll(new[]
            {
                _Supervise(WaitAndClean, lifetime, "戰鬥", true),
                _Supervise(WaitAndClean, lifetime, "玩家操作"),
                _Supervise(WaitAndClean, lifetime, "事件呈現"),
                _Supervise(() => throw failure, lifetime, "UI")
            });
            Assert.That(errors.Single(error => error != null), Is.SameAs(failure));
            Assert.That(cleaned, Is.EqualTo(3));

            async UniTask WaitAndClean()
            {
                try { await UniTask.WaitUntilCanceled(lifetime.Token); }
                finally { await UniTask.NextFrame(); cleaned++; }
            }
        });

        [UnityTest]
        public IEnumerator UiCompletionBeforeBattle_IsReportedAsUnexpectedEnd() => UniTask.ToCoroutine(async () =>
        {
            using var lifetime = new CancellationTokenSource();
            var error = await _Supervise(() => UniTask.CompletedTask, lifetime, "UI");
            Assert.That(error, Is.TypeOf<InvalidOperationException>());
            Assert.That(error.Message, Does.Contain("UI流程"));
            Assert.That(lifetime.IsCancellationRequested, Is.True);
        });

        [UnityTest]
        public IEnumerator SelectionWait_KeepsRenderingAndRejectsDuplicateInput() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                await _Wait(() => s.Selection.RunCount == 1);
                var rendered = s.View.RenderCount;
                _AddEvent(s, new GeneralUpdateEvent(Array.Empty<CardInfo>()));
                _AddEvent(s, new PlayerExecuteStartEvent(Faction.Ally, default, default, Option.Some(Guid.NewGuid())));
                await _Wait(() => s.View.RenderCount > rendered);
                Assert.That(_Pending(s.Presenter).Count, Is.EqualTo(1));
                Assert.That(s.View.Disabled, Is.True, "回合開始事件不得清除選取中的指令");
                rendered = s.View.RenderCount;
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Assert.That(s.View.RenderCount, Is.EqualTo(rendered), "沒有事件時不須呼叫 Render");
                s.Presenter.RecieveEvent(new TurnSubmitCommand(Faction.Ally));
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Cancelled());
                await _Wait(() => s.View.RestoreCount == 1);
                Assert.That(s.View.Disabled, Is.False);
                Assert.That(s.View.Events.OfType<PlayerExecuteEndEvent>(), Is.Empty);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); await _Cancel(presenterTask); }
        });

        [UnityTest]
        public IEnumerator ReusedCommand_DoesNotKeepWaitingForPreviousEntry() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                var command = new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>());
                s.View.OnRestore = () =>
                {
                    s.View.OnRestore = null;
                    s.Presenter.RecieveEvent(command);
                };
                s.Presenter.RecieveEvent(command);
                await _Wait(() => s.Selection.RunCount == 1);
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Cancelled());
                await _Wait(() => s.Selection.RunCount == 2 && s.View.RestoreCount == 2);
                Assert.That(_Pending(s.Presenter).Count, Is.Zero);
                Assert.That(s.View.Disabled, Is.False);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); await _Cancel(presenterTask); }
        });

        [UnityTest]
        public IEnumerator RenderFailure_CancelsAndWaitsForSelectionCleanup() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                await _Wait(() => s.Selection.RunCount == 1);
                s.View.OnRender = () => throw new InvalidOperationException("測試畫面呈現失敗");
                _AddEvent(s, new GeneralUpdateEvent(Array.Empty<CardInfo>()));
                Exception failure = null;
                try { await presenterTask; } catch (InvalidOperationException error) { failure = error; }
                Assert.That(failure, Is.Not.Null);
                Assert.That(s.Selection.Exited, Is.True);
                Assert.That(s.View.RestoreCount, Is.Zero);
                Assert.That(s.View.Disabled, Is.True);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); }
        });

        [UnityTest]
        public IEnumerator CancellationAfterSubmission_DoesNotRestoreWithoutReadyEvent() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var modelCts = new CancellationTokenSource();
            using var presenterCts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(modelCts.Token);
            var presenterTask = _RunLoop(s.Presenter, presenterCts.Token);
            s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
            await _Wait(() => s.Selection.RunCount == 1);
            modelCts.Cancel();
            await _Cancel(modelTask);
            s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Completed(s.Draft));
            await UniTask.NextFrame();
            Assert.That(_Pending(s.Presenter).Count, Is.EqualTo(1), "未收到完成事件前須保留指令");
            presenterCts.Cancel();
            await _Cancel(presenterTask);
            Assert.That(s.View.RestoreCount, Is.Zero);
            Assert.That(s.View.Disabled, Is.True);
        });

        [UnityTest]
        public IEnumerator Confirm_PlaysOnceAndAllowsNextCommand() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                await _Wait(() => s.Selection.RunCount == 1);
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Completed(s.Draft));
                await _Wait(() => s.View.Events.OfType<UsedCardEvent>().Any());
                Assert.That(s.View.Events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
                Assert.That(s.Selection.RunCount, Is.EqualTo(1));
                Assert.That(s.Selection.FailureCount, Is.Zero);
                Assert.That(s.View.RestoreCount, Is.Zero, "成功出牌由操作開始事件恢復互動");
                Assert.That(s.View.Disabled, Is.False);
                var starts = s.View.Events.OfType<PlayerExecuteStartEvent>().ToArray();
                Assert.That(starts[0].CompletedCardIdentity.HasValue, Is.False);
                Assert.That(starts[1].CompletedCardIdentity, Is.EqualTo(Option.Some(s.Card.Identity)));
                s.Presenter.RecieveEvent(new TurnSubmitCommand(Faction.Ally));
                await _Wait(() => s.View.Events.OfType<PlayerExecuteEndEvent>().Any());
                Assert.That(s.View.Disabled, Is.True);
                Assert.That(_Pending(s.Presenter).Count, Is.Zero);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); await _Cancel(presenterTask); }
        });

        [UnityTest]
        public IEnumerator Cancel_RejectsDuplicateCommandsAndRestoresLatestHand() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                s.Presenter.RecieveEvent(new TurnSubmitCommand(Faction.Ally));
                await _Wait(() => s.Selection.RunCount == 1);
                Assert.That(s.View.Disabled, Is.True);
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Cancelled());
                await _Wait(() => s.View.RestoreCount == 1);
                Assert.That(s.View.Disabled, Is.False);
                Assert.That(s.View.Events.OfType<PlayerExecuteEndEvent>(), Is.Empty);
                Assert.That(s.View.Hand.CardInfos.Keys.Select(card => card.Identity), Does.Contain(s.Card.Identity));
                Assert.That(s.View.Events.OfType<UsedCardEvent>(), Is.Empty);
                Assert.That(s.Selection.FailureCount, Is.Zero);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); await _Cancel(presenterTask); }
            Assert.That(s.View.Disabled, Is.True);
        });

        [UnityTest]
        public IEnumerator RejectionNotice_BlocksInputUntilReadyEventIsPresented() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            s.Selection.FailureClosed = new UniTaskCompletionSource();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                await _Wait(() => s.Selection.RunCount == 1);
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Completed(
                    s.Draft.WithSelection("unexpected-group", new ExistCardSubSelectionAction(Array.Empty<Guid>()))));
                await _Wait(() => s.Selection.FailureCount == 1);
                Assert.That(s.View.Events.OfType<CardPlayRejectedEvent>().Count(), Is.EqualTo(1));
                Assert.That(s.View.Events.OfType<PlayerExecuteStartEvent>().Count(), Is.EqualTo(1));
                Assert.That(s.View.Disabled, Is.True);
                s.Presenter.RecieveEvent(new TurnSubmitCommand(Faction.Ally));
                s.Selection.FailureClosed.TrySetResult();
                await _Wait(() => s.View.Events.OfType<PlayerExecuteStartEvent>().Count() == 2);
                Assert.That(s.View.Disabled, Is.False);
                Assert.That(s.View.RestoreCount, Is.Zero);
                Assert.That(_Pending(s.Presenter).Count, Is.Zero);
                Assert.That(s.View.Events.OfType<PlayerExecuteEndEvent>(), Is.Empty);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); await _Cancel(presenterTask); }
        });

        [UnityTest]
        public IEnumerator SceneCancellationDuringRejection_DoesNotPresentReadyEvent() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            s.Selection.FailureClosed = new UniTaskCompletionSource();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
            await _Wait(() => s.Selection.RunCount == 1);
            s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Completed(
                s.Draft.WithSelection("unexpected-group", new ExistCardSubSelectionAction(Array.Empty<Guid>()))));
            await _Wait(() => s.Selection.FailureCount == 1);
            cts.Cancel();
            await _Cancel(modelTask);
            await _Cancel(presenterTask);
            Assert.That(s.View.Events.OfType<PlayerExecuteStartEvent>().Count(), Is.EqualTo(1));
            Assert.That(s.View.RestoreCount, Is.Zero);
            Assert.That(s.View.Disabled, Is.True);
        });

        [UnityTest]
        public IEnumerator ModelReject_ShowsFailureAfterRenderingAndRestoresInteraction() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            try
            {
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                await _Wait(() => s.Selection.RunCount == 1);
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Completed(
                    s.Draft.WithSelection("unexpected-group", new ExistCardSubSelectionAction(Array.Empty<Guid>()))));
                await _Wait(() => s.View.Events.OfType<PlayerExecuteStartEvent>().Count() == 2);
                Assert.That(s.Selection.FailureCount, Is.EqualTo(1));
                Assert.That(s.View.Events.OfType<UsedCardEvent>(), Is.Empty);
                var rejected = s.View.Events.OfType<CardPlayRejectedEvent>().Single();
                Assert.That(rejected.CardIdentity, Is.EqualTo(s.Card.Identity));
                Assert.That(s.View.RestoreCount, Is.Zero);
                Assert.That(s.View.Disabled, Is.False);
                Assert.That(s.Model.GetCard(s.Card.Identity).HasValue, Is.True);
                Assert.That(_Pending(s.Presenter).Count, Is.Zero);
                Assert.That(s.View.Events.OfType<PlayerExecuteEndEvent>(), Is.Empty);
            }
            finally { cts.Cancel(); await _Cancel(modelTask); await _Cancel(presenterTask); }
        });

        [UnityTest]
        public IEnumerator SceneCancellation_DoesNotRestoreInteraction() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var modelTask = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
            await _Wait(() => s.Selection.RunCount == 1);
            cts.Cancel();
            await _Cancel(modelTask);
            await _Cancel(presenterTask);
            Assert.That(s.View.RestoreCount, Is.Zero);
            Assert.That(s.View.Disabled, Is.True);
        });

        [UnityTest]
        public IEnumerator TurnSubmit_AllowsCommandsOnNextPlayerPhase() => UniTask.ToCoroutine(async () =>
        {
            var s = _Create();
            using var cts = new CancellationTokenSource();
            var firstPhase = s.Model._PlayerExecute(cts.Token);
            var presenterTask = _RunLoop(s.Presenter, cts.Token);
            UniTask secondPhase = UniTask.CompletedTask;
            try
            {
                s.Presenter.RecieveEvent(new TurnSubmitCommand(Faction.Ally));
                await firstPhase;
                secondPhase = s.Model._PlayerExecute(cts.Token);
                await _Wait(() => s.View.Events.OfType<PlayerExecuteStartEvent>().Count() == 2);
                var restoredBeforeSelection = s.View.RestoreCount;
                s.Presenter.RecieveEvent(new UseCardCommand(s.Card.Identity, Option.None<ISelectionTarget>()));
                await _Wait(() => s.Selection.RunCount == 1);
                s.Selection.Result.TrySetResult(new CardSelectionPresentationResult.Cancelled());
                await _Wait(() => s.View.RestoreCount == restoredBeforeSelection + 1);
            }
            finally { cts.Cancel(); await _Cancel(secondPhase); await _Cancel(presenterTask); }
        });

        private static Setup _Create()
        {
            var data = new StandardCardData { ID = "selection-flow", Cost = 0, Power = 0, Themes = Array.Empty<CardTheme>() };
            var library = new CardLibrary(new Dictionary<string, CardData> { [data.ID] = data });
            var context = new GameContextManager(library,
                new CardBuffLibrary(new Dictionary<string, CardBuffData>()),
                new PlayerBuffLibrary(new Dictionary<string, PlayerBuffData>()),
                new CharacterBuffLibrary(new Dictionary<string, CharacterBuffData>()),
                new DispositionLibrary(new[] { new DispositionData("test", 1, 0, 0) }),
                new LocalizeLibrary(new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                    new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()),
                new GameRandom(1), CardPropertyEntityFactory.CreateDefault(),
                CardBuffPropertyEntityFactory.CreateDefault(), CardBuffLifeTimeEntityFactory.CreateDefault(),
                ReactionSessionEntityFactory.CreateDefault(), PlayerBuffPropertyEntityFactory.CreateDefault(),
                PlayerBuffLifeTimeEntityFactory.CreateDefault(), CharacterBuffPropertyEntityFactory.CreateDefault(),
                CharacterBuffLifeTimeEntityFactory.CreateDefault());
            var characters = new[] { new CharacterParameter { NameKey = "test", CurrentHealth = 100, MaxHealth = 100 } };
            var ally = new AllyEntity(Guid.NewGuid(), characters, 0, 3, 5, 0, 10, context);
            var enemy = new EnemyEntity(characters, 0, 3, 5, 3, 0, 0, context);
            var status = new GameStatus();
            status.SummonAlly(ally); status.SummonEnemy(enemy);
            var manager = (GameplayManager)typeof(GameplayManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(GameStageSetting), typeof(GameContextManager), typeof(GameStatus) }, null)
                .Invoke(new object[] { default(GameStageSetting), context, status });
            var card = CardEntity.RuntimeCreateFromId(data.ID, library, CardPropertyEntityFactory.CreateDefault());
            ally.CardManager.HandCard.AddCard(card);
            typeof(GameplayManager).GetField("_gameActions", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, new UniTaskAwaitableQueue<IGameAction>());
            var view = new View();
            var selection = new Selection();
            // 測試僅執行操作迴圈，所需替身在此注入，不為正式 Presenter 增加測試建構子。
            var presenter = (GameplayPresenter)FormatterServices.GetUninitializedObject(typeof(GameplayPresenter));
            SetField("_gameplayView", view);
            SetField("_gameplayManager", manager);
            SetField("_subSelectionPresenter", selection);
            var queueField = typeof(GameplayPresenter).GetField("_pendingGameCommands",
                BindingFlags.Instance | BindingFlags.NonPublic);
            queueField.SetValue(presenter, Activator.CreateInstance(queueField.FieldType));
            return new Setup(manager, card, CardSelectionDraft.Create(card, ally, MainSelectionAction.Empty),
                presenter, view, selection);

            void SetField(string name, object value) => typeof(GameplayPresenter)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, value);
        }
        private static async UniTask _RunLoop(GameplayPresenter presenter, CancellationToken token)
        {
            // 定向測試只啟動操作與呈現，使用正式生命週期監督方法。
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                var errors = await UniTask.WhenAll(new[]
                {
                    _Supervise(() => Invoke("_ProcessCommands", lifetime.Token),
                        lifetime, "玩家操作"),
                    _Supervise(() => Invoke("_RenderEvents", lifetime.Token),
                        lifetime, "事件呈現")
                });
                var error = errors.FirstOrDefault(value => value != null);
                if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                token.ThrowIfCancellationRequested();
            }
            finally
            {
                var pending = _Pending(presenter);
                pending.GetType().GetMethod("Clear").Invoke(pending, null);
                ((IGameplayView)typeof(GameplayPresenter).GetField("_gameplayView",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(presenter)).DisableAllInteraction();
            }

            UniTask Invoke(string name, params object[] arguments) => (UniTask)typeof(GameplayPresenter)
                .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(presenter, arguments);
        }

        private static ICollection _Pending(GameplayPresenter presenter) =>
            (ICollection)typeof(GameplayPresenter).GetField("_pendingGameCommands",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presenter);

        private static void _AddEvent(Setup setup, IGameEvent gameEvent) =>
            ((List<IGameEvent>)typeof(GameplayManager).GetField("_gameEvents",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(setup.Model)).Add(gameEvent);

        private static UniTask<Exception> _Supervise(Func<UniTask> run, CancellationTokenSource lifetime,
            string name, bool completesBattle = false)
            => (UniTask<Exception>)typeof(GameplayPresenter)
                .GetMethod("_RunBattleFlow", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { run, lifetime, name, completesBattle });
        private static async UniTask _Wait(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++) await UniTask.NextFrame();
            Assert.That(condition(), Is.True, "流程應在期限內完成");
        }
        private static async UniTask _Cancel(UniTask task)
        { try { await task; } catch (OperationCanceledException) { } }
        private sealed class Setup
        {
            public readonly GameplayManager Model;
            public readonly ICardEntity Card;
            public readonly CardSelectionDraft Draft;
            public readonly GameplayPresenter Presenter;
            public readonly View View;
            public readonly Selection Selection;
            public Setup(GameplayManager model, ICardEntity card, CardSelectionDraft draft,
                GameplayPresenter presenter, View view, Selection selection)
            { Model = model; Card = card; Draft = draft; Presenter = presenter; View = view; Selection = selection; }
        }
        private sealed class Selection : ISubSelectionPresenter
        {
            public int RunCount, FailureCount;
            public bool Exited;
            public UniTaskCompletionSource FailureClosed;
            public readonly UniTaskCompletionSource<CardSelectionPresentationResult> Result = new();
            public async UniTask<CardSelectionPresentationResult> RunSubSelection(IGameplayModel model,
                CardSelectionDraft draft, CancellationToken token)
            {
                RunCount++;
                try { return await Result.Task.AttachExternalCancellation(token); }
                finally { Exited = true; }
            }
            public async UniTask ShowFailure(CancellationToken token, CardSelectionFailure failure = null)
            {
                token.ThrowIfCancellationRequested();
                FailureCount++;
                if (FailureClosed != null) await FailureClosed.Task.AttachExternalCancellation(token);
            }
        }

        private sealed class UIWorker : IUIPresenter
        {
            private readonly Action _onRun;
            public UIWorker(Action onRun) => _onRun = onRun;
            public UniTask Run(CancellationToken token)
            {
                _onRun();
                return UniTask.WaitUntilCanceled(token);
            }
        }

        private sealed class View : IGameplayView
        {
            public int RestoreCount;
            public int RenderCount;
            public Action OnRender;
            public Action OnRestore;
            public bool Disabled;
            public CardCollectionInfo Hand;
            public readonly List<IGameEvent> Events = new();
            public IEnumerable<ISelectableView> SelectableViews => Array.Empty<ISelectableView>();
            public ISelectableView BasicSelectableView => null;
            public DeckCardView DeckCardView => null;
            public GraveyardCardView GraveyardCardView => null;
            public EnemySelectedCardView EnemySelectedCardView => null;
            public AllCardDetailPanel DetailPanel => null;
            public SingleCardDetailPopupPanel SinglePopupPanel => null;
            public FocusCardDetailView FocusCardDetailView => null;
            public ICardSelectionPanel CardSelectionPanel => null;
            public void Init(IGameViewModel model, IGameplayActionReciever receiver, IGameplayModel watcher,
                LocalizeLibrary library, DispositionLibrary disposition) { }
            public void Render(IReadOnlyCollection<IGameEvent> events, IGameplayActionReciever receiver)
            {
                RenderCount++; Events.AddRange(events);
                foreach (var gameEvent in events)
                {
                    if (gameEvent is PlayerExecuteStartEvent { Faction: Faction.Ally }) Disabled = false;
                    if (gameEvent is PlayerExecuteEndEvent) Disabled = true;
                }
                OnRender?.Invoke();
            }
            public void DisableAllHandCards() => Disabled = true;
            public void DisableAllInteraction() => Disabled = true;
            public void RestorePlayerInteraction(CardManagerInfo manager, CardCollectionInfo hand)
            { RestoreCount++; Hand = hand; Disabled = false; OnRestore?.Invoke(); }
            public UniTask DisposeCharacterViews() => UniTask.CompletedTask;
        }
    }
}
