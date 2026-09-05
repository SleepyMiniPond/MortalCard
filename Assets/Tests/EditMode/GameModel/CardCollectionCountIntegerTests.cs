using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class CardCollectionCountIntegerTests
    {
        private const string CardId = "card-collection-count";

        [Test]
        public void Eval_ReturnsCurrentCollectionCountIncludingZero()
        {
            var built = _BuildGameplay();
            var count = _CreateHandCount(new TriggeredPlayer());
            var context = _CreateContext(built);

            _AssertInteger(count, context, 0);

            built.Ally.CardManager.HandCard.AddCards(new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId)
            });

            _AssertInteger(count, context, 2);
        }

        [Test]
        public void Eval_ComposesWithArithmeticAndIntegerComparison()
        {
            var built = _BuildGameplay();
            built.Ally.CardManager.HandCard.AddCards(new[]
            {
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId),
                CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId)
            });
            var context = _CreateContext(built);
            var adjustedCount = new ArithmeticInteger
            {
                Operation = ArithmeticType.Subtract,
                Left = _CreateHandCount(new TriggeredPlayer()),
                Right = new ConstInteger { Value = 1 }
            };
            var condition = new IntegerCondition
            {
                Value = adjustedCount,
                Conditions =
                {
                    new IntegerCompare
                    {
                        Arithmetic = ArithmeticConditionType.Equal,
                        CompareValue = new ConstInteger { Value = 2 }
                    }
                }
            };

            _AssertInteger(adjustedCount, context, 2);
            Assert.That(condition.Eval(context), Is.True);
        }

        [Test]
        public void AssetRoundTripAndValidator_PreserveCountGraph()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/CardCollectionCountRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                asset.Data.ID = "card-collection-count-round-trip";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "card-collection-count",
                    TransformKey = "query",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new IntegerCondition
                        {
                            Value = _CreateHandCount(new PlayerByFaction
                            {
                                Faction = Faction.Ally
                            }),
                            Conditions =
                            {
                                new IntegerCompare
                                {
                                    Arithmetic = ArithmeticConditionType.GreaterThanOrEqual,
                                    CompareValue = new ConstInteger { Value = 3 }
                                }
                            }
                        }
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedCondition = loaded.Data.TransformRules[0].Conditions[0]
                    as IntegerCondition;
                var loadedCount = loadedCondition?.Value as CardCollectionCountInteger;
                var loadedCards = loadedCount?.CardCollection as CardsOfPlayer;

                Assert.That(loadedCount, Is.Not.Null);
                Assert.That(loadedCards?.Player, Is.TypeOf<PlayerByFaction>());
                Assert.That(loadedCards?.Zone, Is.EqualTo(CardCollectionType.HandCard));
                Assert.That(loadedCondition?.Conditions[0], Is.TypeOf<IntegerCompare>());

                _SetCatalogCards(catalog, loaded);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void Validator_WhenCardCollectionIsMissing_ReportsError()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();

            try
            {
                asset.Data.ID = "invalid-card-collection-count";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "invalid-card-collection-count",
                    TransformKey = "query",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new IntegerCondition
                        {
                            Value = new CardCollectionCountInteger(),
                            Conditions =
                            {
                                new IntegerCompare
                                {
                                    Arithmetic = ArithmeticConditionType.Equal,
                                    CompareValue = new ConstInteger { Value = 0 }
                                }
                            }
                        }
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                _SetCatalogCards(catalog, asset);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(
                    errors,
                    Has.Some.Contains("Conditions[0].Value.CardCollection 為空"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(asset);
            }
        }

        private static BuiltGameplay _BuildGameplay()
        {
            return new GameplayManagerTestBuilder()
                .WithCard(CardTestBuilder.CreateCardData(CardId))
                .Build();
        }

        private static CardCollectionCountInteger _CreateHandCount(
            ITargetPlayerValue player)
        {
            return new CardCollectionCountInteger
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = player,
                    Zone = CardCollectionType.HandCard
                }
            };
        }

        private static TriggerContext _CreateContext(BuiltGameplay built)
        {
            return new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnStart, SystemSource.Instance));
        }

        private static void _AssertInteger(
            IIntegerValue integer,
            TriggerContext context,
            int expected)
        {
            Assert.That(integer.Eval(context).TryGetValue(out var value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
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
