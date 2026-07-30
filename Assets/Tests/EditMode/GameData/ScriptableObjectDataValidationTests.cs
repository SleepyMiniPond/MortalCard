using System;
using System.Collections.Generic;
using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class ScriptableObjectDataValidationTests
    {
        [Test]
        public void AllEffectCommandTypes_HaveHandler()
        {
            AssertNoErrors(GameDataValidator.ValidateEffectCommandHandlers());
        }

        [Test]
        public void ScriptableObjectEffects_HaveResolvers()
        {
            AssertNoErrors(GameDataValidator.ValidateEffectResolvers());
        }

        [Test]
        public void ScriptableObjectReferenceIds_ExistInLibraries()
        {
            AssertNoErrors(GameDataValidator.ValidateReferenceIds());
        }

        [Test]
        public void ScriptableObjectReactionSessions_HaveUniqueTimingRules()
        {
            AssertNoErrors(GameDataValidator.ValidateReactionSessionRules());
        }

        [Test]
        public void ValidateReactionSessionRules_WithDuplicateBooleanAndIntegerTimings_ReturnsErrors()
        {
            var sessions = new Dictionary<string, IReactionSessionData>
            {
                {
                    "boolean-session",
                    new SessionBoolean
                    {
                        UpdateRules = new List<SessionBoolean.TimingRule>
                        {
                            new() { Timing = GameTiming.BeforeTurnEnd },
                            new() { Timing = GameTiming.BeforeTurnEnd }
                        }
                    }
                },
                {
                    "integer-session",
                    new SessionInteger
                    {
                        UpdateRules = new List<SessionInteger.TimingRule>
                        {
                            new() { Timing = GameTiming.CardPlayResult },
                            new() { Timing = GameTiming.CardPlayResult }
                        }
                    }
                }
            };

            var errors = GameDataValidator.ValidateReactionSessionRules(
                sessions,
                "Validator Test");

            Assert.That(errors.Count, Is.EqualTo(2));
            Assert.That(
                errors,
                Has.Some.Contains(
                    "Validator Test[boolean-session] 的 TimingRule 重複：BeforeTurnEnd"));
            Assert.That(
                errors,
                Has.Some.Contains(
                    "Validator Test[integer-session] 的 TimingRule 重複：CardPlayResult"));
        }

        private static void AssertNoErrors(IReadOnlyCollection<string> errors)
        {
            Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
        }
    }
}
