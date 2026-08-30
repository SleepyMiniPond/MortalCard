using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class PlayerStateQueryTests
    {
        [TestCase(PlayerIntegerProperty.PlayerIntegerValueType.CurrentEnergy, 2)]
        [TestCase(PlayerIntegerProperty.PlayerIntegerValueType.MaxEnergy, 3)]
        [TestCase(PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition, 4)]
        public void PlayerIntegerProperty_WhenAllyExists_ReturnsRequestedState(
            PlayerIntegerProperty.PlayerIntegerValueType property,
            int expected)
        {
            var built = new GameplayManagerTestBuilder().Build();
            built.Ally.EnergyManager.GainEnergy(2);
            built.Ally.DispositionManager.IncreaseDisposition(4);
            var value = _CreatePlayerIntegerProperty(Faction.Ally, property);

            var result = value.Eval(_CreateContext(built));

            Assert.That(result.TryGetValue(out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void PlayerIntegerProperty_WhenEnemyDispositionIsRequested_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var value = _CreatePlayerIntegerProperty(
                Faction.Enemy,
                PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition);

            var result = value.Eval(_CreateContext(built));

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void PlayerIntegerProperty_WhenPlayerIsMissing_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var value = new PlayerIntegerProperty
            {
                Player = new NonePlayer(),
                Property = PlayerIntegerProperty.PlayerIntegerValueType.CurrentEnergy
            };

            var result = value.Eval(_CreateContext(built));

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void PlayerIntegerProperty_WhenPropertyIsInvalid_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var value = _CreatePlayerIntegerProperty(
                Faction.Ally,
                (PlayerIntegerProperty.PlayerIntegerValueType)999);

            var result = value.Eval(_CreateContext(built));

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void IntegerCondition_WhenDispositionIsUnavailable_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var condition = new IntegerCondition
            {
                Value = _CreatePlayerIntegerProperty(
                    Faction.Enemy,
                    PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition),
                Conditions =
                {
                    new IntegerCompare
                    {
                        Arithmetic = ArithmeticConditionType.Equal,
                        CompareValue = new ConstInteger { Value = 0 }
                    }
                }
            };

            Assert.That(condition.Eval(_CreateContext(built)), Is.False);
        }

        [Test]
        public void PlayerIsDead_WhenMainCharacterIsDeadAndAssistIsAlive_ReturnsTrue()
        {
            var built = _BuildAllyWithCharacters(mainHealth: 0, assistHealth: 100);
            var condition = _CreateAllyDeathCondition();

            Assert.That(built.Ally.IsDead, Is.True);
            Assert.That(condition.Eval(_CreateContext(built)), Is.True);
        }

        [Test]
        public void PlayerIsDead_WhenMainCharacterIsAliveAndAssistIsDead_ReturnsFalse()
        {
            var built = _BuildAllyWithCharacters(mainHealth: 100, assistHealth: 0);
            var condition = _CreateAllyDeathCondition();

            Assert.That(built.Ally.IsDead, Is.False);
            Assert.That(condition.Eval(_CreateContext(built)), Is.False);
        }

        [Test]
        public void PlayerIsDeadCondition_WhenPlayerIsMissing_ReturnsFalse()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var condition = new PlayerCondition
            {
                Player = new NonePlayer(),
                Conditions = { new PlayerIsDeadCondition() }
            };

            Assert.That(condition.Eval(_CreateContext(built)), Is.False);
        }

        [Test]
        public void PlayerStateComposition_AssetRoundTrip_PreservesPolymorphicTypes()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/PlayerStateCompositionRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "player-state-composition-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = _CreatePlayerIntegerProperty(
                        Faction.Ally,
                        PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition)
                });
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "dead-player",
                    TransformKey = "state",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions = { _CreateAllyDeathCondition() },
                    Operation = new RevertCardTransformOperationData()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedValue = loaded.Data.Effects[0] is DamageEffect damage
                    ? damage.Value as PlayerIntegerProperty
                    : null;
                var loadedPlayerCondition = loaded.Data.TransformRules[0].Conditions[0]
                    as PlayerCondition;

                Assert.That(loadedValue, Is.Not.Null);
                Assert.That(
                    loadedValue.Property,
                    Is.EqualTo(PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition));
                Assert.That(loadedValue.Player, Is.TypeOf<PlayerByFaction>());
                Assert.That(loadedPlayerCondition?.Player, Is.TypeOf<PlayerByFaction>());
                Assert.That(
                    loadedPlayerCondition?.Conditions[0],
                    Is.TypeOf<PlayerIsDeadCondition>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void Validator_WithInvalidPlayerProperty_ReportsError()
        {
            var errors = _ValidateValue(_CreatePlayerIntegerProperty(
                Faction.Ally,
                (PlayerIntegerProperty.PlayerIntegerValueType)999));

            Assert.That(
                errors,
                Has.Some.Contains("PlayerIntegerProperty.Property 無效：999"));
        }

        [Test]
        public void Validator_WithEnemyDisposition_ReportsError()
        {
            var errors = _ValidateValue(_CreatePlayerIntegerProperty(
                Faction.Enemy,
                PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition));

            Assert.That(
                errors,
                Has.Some.Contains("PlayerIntegerProperty.CurrentDisposition 不可指定 Enemy"));
        }

        [Test]
        public void Validator_WithDynamicDispositionTarget_ReturnsNoErrors()
        {
            var errors = _ValidateValue(new PlayerIntegerProperty
            {
                Player = new CurrentPlayer(),
                Property = PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition
            });

            Assert.That(errors, Is.Empty);
        }

        private static PlayerIntegerProperty _CreatePlayerIntegerProperty(
            Faction faction,
            PlayerIntegerProperty.PlayerIntegerValueType property)
        {
            return new PlayerIntegerProperty
            {
                Player = new PlayerByFaction { Faction = faction },
                Property = property
            };
        }

        private static PlayerCondition _CreateAllyDeathCondition()
        {
            return new PlayerCondition
            {
                Player = new PlayerByFaction { Faction = Faction.Ally },
                Conditions = { new PlayerIsDeadCondition() }
            };
        }

        private static BuiltGameplay _BuildAllyWithCharacters(
            int mainHealth,
            int assistHealth)
        {
            return new GameplayManagerTestBuilder()
                .WithAllyCharacters(
                    new CharacterParameter
                    {
                        NameKey = "ally-main",
                        CurrentHealth = mainHealth,
                        MaxHealth = 100
                    },
                    new CharacterParameter
                    {
                        NameKey = "ally-assist",
                        CurrentHealth = assistHealth,
                        MaxHealth = 100
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

        private static System.Collections.Generic.IReadOnlyList<string> _ValidateValue(
            IIntegerValue value)
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                card.Data.ID = "player-state-validator";
                card.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = value
                });
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
    }
}
