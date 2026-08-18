using System;
using MortalGame.Editor;
using NUnit.Framework;
using UnityEditor.Build;

namespace MortalGame.Tests
{
    public sealed class GameDataBuildGateTests
    {
        [Test]
        public void EnsureValid_WhenContentIsValid_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => GameDataBuildGate.EnsureValid(Array.Empty<string>()));
        }

        [Test]
        public void EnsureValid_WhenContentHasErrors_StopsBuildWithAllDetails()
        {
            var exception = Assert.Throws<BuildFailedException>(
                () => GameDataBuildGate.EnsureValid(
                    new[]
                    {
                        "Card ID 重複：Attack",
                        "Localization 缺少 Card ID：Defense"
                    }));

            Assert.That(exception.Message, Does.Contain("共 2 個錯誤"));
            Assert.That(exception.Message, Does.Contain("Card ID 重複：Attack"));
            Assert.That(
                exception.Message,
                Does.Contain("Localization 缺少 Card ID：Defense"));
        }

        [Test]
        public void BuildPreprocessor_WhenProjectContentIsValid_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => new GameDataBuildPreprocessor().OnPreprocessBuild(null));
        }
    }
}
