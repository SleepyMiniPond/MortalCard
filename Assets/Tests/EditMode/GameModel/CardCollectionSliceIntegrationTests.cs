using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class CardCollectionSliceIntegrationTests
    {
        private const string AttackCardId = "collection-integration-attack";
        private const string DefenseCardId = "collection-integration-defense";
        private const string PreservedCardId = "collection-integration-preserved";
        private const string SealedCardBuffId = "collection-integration-sealed";

        [Test]
        public void GraveyardAttackCount_WhenThirdAttackIsAdded_BecomesTrue()
        {
            var built = _BuildGameplay();
            var context = _CreateContext(built);
            var condition = _CreateGraveyardAtLeastThreeAttack(new TriggeredPlayer());
            built.Ally.CardManager.Graveyard.AddCards(new[]
            {
                _CreateCard(built, AttackCardId),
                _CreateCard(built, AttackCardId),
                _CreateCard(built, DefenseCardId)
            });

            Assert.That(condition.Eval(context), Is.False);

            built.Ally.CardManager.Graveyard.AddCard(_CreateCard(built, AttackCardId));

            Assert.That(condition.Eval(context), Is.True);
        }

        [Test]
        public void AllPreservedHandCardsUnsealed_OnlyEvaluatesPreservedCards()
        {
            var built = _BuildGameplay();
            var context = _CreateContext(built);
            var preserved = _CreateCard(built, PreservedCardId);
            var unpreserved = _CreateCard(built, DefenseCardId);
            built.Ally.CardManager.HandCard.AddCards(new[] { preserved, unpreserved });
            var condition = _CreateAllPreservedHandCardsUnsealed(new TriggeredPlayer());

            unpreserved.BuffManager.AddBuff(_CreateSealedBuff(built, context));

            Assert.That(condition.Eval(context), Is.True);

            preserved.BuffManager.AddBuff(_CreateSealedBuff(built, context));

            Assert.That(condition.Eval(context), Is.False);
        }

        [Test]
        public void FilterCountAndArithmetic_ComposeIntoIntegerCondition()
        {
            var built = _BuildGameplay();
            var context = _CreateContext(built);
            var condition = _CreateHandAttackCountMinusOneAtLeastTwo(new TriggeredPlayer());
            built.Ally.CardManager.HandCard.AddCards(new[]
            {
                _CreateCard(built, AttackCardId),
                _CreateCard(built, AttackCardId),
                _CreateCard(built, DefenseCardId)
            });

            Assert.That(condition.Eval(context), Is.False);

            built.Ally.CardManager.HandCard.AddCard(_CreateCard(built, AttackCardId));

            Assert.That(condition.Eval(context), Is.True);
        }

        [Test]
        public void AssetRoundTripAndValidator_PreserveCompleteCollectionQueryGraph()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/CardCollectionSliceIntegrationRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();

            try
            {
                asset.Data.ID = "card-collection-slice-integration";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "card-collection-slice-integration",
                    TransformKey = "query",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        _CreateGraveyardAtLeastThreeAttack(_CreateAllyPlayer()),
                        _CreateAllPreservedHandCardsUnsealed(_CreateAllyPlayer()),
                        _CreateHandAttackCountMinusOneAtLeastTwo(_CreateAllyPlayer())
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedConditions = loaded.Data.TransformRules[0].Conditions;
                var graveyardCondition = loadedConditions[0] as IntegerCondition;
                var graveyardCount = graveyardCondition?.Value as CardCollectionCountInteger;
                var graveyardFilter = graveyardCount?.CardCollection as FilteredCardCollection;
                var allCondition = loadedConditions[1] as CardCollectionAllCondition;
                var handFilter = allCondition?.CardCollection as FilteredCardCollection;
                var arithmeticCondition = loadedConditions[2] as IntegerCondition;
                var arithmetic = arithmeticCondition?.Value as ArithmeticInteger;

                Assert.That(graveyardFilter?.CardCollection, Is.TypeOf<CardsOfPlayer>());
                Assert.That(graveyardFilter?.Conditions[0], Is.TypeOf<CardTypesCondition>());
                Assert.That(allCondition?.Conditions[0], Is.TypeOf<CardPropertiesCondition>());
                Assert.That(handFilter?.Conditions[0], Is.TypeOf<CardPropertiesCondition>());
                Assert.That(arithmetic?.Operation, Is.EqualTo(ArithmeticType.Subtract));
                Assert.That(arithmetic?.Left, Is.TypeOf<CardCollectionCountInteger>());

                _SetCatalogCards(catalog, loaded);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static IntegerCondition _CreateGraveyardAtLeastThreeAttack(
            ITargetPlayerValue player)
        {
            return new IntegerCondition
            {
                Value = new CardCollectionCountInteger
                {
                    CardCollection = _CreateFilteredCards(
                        player,
                        CardCollectionType.Graveyard,
                        _CreateCardTypesCondition(CardType.Attack))
                },
                Conditions =
                {
                    _CreateIntegerCompare(
                        ArithmeticConditionType.GreaterThanOrEqual,
                        3)
                }
            };
        }

        private static CardCollectionAllCondition _CreateAllPreservedHandCardsUnsealed(
            ITargetPlayerValue player)
        {
            return new CardCollectionAllCondition
            {
                CardCollection = _CreateFilteredCards(
                    player,
                    CardCollectionType.HandCard,
                    _CreateCardPropertiesCondition(
                        CardProperty.Preserved,
                        SetConditionType.AnyInside)),
                Conditions =
                {
                    _CreateCardPropertiesCondition(
                        CardProperty.Sealed,
                        SetConditionType.AllOutside)
                }
            };
        }

        private static IntegerCondition _CreateHandAttackCountMinusOneAtLeastTwo(
            ITargetPlayerValue player)
        {
            return new IntegerCondition
            {
                Value = new ArithmeticInteger
                {
                    Operation = ArithmeticType.Subtract,
                    Left = new CardCollectionCountInteger
                    {
                        CardCollection = _CreateFilteredCards(
                            player,
                            CardCollectionType.HandCard,
                            _CreateCardTypesCondition(CardType.Attack))
                    },
                    Right = new ConstInteger { Value = 1 }
                },
                Conditions =
                {
                    _CreateIntegerCompare(
                        ArithmeticConditionType.GreaterThanOrEqual,
                        2)
                }
            };
        }

        private static FilteredCardCollection _CreateFilteredCards(
            ITargetPlayerValue player,
            CardCollectionType zone,
            ICardValueCondition condition)
        {
            return new FilteredCardCollection
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = player,
                    Zone = zone
                },
                Conditions = { condition }
            };
        }

        private static CardTypesCondition _CreateCardTypesCondition(CardType type)
        {
            return new CardTypesCondition
            {
                CardTypes = { type },
                Condition = SetConditionType.AnyInside
            };
        }

        private static CardPropertiesCondition _CreateCardPropertiesCondition(
            CardProperty property,
            SetConditionType condition)
        {
            return new CardPropertiesCondition
            {
                CardProperties = { property },
                Condition = condition
            };
        }

        private static IntegerCompare _CreateIntegerCompare(
            ArithmeticConditionType arithmetic,
            int compareValue)
        {
            return new IntegerCompare
            {
                Arithmetic = arithmetic,
                CompareValue = new ConstInteger { Value = compareValue }
            };
        }

        private static ITargetPlayerValue _CreateAllyPlayer()
        {
            return new PlayerByFaction { Faction = Faction.Ally };
        }

        private static BuiltGameplay _BuildGameplay()
        {
            var attack = CardTestBuilder.CreateCardData(AttackCardId);
            attack.Type = CardType.Attack;
            var defense = CardTestBuilder.CreateCardData(DefenseCardId);
            defense.Type = CardType.Defense;
            var preserved = CardTestBuilder.CreateCardData(PreservedCardId);
            preserved.Type = CardType.Defense;
            preserved.PropertyDatas.Add(new PreservedPropertyData());
            var sealedBuff = new CardBuffData
            {
                ID = SealedCardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData(),
                PropertyDatas = { new SealedCardBuffPropertyData() }
            };

            return new GameplayManagerTestBuilder()
                .WithCard(attack)
                .WithCard(defense)
                .WithCard(preserved)
                .WithCardBuff(sealedBuff)
                .Build();
        }

        private static ICardEntity _CreateCard(BuiltGameplay built, string cardId)
        {
            return CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
        }

        private static ICardBuffEntity _CreateSealedBuff(
            BuiltGameplay built,
            TriggerContext context)
        {
            return BuffTestBuilder.CreateCardBuff(
                context,
                built.ContextManager.CardBuffLibrary,
                SealedCardBuffId);
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
