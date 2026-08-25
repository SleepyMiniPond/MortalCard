using NUnit.Framework;

namespace MortalGame.Tests
{
    public class PlayerBuffEntityTests
    {
        [Test]
        public void AddLevel_WhenResultIsNegative_ClampsLevelToZero()
        {
            var buff = BuffTestBuilder.CreatePlayerBuff();

            buff.AddLevel(-2);

            Assert.That(buff.Level, Is.Zero);
        }

        [Test]
        public void AddLevel_WhenResultOverflows_SaturatesLevelAtIntegerMaximum()
        {
            var buff = BuffTestBuilder.CreatePlayerBuff();

            buff.AddLevel(int.MaxValue);

            Assert.That(buff.Level, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Constructor_WhenLevelExceedsMaximum_ClampsLevelToMaximum()
        {
            var buff = BuffTestBuilder.CreatePlayerBuff(level: 5, maxLevel: 3);

            Assert.That(buff.Level, Is.EqualTo(3));
        }

        [Test]
        public void AddLevel_WhenResultExceedsMaximum_ClampsLevelToMaximum()
        {
            var buff = BuffTestBuilder.CreatePlayerBuff(maxLevel: 3);

            buff.AddLevel(5);

            Assert.That(buff.Level, Is.EqualTo(3));
        }
    }
}
