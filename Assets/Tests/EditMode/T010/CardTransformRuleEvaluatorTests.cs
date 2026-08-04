using System;
using System.Collections.Generic;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests.T010
{
    public class CardTransformRuleEvaluatorTests
    {
        [Test]
        public void Evaluate_EvenTurnApplyRule_ReturnsOperationOnlyOnEvenTurn()
        {
            var cardData = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                2,
                3);
            cardData.TransformRules.Add(CreateRule(
                "even-apply",
                priority: 0,
                condition: new TurnParityCondition(isEven: true)));
            var built = new CardTransformationTestBuilder()
                .WithCard(cardData)
                .Build();
            var evenResult = CardTransformRuleEvaluator.Evaluate(
                GameTiming.BeforeTurnStart,
                CreateContext(built));
            built.Gameplay.Status.SetNewTurn();
            var oddResult = CardTransformRuleEvaluator.Evaluate(
                GameTiming.BeforeTurnStart,
                CreateContext(built));

            Assert.That(evenResult.ValueOr((CardFormOperation)null).RuleId, Is.EqualTo("even-apply"));
            Assert.That(oddResult.HasValue, Is.False);
        }

        [Test]
        public void Evaluate_EnergyZeroRevertRule_ReturnsRevertOperation()
        {
            var cardData = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                2,
                3);
            cardData.TransformRules.Add(CreateRule(
                "energy-zero-revert",
                priority: 0,
                operation: new RevertCardTransformOperationData(),
                condition: new AllyEnergyCondition(expectedEnergy: 0)));
            var built = new CardTransformationTestBuilder()
                .WithCard(cardData)
                .Build();

            var result = CardTransformRuleEvaluator.Evaluate(
                GameTiming.BeforeTurnStart,
                CreateContext(built));

            Assert.That(result.ValueOr((CardFormOperation)null),
                Is.TypeOf<RevertCardFormOperation>());
        }

        [Test]
        public void Evaluate_PriorityThenArrayIndex_SelectsOnlyFirstMatchingRule()
        {
            var cardData = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                2,
                3);
            cardData.TransformRules.Add(CreateRule("low", priority: 1));
            cardData.TransformRules.Add(CreateRule("high-first", priority: 10));
            cardData.TransformRules.Add(CreateRule("high-second", priority: 10));
            var built = new CardTransformationTestBuilder()
                .WithCard(cardData)
                .Build();

            var result = CardTransformRuleEvaluator.Evaluate(
                GameTiming.BeforeTurnStart,
                CreateContext(built));

            Assert.That(result.ValueOr((CardFormOperation)null).RuleId, Is.EqualTo("high-first"));
        }

        [Test]
        public void Evaluate_AfterSelfFormChanges_StillReadsRulesFromBaseCardData()
        {
            var baseCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                2,
                3);
            baseCard.TransformRules.Add(CreateRule("base-rule", priority: 0));
            var alternateCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.AlternateCardId,
                5,
                8);
            alternateCard.TransformRules.Add(CreateRule("alternate-rule", priority: 100));
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .WithCard(alternateCard)
                .Build();
            built.Card.TryApplySelfForm(
                "alternate",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.Persistent);

            var result = CardTransformRuleEvaluator.Evaluate(
                GameTiming.BeforeTurnStart,
                CreateContext(built));

            Assert.That(result.ValueOr((CardFormOperation)null).RuleId, Is.EqualTo("base-rule"));
        }

        [Test]
        public void Evaluate_WhenSelfTransformIsSuppressed_ReturnsNoOperation()
        {
            var cardData = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                2,
                3);
            cardData.TransformRules.Add(CreateRule("apply", priority: 0));
            var built = new CardTransformationTestBuilder()
                .WithCard(cardData)
                .Build();

            var result = CardTransformRuleEvaluator.Evaluate(
                GameTiming.BeforeTurnStart,
                CreateContext(built, isSuppressed: true));

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void ValidateCardTransformRules_WithMultipleTransformKeys_ReturnsError()
        {
            var cardData = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                2,
                3);
            cardData.TransformRules.Add(CreateRule("first", priority: 0, transformKey: "first-key"));
            cardData.TransformRules.Add(CreateRule("second", priority: 0, transformKey: "second-key"));

            var errors = GameDataValidator.ValidateCardTransformRules(cardData, "T-010 Test");

            Assert.That(errors, Has.Some.Contains("只能使用一個 TransformKey"));
        }

        private static CardTransformRule CreateRule(
            string ruleId,
            int priority,
            string transformKey = "alternate",
            ICardTransformOperationData operation = null,
            ICondition condition = null)
        {
            return new CardTransformRule
            {
                RuleId = ruleId,
                TransformKey = transformKey,
                Priority = priority,
                Timing = GameTiming.BeforeTurnStart,
                Conditions = new List<ICondition>
                {
                    condition ?? new ConstCondition { Value = true }
                },
                Operation = operation ?? new ApplyCardTransformOperationData
                {
                    TargetCardDataId = CardTransformationTestBuilder.AlternateCardId,
                    Persistence = CardFormPersistence.Persistent
                }
            };
        }

        private static CardFormRuleContext CreateContext(
            BuiltCardTransformationTest built,
            bool isSuppressed = false)
        {
            return new CardFormRuleContext(
                built.Gameplay.Manager,
                built.Card,
                new UpdateTimingAction(GameTiming.BeforeTurnStart, SystemSource.Instance),
                isSuppressed);
        }

        private sealed class TurnParityCondition : ICondition
        {
            private readonly bool _isEven;

            public TurnParityCondition(bool isEven)
            {
                _isEven = isEven;
            }

            public bool Eval(TriggerContext triggerContext)
            {
                return (triggerContext.Model.GameStatus.TurnCount % 2 == 0) == _isEven;
            }
        }

        private sealed class AllyEnergyCondition : ICondition
        {
            private readonly int _expectedEnergy;

            public AllyEnergyCondition(int expectedEnergy)
            {
                _expectedEnergy = expectedEnergy;
            }

            public bool Eval(TriggerContext triggerContext)
            {
                return triggerContext.Model.GameStatus.Ally.CurrentEnergy == _expectedEnergy;
            }
        }
    }
}
