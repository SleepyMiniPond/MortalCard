using System;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public class IntegerValueOptionTests
    {
        [Test]
        public void ConstInteger_WhenValueIsZero_ReturnsSomeZero()
        {
            var result = new ConstInteger { Value = 0 }.Eval(null);

            _AssertSome(result, 0);
        }

        [Test]
        public void CardIntegerProperty_WhenTargetDoesNotExist_ReturnsNone()
        {
            var value = new CardIntegerProperty
            {
                Card = new NoneCard(),
                Property = CardIntegerProperty.CardIntegerValueType.CardPower
            };

            Assert.That(value.Eval(null).HasValue, Is.False);
        }

        [Test]
        public void ArithmeticInteger_WhenOperandIsMissing_ReturnsNone()
        {
            var value = new ArithmeticInteger
            {
                Operation = ArithmeticType.Add,
                Left = new ConstInteger { Value = 3 },
                Right = new MissingIntegerValue()
            };

            Assert.That(value.Eval(null).HasValue, Is.False);
        }

        [Test]
        public void IntegerCondition_WhenValueIsMissing_ReturnsFalse()
        {
            var condition = new IntegerCondition
            {
                Value = new MissingIntegerValue(),
                Conditions =
                {
                    new IntegerCompare
                    {
                        Arithmetic = ArithmeticConditionType.NotEqual,
                        CompareValue = new ConstInteger { Value = 0 }
                    }
                }
            };

            Assert.That(condition.Eval(null), Is.False);
        }

        [Test]
        public void GainEnergyEffect_WhenValueIsMissing_DoesNotCreateGameplayResult()
        {
            var built = new GameplayManagerTestBuilder().Build();
            using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
            var initialEnergy = built.Ally.CurrentEnergy;
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new GainEnergyIntentAction(SystemSource.Instance));
            var effect = new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new MissingIntegerValue()
            };
            var runner = new EffectQueueRunner();

            runner.Enqueue(new CardEffectQueueItem(context, effect));
            var result = runner.RunToCompletion();

            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(initialEnergy));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void CardBuffTurnLifeTime_WhenTurnIsMissing_ReturnsNone()
        {
            var data = new TurnLifeTimeCardBuffData { Turn = new MissingIntegerValue() };

            var result = CardBuffLifeTimeEntityFactory.CreateDefault().Create(data, null);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void PlayerBuffTurnLifeTime_WhenTurnIsMissing_ReturnsNone()
        {
            var data = new PlayerBuffTurnLifeTimeData { Turn = new MissingIntegerValue() };

            var result = PlayerBuffLifeTimeEntityFactory.CreateDefault().Create(data, null);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void QueryCardSubSelectionInfos_WhenSelectCountIsMissing_ReturnsNone()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "missing-count",
                SelectCount = new MissingIntegerValue()
            });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);

            var result = built.Manager.QueryCardSubSelectionInfos(card.Identity);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void SelectedCardsEvalTotalCost_WhenAllCostsExist_ReturnsSum()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.Cost = 2;
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var firstCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var secondCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(firstCard);
            built.Enemy.CardManager.HandCard.AddCard(secondCard);
            built.Enemy.SelectedCards.TryAddCard(firstCard);
            built.Enemy.SelectedCards.TryAddCard(secondCard);

            var result = built.Enemy.SelectedCards.EvalTotalCost(built.Manager);

            _AssertSome(result, 4);
        }

        private static void _AssertSome(Option<int> result, int expected)
        {
            Assert.That(result.TryGetValue(out var value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }

        [Serializable]
        private sealed class MissingIntegerValue : IIntegerValue
        {
            public Option<int> Eval(TriggerContext triggerContext)
            {
                return Option.None<int>();
            }
        }
    }
}
