using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class CardCollectionQuantifierConditionTests
    {
        private const string RareAttackCardId = "t019-quantifier-rare-attack";
        private const string RareDefenseCardId = "t019-quantifier-rare-defense";
        private const string CommonAttackCardId = "t019-quantifier-common-attack";

        [Test]
        public void AnyAndAll_WithCardConditions_UseElementAndSemantics()
        {
            var built = _BuildGameplay();
            var rareAttack = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                RareAttackCardId);
            var rareDefense = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                RareDefenseCardId);
            var commonAttack = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                CommonAttackCardId);
            built.Ally.CardManager.HandCard.AddCards(
                new[] { rareAttack, rareDefense, commonAttack });
            var context = _CreateContext(built);
            var source = _CreateHandCards();
            var anyRareAttack = new CardCollectionAnyCondition
            {
                CardCollection = source,
                Conditions =
                {
                    _CreateTypeCondition(CardType.Attack),
                    _CreateRarityCondition(CardRarity.Rare)
                }
            };
            var allRare = new CardCollectionAllCondition
            {
                CardCollection = source,
                Conditions = { _CreateRarityCondition(CardRarity.Rare) }
            };

            Assert.That(anyRareAttack.Eval(context), Is.True);
            Assert.That(allRare.Eval(context), Is.False);

            built.Ally.CardManager.HandCard.RemoveCard(commonAttack);

            Assert.That(allRare.Eval(context), Is.True);
        }

        [Test]
        public void AnyAndAll_WhenCollectionIsEmpty_BothReturnFalse()
        {
            var built = _BuildGameplay();
            var context = _CreateContext(built);
            var emptySource = new CardsOfPlayer
            {
                Player = new NonePlayer(),
                Zone = CardCollectionType.HandCard
            };
            var any = new CardCollectionAnyCondition
            {
                CardCollection = emptySource,
                Conditions = { _CreateTypeCondition(CardType.Attack) }
            };
            var all = new CardCollectionAllCondition
            {
                CardCollection = emptySource,
                Conditions = { _CreateTypeCondition(CardType.Attack) }
            };

            Assert.That(any.Eval(context), Is.False);
            Assert.That(all.Eval(context), Is.False);
        }

        [Test]
        public void AssetRoundTripAndValidator_PreserveAnyAndAllGraphs()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/CardCollectionQuantifierRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                asset.Data.ID = "t019-card-collection-quantifiers";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "card-collection-quantifiers",
                    TransformKey = "query",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new CardCollectionAnyCondition
                        {
                            CardCollection = _CreateHandCards(Faction.Ally),
                            Conditions = { _CreateTypeCondition(CardType.Attack) }
                        },
                        new CardCollectionAllCondition
                        {
                            CardCollection = _CreateHandCards(Faction.Ally),
                            Conditions = { _CreateRarityCondition(CardRarity.Rare) }
                        }
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedAny = loaded.Data.TransformRules[0].Conditions[0]
                    as CardCollectionAnyCondition;
                var loadedAll = loaded.Data.TransformRules[0].Conditions[1]
                    as CardCollectionAllCondition;

                Assert.That(loadedAny?.CardCollection, Is.TypeOf<CardsOfPlayer>());
                Assert.That(loadedAny?.Conditions[0], Is.TypeOf<CardTypesCondition>());
                Assert.That(loadedAll?.CardCollection, Is.TypeOf<CardsOfPlayer>());
                Assert.That(loadedAll?.Conditions[0], Is.TypeOf<CardRaritiesCondition>());

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
        public void Validator_WithEmptyOrNullQuantifierCondition_ReportsErrors()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();

            try
            {
                asset.Data.ID = "t019-invalid-card-collection-quantifiers";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "invalid-card-collection-quantifiers",
                    TransformKey = "query",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new CardCollectionAnyCondition
                        {
                            CardCollection = _CreateHandCards(Faction.Ally)
                        },
                        new CardCollectionAllCondition
                        {
                            CardCollection = _CreateHandCards(Faction.Ally),
                            Conditions = { null }
                        }
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                _SetCatalogCards(catalog, asset);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(
                    errors,
                    Has.Some.Contains("CardCollectionAnyCondition.Conditions 至少需要一項"));
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

        private static CardsOfPlayer _CreateHandCards()
        {
            return new CardsOfPlayer
            {
                Player = new TriggeredPlayer(),
                Zone = CardCollectionType.HandCard
            };
        }

        private static CardsOfPlayer _CreateHandCards(Faction faction)
        {
            return new CardsOfPlayer
            {
                Player = new PlayerByFaction { Faction = faction },
                Zone = CardCollectionType.HandCard
            };
        }

        private static CardTypesCondition _CreateTypeCondition(CardType type)
        {
            return new CardTypesCondition
            {
                CardTypes = { type },
                Condition = SetConditionType.AnyInside
            };
        }

        private static CardRaritiesCondition _CreateRarityCondition(CardRarity rarity)
        {
            return new CardRaritiesCondition
            {
                CardRarities = { rarity },
                Condition = SetConditionType.AnyInside
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
