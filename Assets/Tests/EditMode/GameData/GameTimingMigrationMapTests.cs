using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class GameTimingMigrationMapTests
    {
        [TestCase((GameTiming)3, GameTiming.BeforeTurnEnd)]
        [TestCase((GameTiming)8, GameTiming.AfterDrawCard)]
        [TestCase((GameTiming)5, GameTiming.BeforeExecuteEnd)]
        [TestCase((GameTiming)10, GameTiming.BeforePlayCardEnd)]
        [TestCase((GameTiming)15, GameTiming.AfterTriggerBuffEffect)]
        public void BuffEffectMapping_UsesGameplayTriggerSide(
            GameTiming legacyTiming,
            GameTiming expectedTiming)
        {
            var found = GameTimingMigrationMap.TryGetTarget(
                legacyTiming,
                GameTimingMigrationUsage.BuffEffect,
                out var targetTiming,
                out var requiresReview);

            Assert.That(found, Is.True);
            Assert.That(requiresReview, Is.False);
            Assert.That(targetTiming, Is.EqualTo(expectedTiming));
        }

        [TestCase((GameTiming)2, GameTiming.BeforeTurnStart)]
        [TestCase((GameTiming)3, GameTiming.AfterTurnEnd)]
        [TestCase((GameTiming)8, GameTiming.BeforeDrawCard)]
        [TestCase((GameTiming)5, GameTiming.AfterExecuteEnd)]
        [TestCase((GameTiming)9, GameTiming.CardPlayIntent)]
        [TestCase((GameTiming)10, GameTiming.CardPlayResult)]
        public void SessionRuleMapping_UsesSessionLifecycleSide(
            GameTiming legacyTiming,
            GameTiming expectedTiming)
        {
            var found = GameTimingMigrationMap.TryGetTarget(
                legacyTiming,
                GameTimingMigrationUsage.SessionUpdateRule,
                out var targetTiming,
                out var requiresReview);

            Assert.That(found, Is.True);
            Assert.That(requiresReview, Is.False);
            Assert.That(targetTiming, Is.EqualTo(expectedTiming));
        }

        [TestCase((GameTiming)6, GameTiming.AfterCharacterSummon)]
        [TestCase((GameTiming)7, GameTiming.AfterCharacterDeath)]
        public void CharacterTimingMapping_RequiresManualReview(
            GameTiming legacyTiming,
            GameTiming expectedTiming)
        {
            var found = GameTimingMigrationMap.TryGetTarget(
                legacyTiming,
                GameTimingMigrationUsage.BuffEffect,
                out var targetTiming,
                out var requiresReview);

            Assert.That(found, Is.True);
            Assert.That(requiresReview, Is.True);
            Assert.That(targetTiming, Is.EqualTo(expectedTiming));
        }

        [Test]
        public void CurrentTiming_DoesNotNeedMigration()
        {
            var found = GameTimingMigrationMap.TryGetTarget(
                GameTiming.BeforeTurnEnd,
                GameTimingMigrationUsage.BuffEffect,
                out _,
                out _);

            Assert.That(found, Is.False);
        }
    }
}
