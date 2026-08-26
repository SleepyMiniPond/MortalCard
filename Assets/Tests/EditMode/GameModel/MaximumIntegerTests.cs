using System;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public class MaximumIntegerTests
    {
        [Test]
        public void Eval_WhenValuesExist_ReturnsMaximumValue()
        {
            var value = new MaximumInteger
            {
                Values =
                {
                    new ConstInteger { Value = -8 },
                    new ConstInteger { Value = 3 },
                    new ConstInteger { Value = -5 }
                }
            };

            _AssertSome(value.Eval(null), 3);
        }

        [Test]
        public void Eval_WhenOnlyOneValueExists_ReturnsThatValue()
        {
            var value = new MaximumInteger
            {
                Values =
                {
                    new ConstInteger { Value = -7 }
                }
            };

            _AssertSome(value.Eval(null), -7);
        }

        [Test]
        public void Eval_WhenValuesAreEmpty_ReturnsNone()
        {
            var value = new MaximumInteger();

            Assert.That(value.Eval(null).HasValue, Is.False);
        }

        [Test]
        public void Eval_WhenAnyValueIsMissing_ReturnsNone()
        {
            var value = new MaximumInteger
            {
                Values =
                {
                    new ConstInteger { Value = -1 },
                    new MissingIntegerValue(),
                    new ConstInteger { Value = 5 }
                }
            };

            Assert.That(value.Eval(null).HasValue, Is.False);
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
