using System.Linq;
using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class CardCollectionIndexTargetTests
    {
        private const string CardId = "card-collection-index";

        [Test]
        public void Eval_WithAscendingAndDescendingOrder_ReturnsIndexedCard()
        {
            var built = _BuildGameplay();
            var first = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId);
            var second = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId);
            var third = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId);
            built.Ally.CardManager.HandCard.AddCards(new[] { first, second, third });
            var context = _CreateContext(built);

            var ascending = _CreateIndex(OrderType.Ascending, 0);
            var descending = _CreateIndex(OrderType.Descending, 0);

            Assert.That(ascending.Eval(context).ValueOr((ICardEntity)null), Is.SameAs(first));
            Assert.That(descending.Eval(context).ValueOr((ICardEntity)null), Is.SameAs(third));
        }

        [Test]
        public void Eval_WithRandomOrder_ReturnsNoneWithoutAdvancingGameRandom()
        {
            var built = _BuildGameplay();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            var context = _CreateContext(built);
            var target = _CreateIndex(OrderType.Random, 0);
            var expectedRandom = new GameRandom(seed: 1);

            Assert.That(target.Eval(context).HasValue, Is.False);
            Assert.That(
                built.ContextManager.GameRandom.Range(0, 100),
                Is.EqualTo(expectedRandom.Range(0, 100)));
        }

        [Test]
        public void AssetRoundTripAndValidator_PreserveIndexGraph()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/CardCollectionIndexRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                asset.Data.ID = "card-collection-index-round-trip";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "card-collection-index",
                    TransformKey = "query",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new CardCondition
                        {
                            Card = _CreateIndex(
                                OrderType.Descending,
                                1,
                                new PlayerByFaction { Faction = Faction.Ally }),
                            Conditions =
                            {
                                new CardTypesCondition
                                {
                                    CardTypes = { CardType.Attack },
                                    Condition = SetConditionType.AnyInside
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
                    as CardCondition;
                var loadedIndex = loadedCondition?.Card as IndexOfCardCollection;
                var loadedCards = loadedIndex?.CardCollection as CardsOfPlayer;

                Assert.That(loadedIndex, Is.Not.Null);
                Assert.That(loadedIndex?.Order, Is.EqualTo(OrderType.Descending));
                Assert.That(loadedIndex?.Index, Is.TypeOf<ConstInteger>());
                Assert.That(loadedCards?.Player, Is.TypeOf<PlayerByFaction>());
                Assert.That(loadedCards?.Zone, Is.EqualTo(CardCollectionType.HandCard));

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
        public void Validator_WithInvalidIndexOrder_ReportsErrors()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();

            try
            {
                asset.Data.ID = "invalid-card-collection-index";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "invalid-card-collection-index",
                    TransformKey = "query",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        _CreateCardCondition(OrderType.None),
                        _CreateCardCondition(OrderType.Random),
                        _CreateCardCondition((OrderType)999)
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                _SetCatalogCards(catalog, asset);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(
                    errors.Count(error =>
                        error.Contains("IndexOfCardCollection.Order 必須是 Ascending 或 Descending")),
                    Is.EqualTo(3));
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

        private static IndexOfCardCollection _CreateIndex(
            OrderType order,
            int index,
            ITargetPlayerValue player = null)
        {
            return new IndexOfCardCollection
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = player ?? new TriggeredPlayer(),
                    Zone = CardCollectionType.HandCard
                },
                Index = new ConstInteger { Value = index },
                Order = order
            };
        }

        private static CardCondition _CreateCardCondition(OrderType order)
        {
            return new CardCondition
            {
                Card = _CreateIndex(order, 0),
                Conditions =
                {
                    new CardTypesCondition
                    {
                        CardTypes = { CardType.Attack },
                        Condition = SetConditionType.AnyInside
                    }
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
