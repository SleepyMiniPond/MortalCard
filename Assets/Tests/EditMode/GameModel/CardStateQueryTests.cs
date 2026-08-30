using System.Collections.Generic;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.Tests.T010;
using NUnit.Framework;
using Optional;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class CardStateQueryTests
    {
        [Test]
        public void CardIntegerProperty_BeforeAndAfterTransform_ReturnsBaseAndEffectiveValues()
        {
            var built = _BuildCardStateTest();

            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardBasePower, 3);
            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardBaseCost, 2);
            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardPower, 3);
            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardCost, 2);

            Assert.That(_Transform(built).IsSuccess, Is.True);
            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardBasePower, 8);
            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardBaseCost, 5);
            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardPower, 8);
            _AssertInteger(built, CardIntegerProperty.CardIntegerValueType.CardCost, 5);
        }

        [Test]
        public void CardIntegerProperty_WhenCardOrPropertyIsInvalid_ReturnsNone()
        {
            var built = _BuildCardStateTest();
            var missingCard = new CardIntegerProperty
            {
                Card = new NoneCard(),
                Property = CardIntegerProperty.CardIntegerValueType.CardPower
            };
            var invalidProperty = new CardIntegerProperty
            {
                Card = new TriggeredCard(),
                Property = (CardIntegerProperty.CardIntegerValueType)999
            };

            Assert.That(missingCard.Eval(built.Context).HasValue, Is.False);
            Assert.That(invalidProperty.Eval(built.Context).HasValue, Is.False);
        }

        [Test]
        public void BaseCardDataAndCardForm_AfterTransform_KeepDistinctComparisonSemantics()
        {
            var built = _BuildCardStateTest();
            var baseCard = CardEntity.RuntimeCreateFromId(
                CardTransformationTestBuilder.BaseCardId,
                built.Gameplay.ContextManager.CardLibrary,
                built.Gameplay.ContextManager.CardPropertyEntityFactory);
            var alternateCard = CardEntity.RuntimeCreateFromId(
                CardTransformationTestBuilder.AlternateCardId,
                built.Gameplay.ContextManager.CardLibrary,
                built.Gameplay.ContextManager.CardPropertyEntityFactory);
            Assert.That(_Transform(built).IsSuccess, Is.True);

            Assert.That(new BaseCardDataIdCondition
            {
                CompareCard = new FixedCard(baseCard)
            }.Eval(built.Context, built.Card), Is.True);
            Assert.That(new CardFormCondition
            {
                CompareCard = new FixedCard(baseCard)
            }.Eval(built.Context, built.Card), Is.False);
            Assert.That(new CardFormCondition
            {
                CompareCard = new FixedCard(alternateCard)
            }.Eval(built.Context, built.Card), Is.True);
        }

        [Test]
        public void CardStateConditions_AfterTransform_QueryEffectiveForm()
        {
            var built = _BuildCardStateTest();
            Assert.That(_Transform(built).IsSuccess, Is.True);

            Assert.That(new CardTypesCondition
            {
                CardTypes = { CardType.Defense },
                Condition = SetConditionType.AnyInside
            }.Eval(built.Context, built.Card), Is.True);
            Assert.That(new CardThemesCondition
            {
                CardThemes = { CardTheme.Emei },
                Condition = SetConditionType.AllInside
            }.Eval(built.Context, built.Card), Is.True);
            Assert.That(new CardRaritiesCondition
            {
                CardRarities = { CardRarity.Rare },
                Condition = SetConditionType.AnyInside
            }.Eval(built.Context, built.Card), Is.True);
            Assert.That(new CardPropertiesCondition
            {
                CardProperties = { CardProperty.Initialize },
                Condition = SetConditionType.AnyInside
            }.Eval(built.Context, built.Card), Is.True);
            Assert.That(new CardPropertiesCondition
            {
                CardProperties = { CardProperty.Preserved },
                Condition = SetConditionType.AnyInside
            }.Eval(built.Context, built.Card), Is.False);
        }

        [Test]
        public void CardComparisonConditions_WhenCompareCardIsMissing_ReturnFalse()
        {
            var built = _BuildCardStateTest();

            Assert.That(new BaseCardDataIdCondition { CompareCard = new NoneCard() }
                .Eval(built.Context, built.Card), Is.False);
            Assert.That(new CardFormCondition { CompareCard = new NoneCard() }
                .Eval(built.Context, built.Card), Is.False);
        }

        [Test]
        public void CardStateComposition_AssetRoundTrip_PreservesPolymorphicTypes()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/CardStateCompositionRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "card-state-composition-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new CardIntegerProperty
                    {
                        Card = new TriggeredCard(),
                        Property = CardIntegerProperty.CardIntegerValueType.CardBasePower
                    }
                });
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "card-state",
                    TransformKey = "state",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new CardCondition
                        {
                            Card = new TriggeredCard(),
                            Conditions =
                            {
                                new BaseCardDataIdCondition { CompareCard = new TriggeredCard() },
                                new CardFormCondition { CompareCard = new TriggeredCard() },
                                new CardTypesCondition
                                {
                                    CardTypes = { CardType.Attack },
                                    Condition = SetConditionType.AnyInside
                                },
                                new CardThemesCondition
                                {
                                    CardThemes = { CardTheme.TangSect },
                                    Condition = SetConditionType.AnyInside
                                },
                                new CardRaritiesCondition
                                {
                                    CardRarities = { CardRarity.Common },
                                    Condition = SetConditionType.AnyInside
                                },
                                new CardPropertiesCondition
                                {
                                    CardProperties = { CardProperty.Preserved },
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
                var loadedCardCondition = loaded.Data.TransformRules[0].Conditions[0] as CardCondition;
                Assert.That(loadedCardCondition?.Conditions[0], Is.TypeOf<BaseCardDataIdCondition>());
                Assert.That(loadedCardCondition?.Conditions[1], Is.TypeOf<CardFormCondition>());
                Assert.That(loadedCardCondition?.Conditions[2], Is.TypeOf<CardTypesCondition>());
                Assert.That(loadedCardCondition?.Conditions[3], Is.TypeOf<CardThemesCondition>());
                Assert.That(loadedCardCondition?.Conditions[4], Is.TypeOf<CardRaritiesCondition>());
                Assert.That(loadedCardCondition?.Conditions[5], Is.TypeOf<CardPropertiesCondition>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void Validator_WithInvalidCardProperty_ReportsError()
        {
            var errors = _Validate(new CardIntegerProperty
            {
                Card = new TriggeredCard(),
                Property = (CardIntegerProperty.CardIntegerValueType)999
            });

            Assert.That(errors, Has.Some.Contains("CardIntegerProperty.Property 無效：999"));
        }

        [Test]
        public void Validator_WithEmptyOrNoneCardStateSet_ReportsErrors()
        {
            var errors = _Validate(
                new ConstInteger { Value = 0 },
                new CardTypesCondition { Condition = SetConditionType.AnyInside },
                new CardThemesCondition
                {
                    CardThemes = { CardTheme.None },
                    Condition = SetConditionType.AnyInside
                },
                new CardRaritiesCondition { Condition = SetConditionType.AnyInside },
                new CardPropertiesCondition { Condition = SetConditionType.AnyInside });

            Assert.That(errors, Has.Some.Contains("CardTypesCondition 比較值至少需要一項"));
            Assert.That(errors, Has.Some.Contains("CardThemesCondition 比較值無效：None"));
            Assert.That(errors, Has.Some.Contains("CardRaritiesCondition 比較值至少需要一項"));
            Assert.That(errors, Has.Some.Contains("CardPropertiesCondition 比較值至少需要一項"));
        }

        private static BuiltCardTransformationTest _BuildCardStateTest()
        {
            var baseCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId, 2, 3);
            baseCard.Type = CardType.Attack;
            baseCard.Rarity = CardRarity.Common;
            baseCard.Themes = new[] { CardTheme.TangSect };
            baseCard.PropertyDatas.Add(new PreservedPropertyData());
            var alternateCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.AlternateCardId, 5, 8);
            alternateCard.Type = CardType.Defense;
            alternateCard.Rarity = CardRarity.Rare;
            alternateCard.Themes = new[] { CardTheme.Emei };
            alternateCard.PropertyDatas.Add(new InitialPriorityPropertyData());

            return new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .WithCard(alternateCard)
                .Build();
        }

        private static CardFormOperationResult _Transform(BuiltCardTransformationTest built)
        {
            return built.Card.TryApplySelfForm(
                "card-state",
                CardTransformationTestBuilder.AlternateCardId,
                CardFormPersistence.BattleOnly);
        }

        private static void _AssertInteger(
            BuiltCardTransformationTest built,
            CardIntegerProperty.CardIntegerValueType property,
            int expected)
        {
            var value = new CardIntegerProperty
            {
                Card = new TriggeredCard(),
                Property = property
            }.Eval(built.Context);
            Assert.That(value.TryGetValue(out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static IReadOnlyList<string> _Validate(
            IIntegerValue value,
            params ICardValueCondition[] conditions)
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                card.Data.ID = "card-state-validator";
                card.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = value
                });
                if (conditions.Length > 0)
                {
                    card.Data.TransformRules.Add(new CardTransformRule
                    {
                        RuleId = "validate-card-state",
                        TransformKey = "state",
                        Timing = GameTiming.AfterTurnStart,
                        Conditions =
                        {
                            new CardCondition
                            {
                                Card = new TriggeredCard(),
                                Conditions = new List<ICardValueCondition>(conditions)
                            }
                        },
                        Operation = new RevertCardTransformOperationData()
                    });
                }

                var serializedCatalog = new SerializedObject(catalog);
                var cardAssets = serializedCatalog.FindProperty("_cardAssets");
                cardAssets.arraySize = 1;
                cardAssets.GetArrayElementAtIndex(0).objectReferenceValue = card;
                serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
                return GameDataValidator.ValidateNestedContent(catalog);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(card);
            }
        }

        private sealed class FixedCard : ITargetCardValue
        {
            private readonly ICardEntity _card;

            public FixedCard(ICardEntity card)
            {
                _card = card;
            }

            public Option<ICardEntity> Eval(TriggerContext triggerContext)
            {
                return _card.Some();
            }
        }
    }
}
