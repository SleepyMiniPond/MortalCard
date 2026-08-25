using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class DispositionManagerTests
    {
        [TestCase(-1, 10, 0, 10)]
        [TestCase(11, 10, 10, 10)]
        [TestCase(1, -1, 0, 0)]
        public void Constructor_ClampsStateToValidRange(
            int initialDisposition,
            int maxDisposition,
            int expectedDisposition,
            int expectedMaximum)
        {
            var manager = new DispositionManager(initialDisposition, maxDisposition);

            Assert.That(manager.CurrentDisposition, Is.EqualTo(expectedDisposition));
            Assert.That(manager.MaxDisposition, Is.EqualTo(expectedMaximum));
        }

        [Test]
        public void IncreaseDisposition_WithNegativeValue_TreatsValueAsZero()
        {
            var manager = new DispositionManager(5, 10);

            var result = manager.IncreaseDisposition(-1);

            Assert.That(manager.CurrentDisposition, Is.EqualTo(5));
            Assert.That(result.DispositionPoint, Is.Zero);
            Assert.That(result.DeltaDisposition, Is.Zero);
            Assert.That(result.OverDisposition, Is.Zero);
        }

        [Test]
        public void DecreaseDisposition_WithNegativeValue_TreatsValueAsZero()
        {
            var manager = new DispositionManager(5, 10);

            var result = manager.DecreaseDisposition(-1);

            Assert.That(manager.CurrentDisposition, Is.EqualTo(5));
            Assert.That(result.DispositionPoint, Is.Zero);
            Assert.That(result.DeltaDisposition, Is.Zero);
            Assert.That(result.OverDisposition, Is.Zero);
        }

        [Test]
        public void IncreaseDisposition_WhenValueExceedsMaximum_ReportsAppliedAndOverValues()
        {
            var manager = new DispositionManager(0, 10);

            var result = manager.IncreaseDisposition(int.MaxValue);

            Assert.That(manager.CurrentDisposition, Is.EqualTo(10));
            Assert.That(result.DeltaDisposition, Is.EqualTo(10));
            Assert.That(result.OverDisposition, Is.EqualTo(int.MaxValue - 10));
        }

        [Test]
        public void DecreaseDisposition_WhenValueExceedsCurrent_ReportsAppliedAndOverValues()
        {
            var manager = new DispositionManager(10, 10);

            var result = manager.DecreaseDisposition(int.MaxValue);

            Assert.That(manager.CurrentDisposition, Is.Zero);
            Assert.That(result.DeltaDisposition, Is.EqualTo(10));
            Assert.That(result.OverDisposition, Is.EqualTo(int.MaxValue - 10));
        }
    }
}
