using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests.T010
{
    public sealed class T019SliceOneIntegrationTests
    {
        [Test]
        public void SwordShieldRules_InOwnersHand_AlternateOnEvenAndOddTurnStarts()
        {
            var sword = CreateSwordWithTransformRules();
            var shield = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.AlternateCardId,
                cost: 2,
                power: 5);
            shield.Type = CardType.Defense;
            var built = new CardTransformationTestBuilder()
                .WithCard(sword)
                .WithCard(shield)
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);

            built.Gameplay.Status.SetNewTurn();
            var firstTurnEvents = built.Gameplay.Manager
                .TriggerTiming(GameTiming.AfterTurnStart, SystemSource.Instance)
                .ToArray();
            built.Gameplay.Status.SetNewTurn();
            var secondTurnEvents = built.Gameplay.Manager
                .TriggerTiming(GameTiming.AfterTurnStart, SystemSource.Instance)
                .ToArray();
            built.Gameplay.Status.SetNewTurn();
            var thirdTurnEvents = built.Gameplay.Manager
                .TriggerTiming(GameTiming.AfterTurnStart, SystemSource.Instance)
                .ToArray();

            Assert.That(firstTurnEvents.OfType<CardFormChangedEvent>(), Is.Empty);
            Assert.That(secondTurnEvents.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            Assert.That(
                secondTurnEvents.OfType<CardFormChangedEvent>().Single().AfterCardDataId,
                Is.EqualTo(CardTransformationTestBuilder.AlternateCardId));
            Assert.That(thirdTurnEvents.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            Assert.That(
                thirdTurnEvents.OfType<CardFormChangedEvent>().Single().AfterCardDataId,
                Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        [Test]
        public void SwordShieldRules_WhenCardIsOutsideOwnersHand_DoNotTransform()
        {
            var sword = CreateSwordWithTransformRules();
            var shield = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.AlternateCardId,
                cost: 2,
                power: 5);
            var built = new CardTransformationTestBuilder()
                .WithCard(sword)
                .WithCard(shield)
                .Build();
            built.Gameplay.Ally.CardManager.Graveyard.AddCard(built.Card);
            built.Gameplay.Status.SetNewTurn();
            built.Gameplay.Status.SetNewTurn();

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.AfterTurnStart, SystemSource.Instance)
                .ToArray();

            Assert.That(events.OfType<CardFormChangedEvent>(), Is.Empty);
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        internal static StandardCardData CreateSwordWithTransformRules()
        {
            var sword = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                cost: 1,
                power: 3);
            sword.PropertyDatas.Add(new PreservedPropertyData());
            sword.TransformRules.Add(_CreateRule(
                "sword-to-shield",
                ArithmeticConditionType.Equal,
                new ApplyCardTransformOperationData
                {
                    TargetCardDataId = CardTransformationTestBuilder.AlternateCardId,
                    Persistence = CardFormPersistence.Persistent
                }));
            sword.TransformRules.Add(_CreateRule(
                "shield-to-sword",
                ArithmeticConditionType.NotEqual,
                new RevertCardTransformOperationData()));
            return sword;
        }

        private static CardTransformRule _CreateRule(
            string ruleId,
            ArithmeticConditionType parityComparison,
            ICardTransformOperationData operation)
        {
            return new CardTransformRule
            {
                RuleId = ruleId,
                TransformKey = "sword-shield",
                Timing = GameTiming.AfterTurnStart,
                Conditions =
                {
                    new CardCollectionContainsCondition
                    {
                        CardCollection = new CardsOfPlayer
                        {
                            Player = new CardOwner { Card = new TriggeredCard() },
                            Zone = CardCollectionType.HandCard
                        },
                        Card = new TriggeredCard()
                    },
                    new IntegerCondition
                    {
                        Value = new ArithmeticInteger
                        {
                            Operation = ArithmeticType.Remainder,
                            Left = new TurnCountInteger(),
                            Right = new ConstInteger { Value = 2 }
                        },
                        Conditions =
                        {
                            new IntegerCompare
                            {
                                Arithmetic = parityComparison,
                                CompareValue = new ConstInteger { Value = 0 }
                            }
                        }
                    }
                },
                Operation = operation
            };
        }
    }
}
