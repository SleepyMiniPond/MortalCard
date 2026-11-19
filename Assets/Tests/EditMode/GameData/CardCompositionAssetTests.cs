using System;
using System.Collections.Generic;
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
    public sealed class CardCompositionAssetTests
    {
        [Test]
        public void CompositionConfiguration_SurvivesSaveUnloadReload()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/EditMode/GameData/CompositionRoundTrip.asset");
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            try
            {
                card.Data.ID = "composition-round-trip";
                _Configure(card.Data);
                AssetDatabase.CreateAsset(card, path);
                AssetDatabase.SaveAssetIfDirty(card);
                Resources.UnloadAsset(card);
                card = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                card = AssetDatabase.LoadAssetAtPath<StandardCardDataScriptable>(path);
                var templates = (CardTemplateSelectionGroup)card.Data.SubSelects[0];
                var effects = (CardEffectSelectionGroup)card.Data.SubSelects[1];
                Assert.That(templates.OfferCount, Is.EqualTo(2));
                Assert.That(effects.OfferCount, Is.EqualTo(4));
                Assert.That(effects.ExcludePreviouslySelected, Is.True);
                Assert.That(effects.TemplateSelectionId, Is.EqualTo("template"));
                var option = effects.PoolsByTemplateId["template"][0];
                Assert.That(option.DescriptionKey, Is.EqualTo("Composition.Draw"));
                Assert.That(option.Effects[0], Is.TypeOf<DrawCardEffect>());
                Assert.That(option.TriggeredEffects[CardTriggeredTiming.Played][0].Conditions[0],
                    Is.TypeOf<ConstCondition>());
            }
            finally
            {
                if (card != null && !AssetDatabase.Contains(card)) UnityEngine.Object.DestroyImmediate(card);
                // 僅刪除本測試建立的唯一暫存資產。
                AssetDatabase.DeleteAsset(path);
            }
        }

        [TestCase("valid", null)]
        [TestCase("missing-template", "StandardCardData")]
        [TestCase("duplicate-template", "模板 ID 重複")]
        [TestCase("invalid-reference", "前面的模板群組")]
        [TestCase("later-reference", "前面的模板群組")]
        [TestCase("missing-pool", "缺少模板候選池")]
        [TestCase("extra-pool", "未提供的模板候選池")]
        [TestCase("empty-pool", "候選池不可為空")]
        [TestCase("duplicate-option", "選項 Id 重複")]
        [TestCase("missing-description", "DescriptionKey 為空")]
        [TestCase("empty-effect", "至少需要一個")]
        [TestCase("invalid-offer-count", "OfferCount 必須大於零")]
        [TestCase("unconfigured-template-offer", "[template].OfferCount 必須大於零")]
        [TestCase("unconfigured-effect-offer", "[effect].OfferCount 必須大於零")]
        [TestCase("invalid-timing", "TriggeredEffects")]
        [TestCase("missing-nested-value", "Value 為空")]
        public void Validator_ChecksCompositionStructureAndNestedEffects(string scenario, string expected)
        {
            _WithCatalog((card, template, catalog) =>
            {
                var group = (CardEffectSelectionGroup)card.Data.SubSelects[1];
                var templates = (CardTemplateSelectionGroup)card.Data.SubSelects[0];
                var option = group.PoolsByTemplateId["template"][0];
                switch (scenario)
                {
                    case "missing-template": templates.TemplateCardDataIds[0] = "unknown"; break;
                    case "duplicate-template": templates.TemplateCardDataIds.Add("template"); break;
                    case "invalid-reference": group.TemplateSelectionId = "unknown"; break;
                    case "later-reference": card.Data.SubSelects.Reverse(); break;
                    case "missing-pool": group.PoolsByTemplateId.Clear(); break;
                    case "extra-pool": group.PoolsByTemplateId["other"] = new() { option }; break;
                    case "empty-pool": group.PoolsByTemplateId["template"].Clear(); break;
                    case "duplicate-option": group.PoolsByTemplateId["template"].Add(option); break;
                    case "missing-description": option.DescriptionKey = ""; break;
                    case "empty-effect": option.Effects.Clear(); option.TriggeredEffects.Clear(); break;
                    case "invalid-offer-count": group.OfferCount = 0; break;
                    case "unconfigured-template-offer":
                        card.Data.SubSelects[0] = new CardTemplateSelectionGroup
                        {
                            Id = templates.Id, TemplateCardDataIds = templates.TemplateCardDataIds
                        };
                        break;
                    case "unconfigured-effect-offer":
                        card.Data.SubSelects[1] = new CardEffectSelectionGroup
                        {
                            Id = group.Id, TemplateSelectionId = group.TemplateSelectionId,
                            PoolsByTemplateId = group.PoolsByTemplateId
                        };
                        break;
                    case "invalid-timing": option.TriggeredEffects[(CardTriggeredTiming)999] = Array.Empty<ConditionalCardEffect>(); break;
                    case "missing-nested-value": ((DrawCardEffect)option.Effects[0]).Value = null; break;
                }
                var errors = GameDataValidator.ValidateNestedContent(catalog);
                if (expected == null) Assert.That(errors, Is.Empty);
                else Assert.That(errors, Has.Some.Contains(expected));
            });
        }

        [Test]
        public void Validator_UsesTemplateSubSelectionScopeForAppendedEffects()
        {
            _WithCatalog((card, template, catalog) =>
            {
                template.Data.SubSelects.Add(new ExistCardSelectionGroup
                {
                    Id = "target", CountRule = CardSelectionCountRule.RequiredExact,
                    SelectCount = new ConstInteger { Value = 1 },
                    CardCandidates = new SingleCardCollection { TargetCard = new TriggeredCard() }
                });
                var option = ((CardEffectSelectionGroup)card.Data.SubSelects[1]).PoolsByTemplateId["template"][0];
                option.Effects.Add(new DiscardCardEffect
                {
                    TargetCards = new SubSelectedCardCollection { SelectionId = "target" }
                });
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
                ((SubSelectedCardCollection)((DiscardCardEffect)option.Effects.Last()).TargetCards).SelectionId = "missing";
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Has.Some.Contains("找不到 ExistCard 群組"));
            });
        }

        [Test]
        public void Validator_ChecksOptionResolversAndReferenceIds()
        {
            _WithCatalog((card, template, catalog) =>
            {
                var option = ((CardEffectSelectionGroup)card.Data.SubSelects[1]).PoolsByTemplateId["template"][0];
                option.Effects.Add(new UnknownEffect());
                Assert.That(GameDataValidator.ValidateEffectResolvers(catalog), Has.Some.Contains("UnknownEffect"));
                option.Effects.Add(new CreateCardEffect
                {
                    Target = new CardOwner { Card = new TriggeredCard() }, CardDataIds = new() { "unknown" },
                    CreateDestination = CardCollectionType.HandCard
                });
                Assert.That(GameDataValidator.ValidateReferenceIds(catalog), Has.Some.Contains("不存在的 ID"));
            });
        }

        [Test]
        public void Validator_ChecksOptionDescriptionLocalization()
        {
            _WithCatalog((card, template, catalog) =>
            {
                var localization = ScriptableObject.CreateInstance<ExcelDatas>();
                try
                {
                    localization.LocalizeUI = new();
                    localization.LocalizeCard = new()
                    {
                        new() { Id = card.Data.ID, Title = "合成卡", Info = "測試" },
                        new() { Id = template.Data.ID, Title = "模板", Info = "測試" }
                    };
                    localization.LocalizeCardBuff = new();
                    localization.LocalizePlayerBuff = new();
                    localization.LocalizePlayer = new();
                    localization.LocalizeKeyWord = new();
                    Assert.That(GameDataValidator.ValidateLocalization(catalog, localization, Array.Empty<PlayerData>()),
                        Has.Some.Contains("CardEffectOption.DescriptionKey"));
                    localization.LocalizeUI.Add(new LocalizeExcelData { Id = "Composition.Draw", Info = "抽牌" });
                    Assert.That(GameDataValidator.ValidateLocalization(catalog, localization, Array.Empty<PlayerData>()), Is.Empty);
                }
                finally { UnityEngine.Object.DestroyImmediate(localization); }
            });
        }

        private static void _Configure(StandardCardData data)
        {
            data.SubSelects.Add(new CardTemplateSelectionGroup
            {
                Id = "template", OfferCount = 2, TemplateCardDataIds = new() { "template" }
            });
            data.SubSelects.Add(new CardEffectSelectionGroup
            {
                Id = "effect", TemplateSelectionId = "template", OfferCount = 4,
                PoolsByTemplateId = new()
                {
                    ["template"] = new()
                    {
                        new CardEffectOption
                        {
                            Id = "draw", DescriptionKey = "Composition.Draw",
                            Effects = new() { _Draw() },
                            TriggeredEffects = new()
                            {
                                [CardTriggeredTiming.Played] = new[]
                                {
                                    new ConditionalCardEffect
                                    {
                                        Conditions = new() { new ConstCondition { Value = true } }, Effect = _Draw()
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private static DrawCardEffect _Draw() => new()
        {
            Targets = new NonePlayers(), Value = new ConstInteger { Value = 1 }
        };

        private static void _WithCatalog(Action<StandardCardDataScriptable, StandardCardDataScriptable, GameContentCatalog> action)
        {
            var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var template = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            try
            {
                card.Data.ID = "composition";
                template.Data.ID = "template";
                _Configure(card.Data);
                typeof(GameContentCatalog).GetField("_cardAssets", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(catalog, new CardDataScriptableBase[] { card, template });
                action(card, template, catalog);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(card);
                UnityEngine.Object.DestroyImmediate(template);
            }
        }

        [Serializable]
        private sealed class UnknownEffect : ICardEffect { }
    }
}
