using System;
using System.Collections.Generic;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public class ScriptableObjectDataValidationTests
    {
        [Test]
        public void AllEffectCommandTypes_HaveHandler()
        {
            AssertNoErrors(GameDataValidator.ValidateEffectCommandHandlers());
        }

        [Test]
        public void ScriptableObjectEffects_HaveResolvers()
        {
            AssertNoErrors(GameDataValidator.ValidateEffectResolvers());
        }

        [Test]
        public void ScriptableObjectReferenceIds_ExistInLibraries()
        {
            AssertNoErrors(GameDataValidator.ValidateReferenceIds());
        }

        [Test]
        public void ScriptableObjectReactionSessions_HaveUniqueTimingRules()
        {
            AssertNoErrors(GameDataValidator.ValidateReactionSessionRules());
        }

        [Test]
        public void CardScriptableTypes_AreValid()
        {
            AssertNoErrors(GameDataValidator.ValidateCardScriptableTypes());
        }

        [Test]
        public void ValidateCardScriptableTypes_WithDuplicateIdAndInvalidSelfTransformTarget_ReturnsErrors()
        {
            var standard = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var overrideCard = ScriptableObject.CreateInstance<OverrideCardDataScriptable>();
            try
            {
                standard.name = "Standard Test";
                standard.Data.ID = "duplicate";
                standard.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "apply-override",
                    TransformKey = "form",
                    Timing = GameTiming.BeforeTurnStart,
                    Conditions = new List<ICondition>(),
                    Operation = new ApplyCardTransformOperationData
                    {
                        TargetCardDataId = "duplicate"
                    }
                });
                standard.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "apply-missing",
                    TransformKey = "form",
                    Timing = GameTiming.BeforeTurnStart,
                    Conditions = new List<ICondition>(),
                    Operation = new ApplyCardTransformOperationData
                    {
                        TargetCardDataId = "missing-standard"
                    }
                });
                overrideCard.name = "Override Test";
                overrideCard.Data.ID = "duplicate";

                var errors = GameDataValidator.ValidateCardScriptableTypes(
                    new CardDataScriptableBase[] { overrideCard, standard },
                    Array.Empty<DeckScriptable>());

                Assert.That(errors, Has.Some.Contains("Standard／Override 資產間重複"));
                Assert.That(errors, Has.Some.Contains("Self Transform Target 必須是 Standard CardData"));
                Assert.That(errors, Has.Some.Contains("Self Transform Target 指向不存在的 CardData：missing-standard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(standard);
                UnityEngine.Object.DestroyImmediate(overrideCard);
            }
        }

        [Test]
        public void ValidateCardScriptableTypes_WithValidOverrideEffect_ReturnsNoErrors()
        {
            var standard = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var overrideCard = ScriptableObject.CreateInstance<OverrideCardDataScriptable>();
            try
            {
                standard.name = "Override Source";
                standard.Data.ID = "override-source";
                standard.Data.Effects.Add(new ApplyCardFormOverrideEffect
                {
                    TargetCards = new SingleCardCollection { TargetCard = new TriggeredCard() },
                    OverrideKey = "negative-form",
                    TargetCardDataId = "override-target",
                    ReleaseRules = new List<CardFormOverrideReleaseRule>
                    {
                        new()
                        {
                            Timing = GameTiming.AfterPlayCardEnd,
                            Conditions = new List<ICondition>()
                        }
                    },
                    ReactionSessions = new Dictionary<string, IReactionSessionData>()
                });
                overrideCard.name = "Override Target";
                overrideCard.Data.ID = "override-target";

                var errors = GameDataValidator.ValidateCardScriptableTypes(
                    new CardDataScriptableBase[] { standard, overrideCard },
                    Array.Empty<DeckScriptable>());

                AssertNoErrors(errors);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(standard);
                UnityEngine.Object.DestroyImmediate(overrideCard);
            }
        }

        [Test]
        public void ValidateCardScriptableTypes_WithInvalidOverrideEffect_ReturnsDetailedErrors()
        {
            var source = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var standardTarget = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                source.name = "Invalid Override Source";
                source.Data.ID = "invalid-override-source";
                source.Data.Effects.Add(new ApplyCardFormOverrideEffect
                {
                    OverrideKey = "override",
                    TargetCardDataId = "standard-target",
                    ReleaseRules = new List<CardFormOverrideReleaseRule>
                    {
                        new()
                        {
                            Timing = GameTiming.None,
                            Conditions = new List<ICondition>
                            {
                                new AllCondition
                                {
                                    Conditions = new List<ICondition>
                                    {
                                        null,
                                        new CardFormOverrideSessionCondition
                                        {
                                            SessionKey = "missing-session"
                                        }
                                    }
                                }
                            }
                        }
                    },
                    ReactionSessions = new Dictionary<string, IReactionSessionData>
                    {
                        {
                            "counter",
                            new SessionInteger
                            {
                                UpdateRules = new List<SessionInteger.TimingRule>
                                {
                                    new() { Timing = GameTiming.BeforeTurnEnd },
                                    new() { Timing = GameTiming.BeforeTurnEnd }
                                }
                            }
                        }
                    }
                });
                standardTarget.name = "Standard Target";
                standardTarget.Data.ID = "standard-target";

                var errors = GameDataValidator.ValidateCardScriptableTypes(
                    new CardDataScriptableBase[] { source, standardTarget },
                    Array.Empty<DeckScriptable>());

                Assert.That(errors, Has.Some.Contains("TargetCards 為空"));
                Assert.That(errors, Has.Some.Contains("Target 必須是 Override CardData"));
                Assert.That(errors, Has.Some.Contains("Timing 不可為 None"));
                Assert.That(errors, Has.Some.Contains("Conditions 含有空值"));
                Assert.That(errors, Has.Some.Contains("引用不存在的 Override SessionKey：missing-session"));
                Assert.That(errors, Has.Some.Contains("TimingRule 重複：BeforeTurnEnd"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(standardTarget);
            }
        }

        [Test]
        public void OverrideCardData_DoesNotExposeTransformRules()
        {
            Assert.That(typeof(StandardCardData).GetField(nameof(StandardCardData.TransformRules)), Is.Not.Null);
            Assert.That(typeof(CardData).GetField(nameof(StandardCardData.TransformRules)), Is.Null);
            Assert.That(typeof(OverrideCardData).GetField(nameof(StandardCardData.TransformRules)), Is.Null);
        }

        [Test]
        public void TriggeredCardEffect_AssetRoundTrip_PreservesTimingAndPolymorphicEffect()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/TriggeredCardEffectRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "triggered-effect-round-trip";
                asset.Data.TriggeredEffects.Add(new TriggeredCardEffect
                {
                    Timing = CardTriggeredTiming.FormChanged,
                    Effects = new ICardEffect[] { new GainEnergyEffect() }
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);

                Assert.That(loaded.Data.TriggeredEffects, Has.Count.EqualTo(1));
                Assert.That(
                    loaded.Data.TriggeredEffects[0].Timing,
                    Is.EqualTo(CardTriggeredTiming.FormChanged));
                Assert.That(loaded.Data.TriggeredEffects[0].Effects, Has.Length.EqualTo(1));
                Assert.That(loaded.Data.TriggeredEffects[0].Effects[0], Is.TypeOf<GainEnergyEffect>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void TurnCountInteger_AssetRoundTrip_PreservesPolymorphicType()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/TurnCountIntegerRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "turn-count-integer-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new TurnCountInteger()
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DamageEffect;

                Assert.That(loadedEffect, Is.Not.Null);
                Assert.That(loadedEffect.Value, Is.TypeOf<TurnCountInteger>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void TurnParityCondition_AssetRoundTrip_PreservesPolymorphicComposition()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/TurnParityConditionRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "turn-parity-condition-round-trip";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "even-turn",
                    TransformKey = "form",
                    Timing = GameTiming.AfterTurnStart,
                    Conditions =
                    {
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
                                    Arithmetic = ArithmeticConditionType.Equal,
                                    CompareValue = new ConstInteger { Value = 0 }
                                }
                            }
                        }
                    },
                    Operation = new ApplyCardTransformOperationData
                    {
                        TargetCardDataId = "shield-form"
                    }
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedCondition = loaded.Data.TransformRules[0].Conditions[0]
                    as IntegerCondition;
                var loadedRemainder = loadedCondition?.Value as ArithmeticInteger;
                var loadedCompare = loadedCondition?.Conditions[0] as IntegerCompare;

                Assert.That(loadedCondition, Is.Not.Null);
                Assert.That(loadedRemainder, Is.Not.Null);
                Assert.That(loadedRemainder.Operation, Is.EqualTo(ArithmeticType.Remainder));
                Assert.That(loadedRemainder.Left, Is.TypeOf<TurnCountInteger>());
                Assert.That(loadedRemainder.Right, Is.TypeOf<ConstInteger>());
                Assert.That(((ConstInteger)loadedRemainder.Right).Value, Is.EqualTo(2));
                Assert.That(loadedCompare, Is.Not.Null);
                Assert.That(
                    loadedCompare.Arithmetic,
                    Is.EqualTo(ArithmeticConditionType.Equal));
                Assert.That(loadedCompare.CompareValue, Is.TypeOf<ConstInteger>());
                Assert.That(((ConstInteger)loadedCompare.CompareValue).Value, Is.EqualTo(0));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void CardTargetComposition_AssetRoundTrip_PreservesExplicitTargetTypes()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/CardTargetCompositionRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "card-target-composition-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new CardIntegerProperty
                    {
                        Card = new PlayingCardOfPlayer
                        {
                            Player = new CardOwner
                            {
                                Card = new ActionCard()
                            }
                        },
                        Property = CardIntegerProperty.CardIntegerValueType.CardPower
                    }
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DamageEffect;
                var loadedProperty = loadedEffect?.Value as CardIntegerProperty;
                var loadedPlayingCard = loadedProperty?.Card as PlayingCardOfPlayer;
                var loadedOwner = loadedPlayingCard?.Player as CardOwner;

                Assert.That(loadedPlayingCard, Is.Not.Null);
                Assert.That(loadedOwner, Is.Not.Null);
                Assert.That(loadedOwner.Card, Is.TypeOf<ActionCard>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void EntityRelationshipTargets_AssetRoundTrip_PreserveCompleteChain()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/EntityRelationshipTargetsRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "entity-relationship-targets-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new CharactersOfPlayer
                    {
                        Player = new CharacterOwner
                        {
                            Character = new MainCharacterOfPlayer
                            {
                                Player = new PlayerByFaction
                                {
                                    Faction = Faction.Enemy
                                }
                            }
                        }
                    },
                    Value = new ConstInteger { Value = 1 }
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DamageEffect;
                var loadedCharacters = loadedEffect?.Targets as CharactersOfPlayer;
                var loadedOwner = loadedCharacters?.Player as CharacterOwner;
                var loadedMainCharacter = loadedOwner?.Character as MainCharacterOfPlayer;
                var loadedPlayer = loadedMainCharacter?.Player as PlayerByFaction;

                Assert.That(loadedCharacters, Is.Not.Null);
                Assert.That(loadedOwner, Is.Not.Null);
                Assert.That(loadedMainCharacter, Is.Not.Null);
                Assert.That(loadedPlayer, Is.Not.Null);
                Assert.That(loadedPlayer.Faction, Is.EqualTo(Faction.Enemy));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void CardsOfPlayer_AssetRoundTrip_PreservesPlayerAndZone()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/CardsOfPlayerRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "cards-of-player-round-trip";
                asset.Data.Effects.Add(new DiscardCardEffect
                {
                    TargetCards = new CardsOfPlayer
                    {
                        Player = new CurrentPlayer(),
                        Zone = CardCollectionType.DisposeZone
                    }
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DiscardCardEffect;
                var loadedCards = loadedEffect?.TargetCards as CardsOfPlayer;

                Assert.That(loadedCards, Is.Not.Null);
                Assert.That(loadedCards.Player, Is.TypeOf<CurrentPlayer>());
                Assert.That(loadedCards.Zone, Is.EqualTo(CardCollectionType.DisposeZone));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void CardIdentityConditions_AssetRoundTrip_PreserveZoneAndPlayingCompositions()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/CardIdentityConditionsRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "card-identity-conditions-round-trip";
                asset.Data.TransformRules.Add(new CardTransformRule
                {
                    RuleId = "identity-condition-round-trip",
                    TransformKey = "identity-condition-round-trip",
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
                        new CardCondition
                        {
                            Card = new PlayingCardOfPlayer
                            {
                                Player = new CardOwner { Card = new TriggeredCard() }
                            },
                            Conditions =
                            {
                                new CardIdentityCondition
                                {
                                    CompareCard = new TriggeredCard()
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
                var loadedContains = loaded.Data.TransformRules[0].Conditions[0]
                    as CardCollectionContainsCondition;
                var loadedCards = loadedContains?.CardCollection as CardsOfPlayer;
                var loadedOwner = loadedCards?.Player as CardOwner;
                var loadedPlayingCondition = loaded.Data.TransformRules[0].Conditions[1]
                    as CardCondition;
                var loadedPlayingCard = loadedPlayingCondition?.Card
                    as PlayingCardOfPlayer;
                var loadedPlayingOwner = loadedPlayingCard?.Player as CardOwner;
                var loadedIdentityCompare = loadedPlayingCondition?.Conditions[0]
                    as CardIdentityCondition;

                Assert.That(loadedContains, Is.Not.Null);
                Assert.That(loadedCards, Is.Not.Null);
                Assert.That(loadedCards.Zone, Is.EqualTo(CardCollectionType.HandCard));
                Assert.That(loadedOwner?.Card, Is.TypeOf<TriggeredCard>());
                Assert.That(loadedContains.Card, Is.TypeOf<TriggeredCard>());
                Assert.That(loadedPlayingCondition, Is.Not.Null);
                Assert.That(loadedPlayingCard, Is.Not.Null);
                Assert.That(loadedPlayingOwner?.Card, Is.TypeOf<TriggeredCard>());
                Assert.That(loadedIdentityCompare?.CompareCard, Is.TypeOf<TriggeredCard>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void SwordShieldRules_AssetRoundTrip_PreserveCompleteSliceOneComposition()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/SwordShieldRulesRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data = CardTransformation.SwordShieldTransformationIntegrationTests.CreateSwordWithTransformRules();
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var applyRule = loaded.Data.TransformRules[0];
                var revertRule = loaded.Data.TransformRules[1];
                var handCondition = applyRule.Conditions[0]
                    as CardCollectionContainsCondition;
                var handCards = handCondition?.CardCollection as CardsOfPlayer;
                var parityCondition = applyRule.Conditions[1] as IntegerCondition;
                var remainder = parityCondition?.Value as ArithmeticInteger;
                var evenCompare = parityCondition?.Conditions[0] as IntegerCompare;
                var oddCondition = revertRule.Conditions[1] as IntegerCondition;
                var oddCompare = oddCondition?.Conditions[0] as IntegerCompare;

                Assert.That(loaded.Data.TransformRules, Has.Count.EqualTo(2));
                Assert.That(applyRule.Timing, Is.EqualTo(GameTiming.AfterTurnStart));
                Assert.That(revertRule.Timing, Is.EqualTo(GameTiming.AfterTurnStart));
                Assert.That(handCondition?.Card, Is.TypeOf<TriggeredCard>());
                Assert.That(handCards?.Zone, Is.EqualTo(CardCollectionType.HandCard));
                Assert.That(handCards?.Player, Is.TypeOf<CardOwner>());
                Assert.That(remainder?.Operation, Is.EqualTo(ArithmeticType.Remainder));
                Assert.That(remainder?.Left, Is.TypeOf<TurnCountInteger>());
                Assert.That((remainder?.Right as ConstInteger)?.Value, Is.EqualTo(2));
                Assert.That(
                    evenCompare?.Arithmetic,
                    Is.EqualTo(ArithmeticConditionType.Equal));
                Assert.That(
                    oddCompare?.Arithmetic,
                    Is.EqualTo(ArithmeticConditionType.NotEqual));
                Assert.That(applyRule.Operation, Is.TypeOf<ApplyCardTransformOperationData>());
                Assert.That(revertRule.Operation, Is.TypeOf<RevertCardTransformOperationData>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void MinimumInteger_AssetRoundTrip_PreservesPolymorphicValues()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/MinimumIntegerRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "minimum-integer-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new MinimumInteger
                    {
                        Values =
                        {
                            new ConstInteger { Value = 8 },
                            new MinimumInteger
                            {
                                Values =
                                {
                                    new ConstInteger { Value = -3 },
                                    new ConstInteger { Value = 5 }
                                }
                            }
                        }
                    }
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DamageEffect;
                var loadedMinimum = loadedEffect?.Value as MinimumInteger;

                Assert.That(loadedMinimum, Is.Not.Null);
                Assert.That(loadedMinimum.Values, Has.Count.EqualTo(2));
                Assert.That(loadedMinimum.Values[0], Is.TypeOf<ConstInteger>());
                Assert.That(
                    ((ConstInteger)loadedMinimum.Values[0]).Value,
                    Is.EqualTo(8));

                var loadedNestedMinimum = loadedMinimum.Values[1] as MinimumInteger;
                Assert.That(loadedNestedMinimum, Is.Not.Null);
                Assert.That(loadedNestedMinimum.Values, Has.Count.EqualTo(2));
                Assert.That(loadedNestedMinimum.Values[0], Is.TypeOf<ConstInteger>());
                Assert.That(loadedNestedMinimum.Values[1], Is.TypeOf<ConstInteger>());
                Assert.That(
                    ((ConstInteger)loadedNestedMinimum.Values[0]).Value,
                    Is.EqualTo(-3));
                Assert.That(
                    ((ConstInteger)loadedNestedMinimum.Values[1]).Value,
                    Is.EqualTo(5));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void MaximumInteger_AssetRoundTrip_PreservesPolymorphicValues()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/MaximumIntegerRoundTrip.asset");
            var asset = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                asset.Data.ID = "maximum-integer-round-trip";
                asset.Data.Effects.Add(new DamageEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new MaximumInteger
                    {
                        Values =
                        {
                            new ConstInteger { Value = -8 },
                            new MaximumInteger
                            {
                                Values =
                                {
                                    new ConstInteger { Value = 3 },
                                    new ConstInteger { Value = -5 }
                                }
                            }
                        }
                    }
                });
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(asset);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(assetPath);
                var loadedEffect = loaded.Data.Effects[0] as DamageEffect;
                var loadedMaximum = loadedEffect?.Value as MaximumInteger;

                Assert.That(loadedMaximum, Is.Not.Null);
                Assert.That(loadedMaximum.Values, Has.Count.EqualTo(2));
                Assert.That(loadedMaximum.Values[0], Is.TypeOf<ConstInteger>());
                Assert.That(
                    ((ConstInteger)loadedMaximum.Values[0]).Value,
                    Is.EqualTo(-8));

                var loadedNestedMaximum = loadedMaximum.Values[1] as MaximumInteger;
                Assert.That(loadedNestedMaximum, Is.Not.Null);
                Assert.That(loadedNestedMaximum.Values, Has.Count.EqualTo(2));
                Assert.That(loadedNestedMaximum.Values[0], Is.TypeOf<ConstInteger>());
                Assert.That(loadedNestedMaximum.Values[1], Is.TypeOf<ConstInteger>());
                Assert.That(
                    ((ConstInteger)loadedNestedMaximum.Values[0]).Value,
                    Is.EqualTo(3));
                Assert.That(
                    ((ConstInteger)loadedNestedMaximum.Values[1]).Value,
                    Is.EqualTo(-5));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void ValidateReactionSessionRules_WithDuplicateBooleanAndIntegerTimings_ReturnsErrors()
        {
            var sessions = new Dictionary<string, IReactionSessionData>
            {
                {
                    "boolean-session",
                    new SessionBoolean
                    {
                        UpdateRules = new List<SessionBoolean.TimingRule>
                        {
                            new() { Timing = GameTiming.BeforeTurnEnd },
                            new() { Timing = GameTiming.BeforeTurnEnd }
                        }
                    }
                },
                {
                    "integer-session",
                    new SessionInteger
                    {
                        UpdateRules = new List<SessionInteger.TimingRule>
                        {
                            new() { Timing = GameTiming.CardPlayResult },
                            new() { Timing = GameTiming.CardPlayResult }
                        }
                    }
                }
            };

            var errors = GameDataValidator.ValidateReactionSessionRules(
                sessions,
                "Validator Test");

            Assert.That(errors.Count, Is.EqualTo(2));
            Assert.That(
                errors,
                Has.Some.Contains(
                    "Validator Test[boolean-session] 的 TimingRule 重複：BeforeTurnEnd"));
            Assert.That(
                errors,
                Has.Some.Contains(
                    "Validator Test[integer-session] 的 TimingRule 重複：CardPlayResult"));
        }

        private static void AssertNoErrors(IReadOnlyCollection<string> errors)
        {
            Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
        }
    }
}
