using System;
using MortalGame.GameModel;
using MortalGame.GameData;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{

    public class EffectQueueRunnerTests
    {
        [Test]
        public void RunToCompletion_WithCardEffects_AppliesCommandsAndCollectsResultsInOrder()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new GainEnergyIntentAction(SystemSource.Instance));
            var runner = new EffectQueueRunner();
            var firstEffect = new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            };
            var secondEffect = new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 2 }
            };

            runner.Enqueue(new CardEffectQueueItem(context, firstEffect));
            runner.Enqueue(new CardEffectQueueItem(context, secondEffect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(3));
            Assert.That(result.Actions.Select(action => action.GetType()).ToArray(), Is.EqualTo(new[]
            {
            typeof(GainEnergyResultAction),
            typeof(GainEnergyResultAction)
        }));
            Assert.That(result.Events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void RunToCompletion_WithPlayerBuffEffect_AppliesCommands()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var context = new TriggerContext(
                built.Manager,
                new PlayerBuffTrigger(BuffTestBuilder.CreatePlayerBuff()),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();
            var effect = new EffectiveDamagePlayerBuffEffect
            {
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
                },
                Value = new ConstInteger { Value = 5 }
            };

            runner.Enqueue(new PlayerBuffEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(95));
            Assert.That(result.Actions.Single(), Is.TypeOf<DamageResultAction>());
            Assert.That(result.Events.OfType<DamageEvent>().Single().Character, Is.SameAs(built.Ally.MainCharacter));
        }

        [Test]
        public void RunToCompletion_WithCharacterBuffEffect_AppliesCommands()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Enemy);
            var context = new TriggerContext(
                built.Manager,
                new CharacterBuffTrigger(BuffTestBuilder.CreateCharacterBuff()),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();
            var effect = new EffectiveDamageCharacterBuffEffect
            {
                Targets = new SingleCharacterCollection
                {
                    Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
                },
                Value = new ConstInteger { Value = 7 }
            };

            runner.Enqueue(new CharacterBuffEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Enemy.MainCharacter.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.Actions.Single(), Is.TypeOf<DamageResultAction>());
            Assert.That(result.Events.OfType<DamageEvent>().Single().Character, Is.SameAs(built.Enemy.MainCharacter));
        }

        [Test]
        public void RunToCompletion_WithUnknownCardBuffEffect_ReturnsEmptyResult()
        {
            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalCardBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new NoOpCardBuffEffect()
                });
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(cardBuffData)
                .Build();
            var createBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var buff = BuffTestBuilder.CreateCardBuff(createBuffContext, built.ContextManager.CardBuffLibrary);
            var context = new TriggerContext(
                built.Manager,
                new CardBuffTrigger(buff),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardBuffEffectQueueItem(context, new NoOpCardBuffEffect()));
            var result = runner.RunToCompletion();

            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void RunToCompletion_WhenItemEnqueuesAnotherItem_ProcessesEnqueuedItemBeforeReturning()
        {
            var runner = new EffectQueueRunner();

            runner.Enqueue(new ChainedQueueItem(null, 1, 2));
            var result = runner.RunToCompletion();

            Assert.That(result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id).ToArray(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void RunToCompletion_WhenItemEnqueuesImmediateItem_ProcessesItBeforeQueuedTail()
        {
            var runner = new EffectQueueRunner();

            runner.Enqueue(new ImmediateQueueItem(null));
            runner.Enqueue(new StaticQueueItem(null, 3));
            var result = runner.RunToCompletion();

            Assert.That(result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id).ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void RunToCompletion_WhenImmediateItemsUseExecutionOrder_PreservesProvidedOrder()
        {
            var runner = new EffectQueueRunner();

            runner.Enqueue(new ImmediateSequenceQueueItem(null));
            runner.Enqueue(new StaticQueueItem(null, 4));
            var result = runner.RunToCompletion();

            Assert.That(
                result.Events.OfType<TestQueueEvent>().Select(evt => evt.Id).ToArray(),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void RunToCompletion_WhenQueueExceedsMaxProcessedItemCount_HaltsSafely()
        {
            var runner = new EffectQueueRunner();
            ExpectBudgetExceededLog(EffectQueueRunner.BUDGET_COUNT);

            runner.Enqueue(new SelfEnqueueingQueueItem(null));
            var result = runner.RunToCompletion();

            Assert.IsTrue(runner.IsHalted);
            Assert.That(runner.PendingItemCount, Is.EqualTo(1));
            Assert.That(
                result.Events.OfType<TestQueueEvent>().Count(),
                Is.EqualTo(EffectQueueRunner.BUDGET_COUNT));
        }

        [Test]
        public void RunToCompletion_WhenBudgetExceeded_ProvidesCorrelationAndTriggerPath()
        {
            var runner = new EffectQueueRunner();
            ExpectBudgetExceededLog(EffectQueueRunner.BUDGET_COUNT);

            runner.Enqueue(new SelfEnqueueingQueueItem(null));
            runner.RunToCompletion();

            Assert.That(runner.IsHalted, Is.True);
            Assert.That(runner.HaltDiagnostic, Is.Not.Null);
            Assert.That(runner.HaltDiagnostic.CorrelationId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(
                runner.HaltDiagnostic.Budget,
                Is.EqualTo(EffectQueueRunner.BUDGET_COUNT));
            Assert.That(
                runner.HaltDiagnostic.ProcessedItemCount,
                Is.EqualTo(EffectQueueRunner.BUDGET_COUNT));
            Assert.That(
                runner.HaltDiagnostic.TriggerPath.Last(),
                Is.EqualTo(nameof(SelfEnqueueingQueueItem)));
            Assert.That(
                runner.HaltDiagnostic.TriggerPath.Count,
                Is.EqualTo(EffectQueueRunner.BUDGET_COUNT + 1));
        }

        [Test]
        public void RunToCompletion_TwoRunners_HaveIndependentBudgets()
        {
            var firstRunner = new EffectQueueRunner();
            var secondRunner = new EffectQueueRunner();

            firstRunner.Enqueue(new StaticQueueItem(null, 1));
            firstRunner.Enqueue(new StaticQueueItem(null, 2));
            var firstResult = firstRunner.RunToCompletion();

            secondRunner.Enqueue(new StaticQueueItem(null, 3));
            secondRunner.Enqueue(new StaticQueueItem(null, 4));
            var secondResult = secondRunner.RunToCompletion();

            Assert.That(
                firstResult.Events.OfType<TestQueueEvent>().Select(evt => evt.Id),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(
                secondResult.Events.OfType<TestQueueEvent>().Select(evt => evt.Id),
                Is.EqualTo(new[] { 3, 4 }));
            Assert.That(firstRunner.ProcessedItemCount, Is.EqualTo(2));
            Assert.That(secondRunner.ProcessedItemCount, Is.EqualTo(2));
            Assert.That(firstRunner.IsHalted, Is.False);
            Assert.That(secondRunner.IsHalted, Is.False);
        }

        private static void ExpectBudgetExceededLog(int budget)
        {
            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    @"^\[EffectQueueRunner\] 執行預算已耗盡。" +
                    @"CorrelationId=[^,]+, " +
                    $@"Budget={budget}, " +
                    $@"ProcessedItemCount={budget}, " +
                    @"TriggerPath=.+$"));
        }

        [Test]
        public void TimingDispatchPlan_OrdersGeneralReactionsBeforeFormTransitions()
        {
            var plan = new TimingDispatchPlan(
                new EffectQueueItem[]
                {
                    new StaticQueueItem(null, 1),
                    new StaticQueueItem(null, 2)
                },
                new EffectQueueItem[]
                {
                    new StaticQueueItem(null, 3)
                });

            Assert.That(
                plan.OrderedItems
                    .Cast<StaticQueueItem>()
                    .Select(item => item.Id),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }
    }
}

public sealed record TestQueueEvent(int Id) : IGameEvent;

public sealed record ChainedQueueItem(
    TriggerContext Context,
    int CurrentId,
    int NextId) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        if (NextId > 0)
        {
            queue.Enqueue(new ChainedQueueItem(Context, NextId, 0));
        }

        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(CurrentId) });
    }
}

public sealed record SelfEnqueueingQueueItem(TriggerContext Context) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        queue.Enqueue(new SelfEnqueueingQueueItem(Context));
        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(queue.ProcessedItemCount) });
    }
}

public sealed record ImmediateQueueItem(TriggerContext Context) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        queue.EnqueueImmediate(new StaticQueueItem(Context, 2));
        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(1) });
    }
}

public sealed record StaticQueueItem(TriggerContext Context, int Id) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        return new EffectResult(Array.Empty<BaseResultAction>(), new IGameEvent[] { new TestQueueEvent(Id) });
    }
}

public sealed record ImmediateSequenceQueueItem(TriggerContext Context) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        queue.EnqueueImmediate(new EffectQueueItem[]
        {
            new StaticQueueItem(Context, 2),
            new StaticQueueItem(Context, 3)
        });
        return new EffectResult(
            Array.Empty<BaseResultAction>(),
            new IGameEvent[] { new TestQueueEvent(1) });
    }
}
