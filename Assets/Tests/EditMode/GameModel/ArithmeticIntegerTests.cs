using System;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public class ArithmeticIntegerTests
    {
        [TestCase(ArithmeticType.None, 0)]
        [TestCase(ArithmeticType.Add, 1)]
        [TestCase(ArithmeticType.Multiply, 2)]
        [TestCase(ArithmeticType.Overwrite, 3)]
        [TestCase(ArithmeticType.Subtract, 4)]
        [TestCase(ArithmeticType.Divide, 5)]
        [TestCase(ArithmeticType.Remainder, 6)]
        public void ArithmeticType_KeepsSerializedValue(
            ArithmeticType operation,
            int expected)
        {
            Assert.That((int)operation, Is.EqualTo(expected));
        }

        [TestCase(ArithmeticType.Subtract, 5, 3, 2)]
        [TestCase(ArithmeticType.Divide, -5, 2, -3)]
        [TestCase(ArithmeticType.Remainder, -5, 2, -1)]
        public void Eval_WhenOperandsExist_ReturnsOperationResult(
            ArithmeticType operation,
            int left,
            int right,
            int expected)
        {
            _AssertSome(_Eval(operation, left, right), expected);
        }

        [TestCase(ArithmeticType.Divide)]
        [TestCase(ArithmeticType.Remainder)]
        public void Eval_WhenRightOperandIsZero_ReturnsNone(
            ArithmeticType operation)
        {
            Assert.That(_Eval(operation, 5, 0).HasValue, Is.False);
        }

        [Test]
        public void Eval_WhenOperandIsMissing_ReturnsNone()
        {
            var value = new ArithmeticInteger
            {
                Operation = ArithmeticType.Subtract,
                Left = new ConstInteger { Value = 5 },
                Right = new MissingIntegerValue()
            };

            Assert.That(value.Eval(null).HasValue, Is.False);
        }

        private static Option<int> _Eval(
            ArithmeticType operation,
            int left,
            int right)
        {
            return new ArithmeticInteger
            {
                Operation = operation,
                Left = new ConstInteger { Value = left },
                Right = new ConstInteger { Value = right }
            }.Eval(null);
        }

        private static void _AssertSome(Option<int> result, int expected)
        {
            Assert.That(result.TryGetValue(out var value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }

        [Serializable]
        private sealed class MissingIntegerValue : IIntegerValue
        {
            public Option<int> Eval(TriggerContext triggerContext)
            {
                return Option.None<int>();
            }
        }
    }
}
