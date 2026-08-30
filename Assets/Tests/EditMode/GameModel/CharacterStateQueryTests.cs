using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class CharacterStateQueryTests
    {
        [TestCase(CharacterIntegerProperty.CharacterIntegerValueType.CurrentHealth, 40)]
        [TestCase(CharacterIntegerProperty.CharacterIntegerValueType.MaxHealth, 100)]
        [TestCase(CharacterIntegerProperty.CharacterIntegerValueType.CurrentShield, 25)]
        public void CharacterIntegerProperty_WhenCharacterExists_ReturnsRequestedState(
            CharacterIntegerProperty.CharacterIntegerValueType property,
            int expected)
        {
            var built = _BuildAlly(40, 100);
            built.Ally.MainCharacter.HealthManager.GetShield(25, built.ContextManager.Context);
            var value = _CreateCharacterIntegerProperty(property);

            var result = value.Eval(_CreateContext(built));

            Assert.That(result.TryGetValue(out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void CharacterIntegerProperty_WhenCharacterIsMissing_ReturnsNone()
        {
            var built = _BuildAlly(40, 100);
            var value = new CharacterIntegerProperty
            {
                Character = new NoneCharacter(),
                Property = CharacterIntegerProperty.CharacterIntegerValueType.CurrentHealth
            };

            var result = value.Eval(_CreateContext(built));

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void CharacterIntegerProperty_WhenPropertyIsInvalid_ReturnsNone()
        {
            var built = _BuildAlly(40, 100);
            var value = _CreateCharacterIntegerProperty(
                (CharacterIntegerProperty.CharacterIntegerValueType)999);

            var result = value.Eval(_CreateContext(built));

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void IntegerCondition_ComposesCharacterStateWithComparison()
        {
            var built = _BuildAlly(40, 100);
            var condition = new IntegerCondition
            {
                Value = _CreateCharacterIntegerProperty(
                    CharacterIntegerProperty.CharacterIntegerValueType.CurrentHealth),
                Conditions =
                {
                    new IntegerCompare
                    {
                        Arithmetic = ArithmeticConditionType.LessThan,
                        CompareValue = new ConstInteger { Value = 50 }
                    }
                }
            };

            Assert.That(condition.Eval(_CreateContext(built)), Is.True);
        }

        [TestCase(0, true)]
        [TestCase(1, false)]
        public void CharacterIsDeadCondition_ReturnsCharacterDeathState(
            int currentHealth,
            bool expected)
        {
            var built = _BuildAlly(currentHealth, 100);
            var condition = new CharacterCondition
            {
                Character = _CreateAllyMainCharacterTarget(),
                Conditions = { new CharacterIsDeadCondition() }
            };

            Assert.That(condition.Eval(_CreateContext(built)), Is.EqualTo(expected));
        }

        [Test]
        public void CharacterIsDeadCondition_WhenCharacterIsMissing_ReturnsFalse()
        {
            var built = _BuildAlly(0, 100);
            var condition = new CharacterCondition
            {
                Character = new NoneCharacter(),
                Conditions = { new CharacterIsDeadCondition() }
            };

            Assert.That(condition.Eval(_CreateContext(built)), Is.False);
        }

        [Test]
        public void CharacterStateComposition_AssetRoundTrip_PreservesPolymorphicTypes()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/CharacterStateCompositionRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "character-state-composition-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = _CreateCharacterIntegerProperty(
                        CharacterIntegerProperty.CharacterIntegerValueType.CurrentShield)
                });
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "dead-character",
                    TransformKey = "state",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
                        new CharacterCondition
                        {
                            Character = _CreateAllyMainCharacterTarget(),
                            Conditions = { new CharacterIsDeadCondition() }
                        }
                    },
                    Operation = new RevertCardTransformOperationData()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedValue = loaded.Data.Effects[0] is DamageEffect damage
                    ? damage.Value as CharacterIntegerProperty
                    : null;
                var loadedCharacterCondition = loaded.Data.TransformRules[0].Conditions[0]
                    as CharacterCondition;
                var loadedMainCharacter = loadedValue?.Character as MainCharacterOfPlayer;

                Assert.That(loadedValue, Is.Not.Null);
                Assert.That(
                    loadedValue.Property,
                    Is.EqualTo(CharacterIntegerProperty.CharacterIntegerValueType.CurrentShield));
                Assert.That(loadedMainCharacter?.Player, Is.TypeOf<PlayerByFaction>());
                Assert.That(loadedCharacterCondition?.Character, Is.TypeOf<MainCharacterOfPlayer>());
                Assert.That(
                    loadedCharacterCondition?.Conditions[0],
                    Is.TypeOf<CharacterIsDeadCondition>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void Validator_WithInvalidCharacterProperty_ReportsError()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                card.Data.ID = "invalid-character-property";
                card.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = _CreateCharacterIntegerProperty(
                        (CharacterIntegerProperty.CharacterIntegerValueType)999)
                });
                _SetCatalogCard(catalog, card);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(
                    errors,
                    Has.Some.Contains("CharacterIntegerProperty.Property 無效：999"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(card);
            }
        }

        [Test]
        public void Validator_WithValidCharacterStateComposition_ReturnsNoErrors()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                card.Data.ID = "valid-character-state-composition";
                card.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = _CreateCharacterIntegerProperty(
                        CharacterIntegerProperty.CharacterIntegerValueType.MaxHealth)
                });
                _SetCatalogCard(catalog, card);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(errors, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(card);
            }
        }

        private static CharacterIntegerProperty _CreateCharacterIntegerProperty(
            CharacterIntegerProperty.CharacterIntegerValueType property)
        {
            return new CharacterIntegerProperty
            {
                Character = _CreateAllyMainCharacterTarget(),
                Property = property
            };
        }

        private static MainCharacterOfPlayer _CreateAllyMainCharacterTarget()
        {
            return new MainCharacterOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally }
            };
        }

        private static BuiltGameplay _BuildAlly(int currentHealth, int maxHealth)
        {
            return new GameplayManagerTestBuilder()
                .WithAllyCharacters(new CharacterParameter
                {
                    NameKey = "ally-main",
                    CurrentHealth = currentHealth,
                    MaxHealth = maxHealth
                })
                .Build();
        }

        private static TriggerContext _CreateContext(BuiltGameplay built)
        {
            return new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterTurnStart, SystemSource.Instance));
        }

        private static void _SetCatalogCard(
            GameContentCatalog catalog,
            CardDataScriptableBase card)
        {
            var serializedCatalog = new SerializedObject(catalog);
            var cardAssets = serializedCatalog.FindProperty("_cardAssets");
            cardAssets.arraySize = 1;
            cardAssets.GetArrayElementAtIndex(0).objectReferenceValue = card;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
