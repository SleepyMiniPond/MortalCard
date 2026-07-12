using System;
using System.Linq;
using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class GameTimingSerializationTests
    {
        [TestCase(GameTiming.None, 0)]
        [TestCase(GameTiming.GameStart, 1)]
        [TestCase(GameTiming.EffectIntent, 11)]
        [TestCase(GameTiming.EffectTargetIntent, 12)]
        [TestCase(GameTiming.EffectTargetResult, 13)]
        public void ExistingTiming_KeepsSerializedValue(GameTiming timing, int expectedValue)
        {
            Assert.That((int)timing, Is.EqualTo(expectedValue));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(14)]
        [TestCase(15)]
        public void LegacySerializedValue_IsReservedButNoLongerDefined(int legacyValue)
        {
            Assert.That(Enum.IsDefined(typeof(GameTiming), legacyValue), Is.False);
        }

        [Test]
        public void BeforeAfterTimings_AreAppendedAfterExistingSerializedValues()
        {
            var beforeAfterTimings = new[]
            {
                GameTiming.BeforeTurnStart,
                GameTiming.AfterTurnStart,
                GameTiming.BeforeDrawCard,
                GameTiming.AfterDrawCard,
                GameTiming.BeforeExecuteStart,
                GameTiming.AfterExecuteStart,
                GameTiming.BeforeExecuteEnd,
                GameTiming.AfterExecuteEnd,
                GameTiming.BeforeTurnEnd,
                GameTiming.AfterTurnEnd,
                GameTiming.BeforePlayCardStart,
                GameTiming.AfterPlayCardStart,
                GameTiming.BeforePlayCardEnd,
                GameTiming.AfterPlayCardEnd,
                GameTiming.BeforeTriggerBuffEffect,
                GameTiming.AfterTriggerBuffEffect,
                GameTiming.BeforeCharacterSummon,
                GameTiming.AfterCharacterSummon,
                GameTiming.BeforeCharacterDeath,
                GameTiming.AfterCharacterDeath,
                GameTiming.CardPlayIntent,
                GameTiming.CardPlayResult
            };

            Assert.That(beforeAfterTimings.Select(timing => (int)timing), Is.All.GreaterThan(15));
            Assert.That(beforeAfterTimings.Distinct().Count(), Is.EqualTo(beforeAfterTimings.Length));
        }

        [Test]
        public void UpdateTimingDropdown_ExcludesObsoleteTimings()
        {
            Assert.That(DropdownHelper.IsSelectableGameTiming((GameTiming)3), Is.False);
            Assert.That(DropdownHelper.IsSelectableGameTiming((GameTiming)10), Is.False);
            Assert.That(DropdownHelper.IsSelectableGameTiming((GameTiming)15), Is.False);
            Assert.That(DropdownHelper.IsSelectableGameTiming(GameTiming.BeforeTurnEnd), Is.True);
            Assert.That(DropdownHelper.IsSelectableGameTiming(GameTiming.CardPlayResult), Is.True);
        }
    }
}
