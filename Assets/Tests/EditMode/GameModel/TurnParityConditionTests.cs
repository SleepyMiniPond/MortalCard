using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class TurnParityConditionTests
    {
        [TestCase(1, true, false)]
        [TestCase(2, false, true)]
        [TestCase(3, true, false)]
        [TestCase(4, false, true)]
        public void Eval_ComposesTurnCountRemainderAndComparison(
            int turnCount,
            bool expectedOdd,
            bool expectedEven)
        {
            var built = new GameplayManagerTestBuilder().Build();
            for (var currentTurn = 0; currentTurn < turnCount; currentTurn++)
            {
                built.Status.SetNewTurn();
            }

            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnStart, SystemSource.Instance));

            Assert.That(_CreateParityCondition(ArithmeticConditionType.NotEqual).Eval(context),
                Is.EqualTo(expectedOdd));
            Assert.That(_CreateParityCondition(ArithmeticConditionType.Equal).Eval(context),
                Is.EqualTo(expectedEven));
        }

        private static IntegerCondition _CreateParityCondition(
            ArithmeticConditionType comparison)
        {
            return new IntegerCondition
            {
                Value = new ArithmeticInteger
                {
                    Operation = ArithmeticType.Remainder,
                    Left = new TurnCountInteger(),
                    Right = new ConstInteger { Value = 2 }
                },
                Conditions =
                {
                    new IntegerCompare
                    {
                        Arithmetic = comparison,
                        CompareValue = new ConstInteger { Value = 0 }
                    }
                }
            };
        }
    }
}
