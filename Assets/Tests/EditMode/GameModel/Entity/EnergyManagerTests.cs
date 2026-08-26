using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class EnergyManagerTests
    {
        [TestCase(-1, 10, 0, 10)]
        [TestCase(11, 10, 10, 10)]
        [TestCase(1, -1, 0, 0)]
        public void Constructor_ClampsEnergyToValidRange(
            int energy,
            int maxEnergy,
            int expectedEnergy,
            int expectedMaximum)
        {
            var manager = new EnergyManager(energy, maxEnergy);

            Assert.That(manager.Energy, Is.EqualTo(expectedEnergy));
            Assert.That(manager.MaxEnergy, Is.EqualTo(expectedMaximum));
        }

        [Test]
        public void GainMethods_WithNegativeValue_TreatValueAsZero()
        {
            var manager = new EnergyManager(5, 10);

            var recoverResult = manager.RecoverEnergy(-1);
            var gainResult = manager.GainEnergy(-1);

            Assert.That(manager.Energy, Is.EqualTo(5));
            _AssertZero(recoverResult.EnergyPoint, recoverResult.DeltaEp, recoverResult.OverEp);
            _AssertZero(gainResult.EnergyPoint, gainResult.DeltaEp, gainResult.OverEp);
        }

        [Test]
        public void LossMethods_WithNegativeValue_TreatValueAsZero()
        {
            var manager = new EnergyManager(5, 10);

            var consumeResult = manager.ConsumeEnergy(-1);
            var loseResult = manager.LoseEnergy(-1);

            Assert.That(manager.Energy, Is.EqualTo(5));
            _AssertZero(consumeResult.EnergyPoint, consumeResult.DeltaEp, consumeResult.OverEp);
            _AssertZero(loseResult.EnergyPoint, loseResult.DeltaEp, loseResult.OverEp);
        }

        [Test]
        public void GainEnergy_WhenAdditionOverflows_ClampsAndReportsOverValue()
        {
            var manager = new EnergyManager(1, int.MaxValue);

            var result = manager.GainEnergy(int.MaxValue);

            Assert.That(manager.Energy, Is.EqualTo(int.MaxValue));
            Assert.That(result.DeltaEp, Is.EqualTo(int.MaxValue - 1));
            Assert.That(result.OverEp, Is.EqualTo(1));
        }

        [Test]
        public void LoseEnergy_WhenValueExceedsCurrent_ClampsAndReportsOverValue()
        {
            var manager = new EnergyManager(10, 10);

            var result = manager.LoseEnergy(int.MaxValue);

            Assert.That(manager.Energy, Is.Zero);
            Assert.That(result.DeltaEp, Is.EqualTo(10));
            Assert.That(result.OverEp, Is.EqualTo(int.MaxValue - 10));
        }

        private static void _AssertZero(int energyPoint, int deltaEnergy, int overEnergy)
        {
            Assert.That(energyPoint, Is.Zero);
            Assert.That(deltaEnergy, Is.Zero);
            Assert.That(overEnergy, Is.Zero);
        }
    }
}
