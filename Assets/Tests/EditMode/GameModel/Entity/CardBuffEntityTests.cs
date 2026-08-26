using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class CardBuffEntityTests
    {
        [Test]
        public void CreateFromData_WhenLevelIsNegative_ClampsLevelToZero()
        {
            var buff = _CreateCardBuff(-1);

            Assert.That(buff.Level, Is.Zero);
        }

        [Test]
        public void AddLevel_WhenResultIsNegative_ClampsLevelToZero()
        {
            var buff = _CreateCardBuff();

            buff.AddLevel(-2);

            Assert.That(buff.Level, Is.Zero);
        }

        [Test]
        public void AddLevel_WhenResultOverflows_SaturatesLevelAtIntegerMaximum()
        {
            var buff = _CreateCardBuff();

            buff.AddLevel(int.MaxValue);

            Assert.That(buff.Level, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Clone_WhenLevelIsZero_PreservesZeroLevel()
        {
            var buff = _CreateCardBuff();
            buff.AddLevel(-1);

            var clone = buff.Clone();

            Assert.That(clone.Level, Is.Zero);
            Assert.That(clone.Identity, Is.Not.EqualTo(buff.Identity));
        }

        private static CardBuffEntity _CreateCardBuff(int level = 1)
        {
            var buffData = new CardBuffData
            {
                ID = BuffTestBuilder.CardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(buffData)
                .Build();
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new AddCardBuffIntentAction(SystemSource.Instance));

            return BuffTestBuilder.CreateCardBuff(
                context,
                built.ContextManager.CardBuffLibrary,
                level: level);
        }
    }
}
