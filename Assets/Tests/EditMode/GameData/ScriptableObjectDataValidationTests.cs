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
