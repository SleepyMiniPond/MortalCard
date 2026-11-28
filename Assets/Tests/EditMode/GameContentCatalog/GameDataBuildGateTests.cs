using System;
using MortalGame.Editor.Exceptions;
using MortalGame.Editor;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class GameDataBuildGateTests
    {
        [Test]
        public void EnsureValid_WhenContentIsValid_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => GameDataBuildGate.EnsureValid(Array.Empty<ValidationIssue>()));
        }

        [Test]
        public void EnsureValid_WhenContentHasErrors_StopsBuildWithAllDetails()
        {
            var errors = new ValidationIssue[]
            {
                new ValidationIssue.DuplicateContentId("Card", "Attack"),
                new ValidationIssue.UnknownReferenceId("Localization", "Defense")
            };
            var exception = Assert.Throws<GameDataValidationBuildFailedException>(
                () => GameDataBuildGate.EnsureValid(errors));
            errors[0] = new ValidationIssue.MissingRequiredValue("外部清單已修改");

            Assert.That(exception.ValidationErrors, Is.EqualTo(new ValidationIssue[]
            {
                new ValidationIssue.DuplicateContentId("Card", "Attack"),
                new ValidationIssue.UnknownReferenceId("Localization", "Defense")
            }));
            Assert.That(exception.Message, Does.Contain("共 2 個錯誤"));
            Assert.That(exception.Message, Does.Contain("Card.ID 重複：Attack"));
            Assert.That(exception.Message, Does.Contain("Localization 指向不存在的 ID：Defense"));
        }

        [Test]
        public void BuildPreprocessor_WhenProjectContentIsValid_DoesNotThrow()
        {
            Assert.DoesNotThrow(
                () => new GameDataBuildPreprocessor().OnPreprocessBuild(null));
        }
    }
}
