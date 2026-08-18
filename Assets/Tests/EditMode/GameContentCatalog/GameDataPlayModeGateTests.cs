using System;
using MortalGame.Editor;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class GameDataPlayModeGateTests
    {
        [Test]
        public void CanEnterPlayMode_WhenContentIsValid_ReturnsTrue()
        {
            Assert.That(
                GameDataPlayModeGate.CanEnterPlayMode(Array.Empty<string>()),
                Is.True);
        }

        [Test]
        public void CanEnterPlayMode_WhenContentHasErrors_ReturnsFalse()
        {
            Assert.That(
                GameDataPlayModeGate.CanEnterPlayMode(
                    new[] { "Localization 缺少 Card ID：Attack" }),
                Is.False);
        }

        [Test]
        public void CanEnterPlayMode_WhenProjectContentIsValid_ReturnsTrue()
        {
            Assert.That(
                GameDataPlayModeGate.CanEnterPlayMode(
                    GameDataValidator.ValidateAll()),
                Is.True);
        }
    }
}
