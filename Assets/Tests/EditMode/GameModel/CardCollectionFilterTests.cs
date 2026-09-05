using System.Reflection;
using System.Linq;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class CardCollectionFilterTests
    {
        private const string RareAttackCardId = "t019-rare-attack";
        private const string RareDefenseCardId = "t019-rare-defense";
        private const string CommonAttackCardId = "t019-common-attack";

        [Test]
        public void Eval_WithMultipleConditions_FiltersByAndAndPreservesSourceOrder()
        {
            var built = _BuildGameplay();
            var firstMatch = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                RareAttackCardId);
            var rareDefense = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                RareDefenseCardId);
            var commonAttack = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                CommonAttackCardId);
            var secondMatch = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                RareAttackCardId);
            var sourceOrder = new[] { firstMatch, rareDefense, commonAttack, secondMatch };
            built.Ally.CardManager.HandCard.AddCards(sourceOrder);
            var context = _CreateContext(built);
            var filter = _CreateRareAttackFilter(new TriggeredPlayer());

            var result = filter.Eval(context);

            Assert.That(result, Is.EqualTo(new[] { firstMatch, secondMatch }));
            Assert.That(built.Ally.CardManager.HandCard.Cards, Is.EqualTo(sourceOrder));

            built.Ally.CardManager.HandCard.RemoveCard(firstMatch);

            Assert.That(filter.Eval(context), Is.EqualTo(new[] { secondMatch }));
        }

        [Test]
        public void Eval_WhenSourcePlayerIsMissing_ReturnsEmptyCollection()
        {
            var built = _BuildGameplay();
            var filter = _CreateRareAttackFilter(new NonePlayer());

            var result = filter.Eval(_CreateContext(built));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void AssetRoundTripAndValidator_PreservePolymorphicFilterGraph()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/CardCollectionFilterRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                asset.Data.ID = "t019-card-collection-filter";
                asset.Data.Effects.Add(new DiscardCardEffect
                {
                    TargetCards = _CreateRareAttackFilter(new PlayerByFaction
                    {
                        Faction = Faction.Ally
                    })
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DiscardCardEffect;
                var loadedFilter = loadedEffect?.TargetCards as FilteredCardCollection;
                var loadedSource = loadedFilter?.CardCollection as CardsOfPlayer;

                Assert.That(loadedFilter, Is.Not.Null);
                Assert.That(loadedSource?.Player, Is.TypeOf<PlayerByFaction>());
                Assert.That(loadedSource?.Zone, Is.EqualTo(CardCollectionType.HandCard));
                Assert.That(loadedFilter?.Conditions, Has.Count.EqualTo(2));
                Assert.That(loadedFilter?.Conditions[0], Is.TypeOf<CardTypesCondition>());
                Assert.That(loadedFilter?.Conditions[1], Is.TypeOf<CardRaritiesCondition>());

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
        public void Validator_WithEmptyOrNullFilterCondition_ReportsErrors()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();

            try
            {
                asset.Data.ID = "t019-invalid-card-collection-filter";
                asset.Data.Effects.Add(new DiscardCardEffect
                {
                    TargetCards = new FilteredCardCollection
                    {
                        CardCollection = new CardsOfPlayer
                        {
                            Player = new PlayerByFaction { Faction = Faction.Ally },
                            Zone = CardCollectionType.HandCard
                        }
                    }
                });
                asset.Data.Effects.Add(new DiscardCardEffect
                {
                    TargetCards = new FilteredCardCollection
                    {
                        CardCollection = new CardsOfPlayer
                        {
                            Player = new PlayerByFaction { Faction = Faction.Ally },
                            Zone = CardCollectionType.HandCard
                        },
                        Conditions = { null }
                    }
                });
                _SetCatalogCards(catalog, asset);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(
                    errors,
                    Has.Some.Contains("FilteredCardCollection.Conditions 至少需要一項"));
                Assert.That(errors, Has.Some.Contains("Conditions[0] 為空"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(asset);
            }
        }

        private static BuiltGameplay _BuildGameplay()
        {
            var rareAttack = CardTestBuilder.CreateCardData(RareAttackCardId);
            rareAttack.Type = CardType.Attack;
            rareAttack.Rarity = CardRarity.Rare;
            var rareDefense = CardTestBuilder.CreateCardData(RareDefenseCardId);
            rareDefense.Type = CardType.Defense;
            rareDefense.Rarity = CardRarity.Rare;
            var commonAttack = CardTestBuilder.CreateCardData(CommonAttackCardId);
            commonAttack.Type = CardType.Attack;
            commonAttack.Rarity = CardRarity.Common;

            return new GameplayManagerTestBuilder()
                .WithCard(rareAttack)
                .WithCard(rareDefense)
                .WithCard(commonAttack)
                .Build();
        }

        private static FilteredCardCollection _CreateRareAttackFilter(
            ITargetPlayerValue player)
        {
            return new FilteredCardCollection
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = player,
                    Zone = CardCollectionType.HandCard
                },
                Conditions =
                {
                    new CardTypesCondition
                    {
                        CardTypes = { CardType.Attack },
                        Condition = SetConditionType.AnyInside
                    },
                    new CardRaritiesCondition
                    {
                        CardRarities = { CardRarity.Rare },
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
