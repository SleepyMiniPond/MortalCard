using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class HealthManagerTests
    {
        [TestCase(-1, 10, 0, 10)]
        [TestCase(11, 10, 10, 10)]
        [TestCase(1, -1, 0, 0)]
        public void Constructor_ClampsHealthToValidRange(
            int currentHealth,
            int maxHealth,
            int expectedHealth,
            int expectedMaximum)
        {
            var manager = new HealthManager(currentHealth, maxHealth);

            Assert.That(manager.Hp, Is.EqualTo(expectedHealth));
            Assert.That(manager.MaxHp, Is.EqualTo(expectedMaximum));
            Assert.That(manager.Dp, Is.Zero);
        }

        [TestCase(DamageType.Normal)]
        [TestCase(DamageType.Penetrate)]
        [TestCase(DamageType.Additional)]
        [TestCase(DamageType.Effective)]
        public void TakeDamage_WithNegativeValue_TreatsValueAsZero(DamageType damageType)
        {
            var manager = new HealthManager(10, 10);
            manager.GetShield(5, default);

            var result = manager.TakeDamage(-1, default, damageType);

            Assert.That(manager.Hp, Is.EqualTo(10));
            Assert.That(manager.Dp, Is.EqualTo(5));
            Assert.That(result.DamagePoint, Is.Zero);
            Assert.That(result.DeltaHp, Is.Zero);
            Assert.That(result.DeltaDp, Is.Zero);
            Assert.That(result.OverHp, Is.Zero);
        }

        [Test]
        public void GetHeal_WithNegativeValue_TreatsValueAsZero()
        {
            var manager = new HealthManager(5, 10);

            var result = manager.GetHeal(-1, default);

            Assert.That(manager.Hp, Is.EqualTo(5));
            Assert.That(result.HealPoint, Is.Zero);
            Assert.That(result.DeltaHp, Is.Zero);
            Assert.That(result.OverHp, Is.Zero);
        }

        [Test]
        public void GetShield_WithNegativeValue_TreatsValueAsZero()
        {
            var manager = new HealthManager(10, 10);
            manager.GetShield(5, default);

            var result = manager.GetShield(-1, default);

            Assert.That(manager.Dp, Is.EqualTo(5));
            Assert.That(result.ShieldPoint, Is.Zero);
            Assert.That(result.DeltaDp, Is.Zero);
            Assert.That(result.OverDp, Is.Zero);
        }

        [Test]
        public void GetHeal_WhenAdditionOverflows_ClampsAndReportsOverValue()
        {
            var manager = new HealthManager(1, int.MaxValue);

            var result = manager.GetHeal(int.MaxValue, default);

            Assert.That(manager.Hp, Is.EqualTo(int.MaxValue));
            Assert.That(result.DeltaHp, Is.EqualTo(int.MaxValue - 1));
            Assert.That(result.OverHp, Is.EqualTo(1));
        }

        [Test]
        public void GetShield_WhenAdditionOverflows_ClampsAndReportsOverValue()
        {
            var manager = new HealthManager(int.MaxValue, int.MaxValue);
            manager.GetShield(1, default);

            var result = manager.GetShield(int.MaxValue, default);

            Assert.That(manager.Dp, Is.EqualTo(int.MaxValue));
            Assert.That(result.DeltaDp, Is.EqualTo(int.MaxValue - 1));
            Assert.That(result.OverDp, Is.EqualTo(1));
        }
    }
}
