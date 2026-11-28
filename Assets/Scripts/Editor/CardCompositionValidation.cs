using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Editor
{
    public static partial class GameDataValidator
    {
        private static void _ValidateCardComposition(
            CardData card, string context, GameContentCatalog catalog, ICollection<ValidationIssue> errors)
        {
            if (card == null) return;
            var templatesById = catalog.CardAssets
                .OfType<StandardCardDataScriptable>()
                .Where(asset => asset.Data != null && !string.IsNullOrWhiteSpace(asset.Data.ID))
                .GroupBy(asset => asset.Data.ID).ToDictionary(group => group.Key, group => group.First().Data);
            var precedingTemplates = new Dictionary<string, CardTemplateSelectionGroup>();
            foreach (var group in card.SubSelects ?? new List<ISubSelectionGroup>())
            {
                if (group is CardTemplateSelectionGroup templates)
                {
                    if (templates.OfferCount <= 0)
                        errors.Add(new ValidationIssue.NonPositiveTemplateOfferCount(context, templates.Id));
                    if (templates.TemplateCardDataIds == null || templates.TemplateCardDataIds.Count == 0)
                        errors.Add(new ValidationIssue.EmptyTemplateCardIds(context, templates.Id));
                    else
                    {
                        if (templates.TemplateCardDataIds.Distinct().Count() != templates.TemplateCardDataIds.Count)
                            errors.Add(new ValidationIssue.DuplicateTemplateCardId(context, templates.Id));
                        foreach (var id in templates.TemplateCardDataIds)
                            if (string.IsNullOrWhiteSpace(id) || !templatesById.ContainsKey(id))
                                errors.Add(new ValidationIssue.InvalidTemplateCardType(context, templates.Id, id));
                    }
                    if (!string.IsNullOrWhiteSpace(templates.Id)) precedingTemplates[templates.Id] = templates;
                }
                if (group is not CardEffectSelectionGroup effects) continue;
                var effectContext = $"{context}[{effects.Id}]";
                if (effects.OfferCount <= 0) errors.Add(new ValidationIssue.NonPositiveEffectOfferCount(effectContext));
                if (string.IsNullOrWhiteSpace(effects.TemplateSelectionId) ||
                    !precedingTemplates.TryGetValue(effects.TemplateSelectionId, out var templateGroup))
                {
                    errors.Add(new ValidationIssue.InvalidTemplateSelectionReference(effectContext));
                    continue;
                }
                var templateIds = templateGroup.TemplateCardDataIds ?? new List<string>();
                if (effects.PoolsByTemplateId == null) continue;
                foreach (var templateId in templateIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                    if (!effects.PoolsByTemplateId.ContainsKey(templateId))
                        errors.Add(new ValidationIssue.MissingTemplatePool(effectContext, templateId));
                foreach (var pair in effects.PoolsByTemplateId)
                {
                    if (!templateIds.Contains(pair.Key)) errors.Add(new ValidationIssue.UnexpectedTemplatePool(effectContext, pair.Key));
                    var pool = pair.Value;
                    if (pool == null) continue;
                    if (pool.Count == 0) errors.Add(new ValidationIssue.EmptyTemplatePool(effectContext, pair.Key));
                    var ids = new HashSet<string>();
                    foreach (var option in pool.Where(option => option != null))
                    {
                        if (string.IsNullOrWhiteSpace(option.Id)) errors.Add(new ValidationIssue.EmptyEffectOptionId(effectContext));
                        else if (!ids.Add(option.Id)) errors.Add(new ValidationIssue.DuplicateEffectOptionId(effectContext, option.Id));
                        if (string.IsNullOrWhiteSpace(option.DescriptionKey)) errors.Add(new ValidationIssue.MissingDescriptionKey(effectContext));
                        var effectValues = SerializedDataGraphUtility.Find<IIntegerValue>(option.Effects)
                            .Concat(SerializedDataGraphUtility.Find<IIntegerValue>(option.TriggeredEffects)).ToArray();
                        foreach (var parameter in option.DescriptionParameters ?? new())
                        {
                            if (!Regex.IsMatch(parameter.Key, @"^[a-zA-Z][a-zA-Z0-9_]*$") ||
                                parameter.Key is "cost" or "power")
                                errors.Add(new ValidationIssue.InvalidDescriptionParameterName(effectContext, option.Id, parameter.Key));
                            if (parameter.Value == null || !effectValues.Any(value => ReferenceEquals(value, parameter.Value.Value)))
                                errors.Add(new ValidationIssue.InvalidDescriptionParameterValue(effectContext, option.Id, parameter.Key));
                            else if (!ValueConditionValidator.TryEvaluateStaticInteger(parameter.Value.Value, out _) &&
                                     string.IsNullOrWhiteSpace(parameter.Value.RuleKey))
                                errors.Add(new ValidationIssue.MissingDescriptionRuleKey(effectContext, option.Id, parameter.Key));
                        }
                        if ((option.Effects?.Count ?? 0) == 0 &&
                            !(option.TriggeredEffects?.Any(entry => (entry.Value?.Length ?? 0) > 0) ?? false))
                            errors.Add(new ValidationIssue.EmptyEffectOption(effectContext, option.Id));
                        if (!templatesById.TryGetValue(pair.Key, out var template)) continue;
                        // 選項會在成品執行，子選取引用必須屬於模板，而非合成來源卡。
                        _ValidateCardNestedSemantics(new StandardCardData
                        {
                            ID = template.ID,
                            SubSelects = template.SubSelects,
                            Effects = option.Effects,
                            TriggeredEffects = option.TriggeredEffects
                        }, $"{effectContext}[{pair.Key}/{option.Id}]", errors);
                    }
                }
            }
        }

        private static void _ValidateCompositionDescriptions(CardData card, ExcelDatas localization, ICollection<ValidationIssue> errors)
        {
            var texts = (localization.LocalizeUI ?? new()).Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .GroupBy(row => row.Id).ToDictionary(group => group.Key, group => group.First().Info);
            foreach (var option in SerializedDataGraphUtility.Find<CardEffectOption>(card))
            {
                if (option.DescriptionKey == null || !texts.TryGetValue(option.DescriptionKey, out var text)) continue;
                var parameters = option.DescriptionParameters ?? new();
                foreach (Match match in Regex.Matches(text ?? string.Empty, @"\{:([a-zA-Z][a-zA-Z0-9_]*)\}"))
                    if (match.Groups[1].Value is not ("cost" or "power") && !parameters.ContainsKey(match.Groups[1].Value))
                        errors.Add(new ValidationIssue.MissingDescriptionParameter(card.ID, option.DescriptionKey, match.Groups[1].Value));
                foreach (var parameter in parameters.Values.Where(parameter => parameter != null && !string.IsNullOrEmpty(parameter.RuleKey)))
                    if (!texts.ContainsKey(parameter.RuleKey))
                        errors.Add(new ValidationIssue.MissingDescriptionRuleKeyReference(card.ID, parameter.RuleKey));
            }
        }
    }
}
