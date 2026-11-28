using System;
using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests.CardTransformation
{
    public sealed class CardFormRejectionTests
    {
        [TestCase(null, "target")]
        [TestCase(" ", "target")]
        [TestCase("self", null)]
        [TestCase("self", " ")]
        public void SelfForm_MissingInputReturnsTypedRejectionWithoutChangingCard(string key, string target)
        {
            var built = new CardTransformationTestBuilder().Build();
            var result = built.Card.TryApplySelfForm(key, target, CardFormPersistence.Persistent);

            Assert.That(result.Rejection, Is.TypeOf<CardFormRejection.InvalidSelfFormInput>());
            _AssertRejectedWithoutChangingCard(result, built);
        }

        [TestCase("key")]
        [TestCase("target")]
        [TestCase("source")]
        [TestCase("rules")]
        [TestCase("sessions")]
        public void Override_MissingInputReturnsTypedRejectionWithoutCreatingLayer(string missing)
        {
            var built = new CardTransformationTestBuilder().Build();
            var layer = built.Card.BuffManager.ActiveLayerHandle;
            var result = built.Card.TryApplyOverrideForm(
                missing == "key" ? "" : "override",
                missing == "target" ? "" : "target",
                missing == "source" ? null : SystemSource.Instance,
                missing == "rules" ? null : Array.Empty<CardFormOverrideReleaseRule>(),
                missing == "sessions" ? null : new Dictionary<string, IReactionSessionEntity>());

            Assert.That(result.Rejection, Is.TypeOf<CardFormRejection.InvalidOverrideInput>());
            _AssertRejectedWithoutChangingCard(result, built);
            Assert.That(built.Card.BuffManager.ActiveLayerHandle, Is.SameAs(layer));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingTarget_PreservesRequestedIdAndDoesNotChangeCard(bool externalOverride)
        {
            const string target = "missing-form-target";
            var built = new CardTransformationTestBuilder().Build();
            LogAssert.Expect(LogType.Error, $"Card ID[{target}] not found in library.");
            LogAssert.Expect(LogType.Error, $"Card ID[{target}] 不是 {(externalOverride ? "Override" : "Standard")} CardData。");
            var result = externalOverride
                ? built.Card.TryApplyOverrideForm("override", target, SystemSource.Instance,
                    Array.Empty<CardFormOverrideReleaseRule>(), new Dictionary<string, IReactionSessionEntity>())
                : built.Card.TryApplySelfForm("self", target, CardFormPersistence.Persistent);

            if (externalOverride)
            {
                Assert.That(result.Rejection, Is.TypeOf<CardFormRejection.MissingOverrideCardData>());
                Assert.That(((CardFormRejection.MissingOverrideCardData)result.Rejection).TargetCardDataId,
                    Is.EqualTo(target));
            }
            else
            {
                Assert.That(result.Rejection, Is.TypeOf<CardFormRejection.MissingSelfFormCardData>());
                Assert.That(((CardFormRejection.MissingSelfFormCardData)result.Rejection).TargetCardDataId,
                    Is.EqualTo(target));
            }
            Assert.That(result.Rejection.Message, Does.Contain(target));
            _AssertRejectedWithoutChangingCard(result, built);
        }

        [Test]
        public void SelfForm_SuccessAndRepeatedNoOpDoNotCarryRejection()
        {
            var built = new CardTransformationTestBuilder().Build();
            var applied = built.Card.TryApplySelfForm("self", CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);
            var repeated = built.Card.TryApplySelfForm("self", CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);
            var reverted = built.Card.TryRevertSelfForm("self");

            Assert.That(applied.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(repeated.Status, Is.EqualTo(CardFormOperationStatus.NoOp));
            Assert.That(reverted.Status, Is.EqualTo(CardFormOperationStatus.Reverted));
            Assert.That(applied.Rejection, Is.Null);
            Assert.That(repeated.Rejection, Is.Null);
            Assert.That(reverted.Rejection, Is.Null);
        }

        private static void _AssertRejectedWithoutChangingCard(CardFormOperationResult result, BuiltCardTransformationTest built)
        {
            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.Rejected));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.BeforeCardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(result.AfterCardDataId, Is.EqualTo(result.BeforeCardDataId));
            Assert.That(built.Card.CardDataId, Is.EqualTo(result.BeforeCardDataId));
            Assert.That(built.Card.SelfFormState.HasValue, Is.False);
            Assert.That(built.Card.OverrideFormState.HasValue, Is.False);
        }
    }
}
