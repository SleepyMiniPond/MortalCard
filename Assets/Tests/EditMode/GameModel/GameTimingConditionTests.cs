using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class GameTimingConditionTests
    {
        [Test]
        public void Eval_WhenReactionOriginTimingMatches_ReturnsTrue()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new UpdateTimingAction(
                    GameTiming.BeforeTurnStart,
                    SystemSource.Instance));
            var condition = new GameTimingCondition
            {
                Timing = GameTiming.BeforeTurnStart
            };

            Assert.That(condition.Eval(context), Is.True);
        }

        [Test]
        public void Eval_WhenReactionOriginTimingDoesNotMatch_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new UpdateTimingAction(
                    GameTiming.AfterTurnStart,
                    SystemSource.Instance));
            var condition = new GameTimingCondition
            {
                Timing = GameTiming.BeforeTurnStart
            };

            Assert.That(condition.Eval(context), Is.False);
        }

        [Test]
        public void Eval_WhenActionHasTimingButHasNoReactionOriginTiming_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new NonDispatchAction(GameTiming.BeforeTurnStart));
            var condition = new GameTimingCondition
            {
                Timing = GameTiming.BeforeTurnStart
            };

            Assert.That(condition.Eval(context), Is.False);
        }

        [Test]
        public void Eval_WhenEffectChildContextReplacesAction_PreservesReactionOriginTiming()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var timingContext = _CreateContext(
                built,
                new UpdateTimingAction(
                    GameTiming.BeforeTurnStart,
                    SystemSource.Instance));
            var effectChildContext = timingContext with
            {
                Action = new NonDispatchAction(GameTiming.EffectIntent)
            };
            var condition = new GameTimingCondition
            {
                Timing = GameTiming.BeforeTurnStart
            };

            Assert.That(condition.Eval(effectChildContext), Is.True);
        }

        [Test]
        public void Eval_WhenNestedTimingContextIsCreated_UsesNestedReactionOriginTiming()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var nestedContext = _CreateContext(
                built,
                new UpdateTimingAction(
                    GameTiming.AfterTurnStart,
                    SystemSource.Instance));
            var beforeCondition = new GameTimingCondition
            {
                Timing = GameTiming.BeforeTurnStart
            };
            var afterCondition = new GameTimingCondition
            {
                Timing = GameTiming.AfterTurnStart
            };

            Assert.That(beforeCondition.Eval(nestedContext), Is.False);
            Assert.That(afterCondition.Eval(nestedContext), Is.True);
        }

        [Test]
        public void Eval_WhenConfiguredTimingIsNone_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateContext(
                built,
                new UpdateTimingAction(
                    GameTiming.BeforeTurnStart,
                    SystemSource.Instance));
            var condition = new GameTimingCondition
            {
                Timing = GameTiming.None
            };

            Assert.That(condition.Eval(context), Is.False);
        }

        private static TriggerContext _CreateContext(
            BuiltGameplay built,
            IActionUnit action)
        {
            return new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                action);
        }

        private sealed record NonDispatchAction(GameTiming Timing) : IActionUnit
        {
            public IActionSource Source => SystemSource.Instance;
        }
    }
}
