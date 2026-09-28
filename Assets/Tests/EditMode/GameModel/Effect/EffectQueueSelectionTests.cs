using Optional;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class EffectQueueSelectionTests
    {
        [Test]
        public void TailBatchAndExplicitEmptySelection_PreserveOrderAndRestoreInheritedSelection()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var selected = _Selection();
            var seen = new List<GameContext>();
            var runner = new EffectQueueRunner(built.ContextManager);
            runner.Enqueue(new CallbackItem(queue =>
            {
                queue.Enqueue(new EffectQueueItem[]
                {
                    new CallbackItem(_ => seen.Add(built.ContextManager.Context)),
                    new CallbackItem(_ => seen.Add(built.ContextManager.Context))
                }, EffectQueuePosition.Tail);
                queue.Enqueue(new CallbackItem(_ => seen.Add(built.ContextManager.Context)),
                    EffectQueuePosition.Immediate, GameContext.EMPTY.Some());
            }), selection: selected.Some());
            runner.RunToCompletion();

            Assert.That(seen, Is.EqualTo(new[] { GameContext.EMPTY, selected, selected }));
            Assert.That(built.ContextManager.Context, Is.SameAs(GameContext.EMPTY));
        }

        [Test]
        public void NestedSelections_RestoreSiblingsAndOuterContextIncludingGroups()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var manager = built.ContextManager;
            var outer = _Selection();
            var first = _Selection();
            var nested = _Selection();
            var seen = new List<GameContext>();
            using var outerScope = manager.SetContext(outer);
            var runner = new EffectQueueRunner(built.ContextManager);
            runner.Enqueue(new CallbackItem(_ => seen.Add(manager.Context)));
            runner.Enqueue(new EffectQueueItem[]
            {
                new CallbackItem(queue =>
                {
                    seen.Add(manager.Context);
                    queue.Enqueue(new CallbackItem(_ => seen.Add(manager.Context)));
                    queue.Enqueue(new EffectQueueItem[]
                    {
                        new CallbackItem(inner =>
                        {
                            seen.Add(manager.Context);
                            inner.Enqueue(new CallbackItem(_ => seen.Add(manager.Context)), EffectQueuePosition.Immediate);
                        })
                    }, EffectQueuePosition.Immediate, nested.Some());
                }),
                new CallbackItem(_ => seen.Add(manager.Context))
            }, EffectQueuePosition.Immediate, first.Some());

            Assert.That(manager.Context, Is.SameAs(outer));
            runner.RunToCompletion();

            Assert.That(seen, Is.EqualTo(new[] { first, nested, nested, first, outer, first }));
            Assert.That(manager.Context, Is.SameAs(outer));
            Assert.That(runner.ProcessedItemCount, Is.EqualTo(6));
        }

        [Test]
        public void DerivedEffectAndCommand_UseSnapshotAfterEnqueueScopeHasEnded()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var selected = GameContext.EMPTY with { SelectedPlayer = built.Ally.Identity };
            var context = new TriggerContext(built.Manager, new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner(built.ContextManager);
            runner.Enqueue(new EffectQueueItem[]
            {
                // 此工作沒有 TriggerContext，仍需把選取傳給衍生的 Resolver 與 Command。
                new CallbackItem(queue => queue.Enqueue(new CardEffectQueueItem(context,
                    new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new SelectedPlayer() },
                        Value = new ConstInteger { Value = 1 }
                    }), EffectQueuePosition.Immediate))
            }, EffectQueuePosition.Immediate, selected.Some());

            runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(built.Enemy.CurrentEnergy, Is.EqualTo(0));
            Assert.That(built.ContextManager.Context, Is.SameAs(GameContext.EMPTY));
            Assert.That(runner.ProcessedItemCount, Is.EqualTo(3));
        }

        [Test]
        public void Exception_RestoresSelectionAndDoesNotLeakToNewWork()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var runner = new EffectQueueRunner(built.ContextManager);
            runner.Enqueue(new EffectQueueItem[]
            {
                new CallbackItem(_ => throw new InvalidOperationException("測試中止"))
            }, EffectQueuePosition.Immediate, _Selection().Some());

            Assert.Throws<InvalidOperationException>(() => runner.RunToCompletion());
            Assert.That(built.ContextManager.Context, Is.SameAs(GameContext.EMPTY));
            runner.Enqueue(new CallbackItem(_ =>
                Assert.That(built.ContextManager.Context, Is.SameAs(GameContext.EMPTY))));
            runner.RunToCompletion();
        }

        [Test]
        public void Cancellation_RestoresSelectionBeforeNextItem()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var cancellation = new CancellationTokenSource();
            var runner = new EffectQueueRunner(built.ContextManager, new EffectQueueExecutionScope(10), _ => { }, cancellation.Token);
            var selected = _Selection();
            runner.Enqueue(new EffectQueueItem[]
            {
                new CallbackItem(_ =>
                {
                    Assert.That(built.ContextManager.Context, Is.SameAs(selected));
                    cancellation.Cancel();
                }),
                new CallbackItem(_ => Assert.Fail("取消後不得執行下一個工作"))
            }, EffectQueuePosition.Immediate, selected.Some());

            Assert.Throws<OperationCanceledException>(() => runner.RunToCompletion());
            Assert.That(built.ContextManager.Context, Is.SameAs(GameContext.EMPTY));
            Assert.That(runner.ProcessedItemCount, Is.EqualTo(1));
        }

        [Test]
        public void BudgetHalt_RestoresSelectionAndPreservesCommittedEventsOnce()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var recorded = new List<IGameEvent>();
            var runner = new EffectQueueRunner(built.ContextManager, new EffectQueueExecutionScope(1), recorded.AddRange, CancellationToken.None);
            var selected = _Selection();
            runner.Enqueue(new EffectQueueItem[]
            {
                new EventItem(),
                new CallbackItem(_ => Assert.Fail("預算耗盡後不得執行下一個工作"))
            }, EffectQueuePosition.Immediate, selected.Some());
            LogAssert.Expect(LogType.Error, new Regex("\\[EffectQueueRunner\\] 執行預算已耗盡"));

            var result = runner.RunToCompletion();

            Assert.That(runner.IsHalted, Is.True);
            Assert.That(result.Events.Count, Is.EqualTo(1));
            Assert.That(recorded.Count, Is.EqualTo(1));
            Assert.That(built.ContextManager.Context, Is.SameAs(GameContext.EMPTY));
        }

        private static GameContext _Selection()
        {
            return new GameContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                ImmutableDictionary<string, ImmutableArray<Guid>>.Empty
                    .Add("selected", ImmutableArray.Create(Guid.NewGuid())));
        }

        private sealed record CallbackItem(Action<IEffectQueueContext> Callback) : EffectQueueItem((TriggerContext)null)
        {
            public override EffectResult Execute(IEffectQueueContext queue)
            {
                Callback(queue);
                return EffectResult.Empty;
            }
        }

        private sealed record SelectionTestEvent : IGameEvent;

        private sealed record EventItem() : EffectQueueItem((TriggerContext)null)
        {
            public override EffectResult Execute(IEffectQueueContext queue) => new(
                Array.Empty<BaseResultAction>(), new IGameEvent[] { new SelectionTestEvent() });
        }
    }
}
