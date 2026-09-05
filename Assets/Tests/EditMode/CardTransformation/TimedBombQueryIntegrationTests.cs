using System.Collections.Generic;
using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests.CardTransformation
{
    public sealed class TimedBombQueryIntegrationTests
    {
        private const string TimedBombCardId = "t019-timed-bomb-form";

        [Test]
        public void TimedBombComposition_OnOwnersExecuteEndInHand_ResolvesTargetAndEffectivePower()
        {
            var bombForm = new OverrideCardData
            {
                ID = TimedBombCardId,
                Cost = 5,
                Power = 10
            };
            var built = new CardTransformationTestBuilder()
                .WithCard(bombForm)
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            var applyResult = built.Card.TryApplyOverrideForm(
                "timed-bomb",
                TimedBombCardId,
                SystemSource.Instance,
                new List<CardFormOverrideReleaseRule>(),
                new Dictionary<string, IReactionSessionEntity>());
            var context = new TriggerContext(
                built.Gameplay.Manager,
                new CardTrigger(built.Card),
                new UpdateTimingAction(
                    GameTiming.AfterExecuteEnd,
                    new SystemExectueEndSource(built.Gameplay.Ally)));
            var target = _CreateTimedBombTarget();
            var value = _CreateTimedBombValue();
            var conditions = _CreateTimedBombConditions();

            Assert.That(applyResult.Status, Is.EqualTo(CardFormOperationStatus.Applied));
            using (built.Gameplay.Status.SetCurrentPlayer(built.Gameplay.Ally))
            {
                Assert.That(conditions, Has.All.Matches<ICondition>(condition => condition.Eval(context)));
                Assert.That(
                    target.Eval(context).ValueOr((ICharacterEntity)null),
                    Is.SameAs(built.Gameplay.Ally.MainCharacter));
                Assert.That(value.Eval(context).ValueOr(-1), Is.EqualTo(10));

                built.Gameplay.Ally.CardManager.HandCard.RemoveCard(built.Card);
                built.Gameplay.Ally.CardManager.Graveyard.AddCard(built.Card);

                Assert.That(conditions[2].Eval(context), Is.False);
            }

            built.Gameplay.Ally.CardManager.Graveyard.RemoveCard(built.Card);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            using (built.Gameplay.Status.SetCurrentPlayer(built.Gameplay.Enemy))
            {
                Assert.That(conditions[1].Eval(context), Is.False);
            }
        }

        [Test]
        public void TimedBombDataGraph_AssetRoundTripAndValidator_PreserveCompleteComposition()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/CardTransformation/TimedBombCompositionRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                asset.Data.ID = "t019-timed-bomb-composition";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new SingleCharacterCollection
                    {
                        Target = _CreateTimedBombTarget()
                    },
                    Value = _CreateTimedBombValue()
                });
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "timed-bomb-query-graph",
                    TransformKey = "timed-bomb",
                    Timing = GameTiming.AfterExecuteEnd,
                    Conditions = _CreateTimedBombConditions(),
                    Operation = new RevertCardTransformOperationData()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DamageEffect;
                var loadedTarget = (loadedEffect?.Targets as SingleCharacterCollection)?.Target
                    as MainCharacterOfPlayer;
                var loadedOwner = loadedTarget?.Player as CardOwner;
                var loadedValue = loadedEffect?.Value as CardIntegerProperty;
                var loadedConditions = loaded.Data.TransformRules[0].Conditions;
                var loadedContains = loadedConditions[2] as CardCollectionContainsCondition;
                var loadedCards = loadedContains?.CardCollection as CardsOfPlayer;

                Assert.That(loadedTarget, Is.Not.Null);
                Assert.That(loadedOwner?.Card, Is.TypeOf<TriggeredCard>());
                Assert.That(loadedValue?.Card, Is.TypeOf<TriggeredCard>());
                Assert.That(
                    loadedValue?.Property,
                    Is.EqualTo(CardIntegerProperty.CardIntegerValueType.CardPower));
                Assert.That(loadedConditions[0], Is.TypeOf<GameTimingCondition>());
                Assert.That(loadedConditions[1], Is.TypeOf<IsTriggeredOwnerTurnCondition>());
                Assert.That(loadedContains?.Card, Is.TypeOf<TriggeredCard>());
                Assert.That(loadedCards?.Player, Is.TypeOf<CardOwner>());
                Assert.That(loadedCards?.Zone, Is.EqualTo(CardCollectionType.HandCard));

                _SetCatalogCards(catalog, loaded);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
                Assert.That(
                    GameDataValidator.ValidateCardTransformRules(loaded.Data, assetPath),
                    Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void ValidateNestedContent_WithInvalidConditionGraph_ReportsAllSemanticErrors()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();

            try
            {
                asset.Data.ID = "t019-invalid-condition-graph";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "invalid-conditions",
                    TransformKey = "condition-validation",
                    Timing = GameTiming.AfterExecuteEnd,
                    Conditions =
                    {
                        new AllCondition(),
                        new AnyCondition(),
                        new InverseCondition(),
                        new GameTimingCondition { Timing = GameTiming.None },
                        new IntegerCondition
                        {
                            Value = new ConstInteger { Value = 1 },
                            Conditions =
                            {
                                new IntegerCompare
                                {
                                    Arithmetic = ArithmeticConditionType.None,
                                    CompareValue = new ConstInteger { Value = 1 }
                                }
                            }
                        },
                        null
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                _SetCatalogCards(catalog, asset);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(errors, Has.Some.Contains("AllCondition.Conditions 至少需要一項"));
                Assert.That(errors, Has.Some.Contains("AnyCondition.Conditions 至少需要一項"));
                Assert.That(errors, Has.Some.Contains(".Condition 為空"));
                Assert.That(errors, Has.Some.Contains("GameTimingCondition.Timing 必須是有效 Timing"));
                Assert.That(errors, Has.Some.Contains("IntegerCompare.Arithmetic 必須是有效比較運算"));
                Assert.That(errors, Has.Some.Contains("Conditions[5] 為空"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(asset);
            }
        }

        private static MainCharacterOfPlayer _CreateTimedBombTarget()
        {
            return new MainCharacterOfPlayer
            {
                Player = new CardOwner { Card = new TriggeredCard() }
            };
        }

        private static CardIntegerProperty _CreateTimedBombValue()
        {
            return new CardIntegerProperty
            {
                Card = new TriggeredCard(),
                Property = CardIntegerProperty.CardIntegerValueType.CardPower
            };
        }

        private static List<ICondition> _CreateTimedBombConditions()
        {
            return new List<ICondition>
            {
                new GameTimingCondition { Timing = GameTiming.AfterExecuteEnd },
                new IsTriggeredOwnerTurnCondition(),
                new CardCollectionContainsCondition
                {
                    CardCollection = new CardsOfPlayer
                    {
                        Player = new CardOwner { Card = new TriggeredCard() },
                        Zone = CardCollectionType.HandCard
                    },
                    Card = new TriggeredCard()
                }
            };
        }

        private static void _SetCatalogCards(
            GameContentCatalog catalog,
            params CardDataScriptableBase[] cards)
        {
            typeof(GameContentCatalog)
                .GetField("_cardAssets", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(catalog, cards);
        }
    }
}
