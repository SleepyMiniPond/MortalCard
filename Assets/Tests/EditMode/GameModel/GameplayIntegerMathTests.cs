using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public class GameplayIntegerMathTests
    {
        [TestCase(2, 3, 5)]
        [TestCase(int.MaxValue, 1, int.MaxValue)]
        [TestCase(int.MinValue, -1, int.MinValue)]
        public void Add_UsesSaturatingArithmetic(int left, int right, int expected)
        {
            _AssertSome(GameplayIntegerMath.Add(left, right), expected);
        }

        [TestCase(5, 3, 2)]
        [TestCase(int.MaxValue, -1, int.MaxValue)]
        [TestCase(int.MinValue, 1, int.MinValue)]
        public void Subtract_UsesSaturatingArithmetic(int left, int right, int expected)
        {
            _AssertSome(GameplayIntegerMath.Subtract(left, right), expected);
        }

        [TestCase(3, -4, -12)]
        [TestCase(int.MaxValue, 2, int.MaxValue)]
        [TestCase(int.MinValue, 2, int.MinValue)]
        [TestCase(int.MinValue, -1, int.MaxValue)]
        public void Multiply_UsesSaturatingArithmetic(int left, int right, int expected)
        {
            _AssertSome(GameplayIntegerMath.Multiply(left, right), expected);
        }

        [TestCase(5, 2, 2)]
        [TestCase(-5, 2, -3)]
        [TestCase(5, -2, -3)]
        [TestCase(-5, -2, 2)]
        [TestCase(int.MinValue, -1, int.MaxValue)]
        public void Divide_UsesFloorDivisionAndSaturatesOverflow(
            int dividend,
            int divisor,
            int expected)
        {
            _AssertSome(GameplayIntegerMath.Divide(dividend, divisor), expected);
        }

        [Test]
        public void Divide_WhenDivisorIsZero_ReturnsNone()
        {
            Assert.That(GameplayIntegerMath.Divide(5, 0).HasValue, Is.False);
        }

        [TestCase(5, 2, 1)]
        [TestCase(-5, 2, -1)]
        [TestCase(5, -2, 1)]
        [TestCase(-5, -2, -1)]
        public void Remainder_UsesIntegerRemainderSemantics(
            int dividend,
            int divisor,
            int expected)
        {
            _AssertSome(GameplayIntegerMath.Remainder(dividend, divisor), expected);
        }

        [Test]
        public void Remainder_WhenDivisorIsZero_ReturnsNone()
        {
            Assert.That(GameplayIntegerMath.Remainder(5, 0).HasValue, Is.False);
        }

        private static void _AssertSome(Option<int> result, int expected)
        {
            Assert.That(result.TryGetValue(out var value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }
    }
}
