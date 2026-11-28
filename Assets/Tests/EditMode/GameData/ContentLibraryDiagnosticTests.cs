using System.Collections.Generic;
using MortalGame.GameData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class ContentLibraryDiagnosticTests
    {
        private const string MissingId = "missing-content-diagnostic";

        [Test]
        public void PlayerBuffMissing_ReportsEachLookupAndPreservesDistinctFallbacks()
        {
            var library = new PlayerBuffLibrary(new Dictionary<string, PlayerBuffData>());
            _ExpectMissing("PlayerBuff");
            Assert.That(library.GetBuffEffects(MissingId, default).HasValue, Is.False);
            _ExpectMissing("PlayerBuff");
            Assert.That(library.GetBuffSessions(MissingId), Is.Empty);
            _ExpectMissing("PlayerBuff");
            Assert.That(library.GetBuffMaxLevel(MissingId), Is.Zero);
            _ExpectMissing("PlayerBuff");
            Assert.That(library.GetBuffLifeTime(MissingId), Is.Null);
            _ExpectMissing("PlayerBuff");
            Assert.That(library.GetBuffProperties(MissingId), Is.Empty);
        }

        [Test]
        public void CharacterBuffMissing_PreservesNullSessionsAndEmptyProperties()
        {
            var library = new CharacterBuffLibrary(new Dictionary<string, CharacterBuffData>());
            _ExpectMissing("CharacterBuff");
            Assert.That(library.GetBuffEffects(MissingId, default).HasValue, Is.False);
            _ExpectMissing("CharacterBuff");
            Assert.That(library.GetBuffSessions(MissingId), Is.Null);
            _ExpectMissing("CharacterBuff");
            Assert.That(library.GetBuffLifeTime(MissingId), Is.Null);
            _ExpectMissing("CharacterBuff");
            Assert.That(library.GetBuffProperties(MissingId), Is.Empty);
        }

        [Test]
        public void CardBuffMissing_PreservesNullDataAndNoneEffects()
        {
            var library = new CardBuffLibrary(new Dictionary<string, CardBuffData>());
            _ExpectMissing("CardBuff");
            Assert.That(library.GetCardBuffData(MissingId), Is.Null);
            _ExpectMissing("CardBuff");
            Assert.That(library.GetBuffEffects(MissingId, default).HasValue, Is.False);
        }

        private static void _ExpectMissing(string kind) =>
            LogAssert.Expect(LogType.Error, $"{kind} ID[{MissingId}] not found in library.");
    }
}
