using System;
using System.Collections.Generic;
using MortalGame.Editor;
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

        private static void AssertNoErrors(IReadOnlyCollection<string> errors)
        {
            Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
        }
    }
}
