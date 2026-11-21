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
            CardData card, string context, GameContentCatalog catalog, ICollection<string> errors)
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
                        errors.Add($"{context}[{templates.Id}].OfferCount 必須大於零");
                    if (templates.TemplateCardDataIds == null || templates.TemplateCardDataIds.Count == 0)
                        errors.Add($"{context}[{templates.Id}].TemplateCardDataIds 至少需要一項");
                    else
                    {
                        if (templates.TemplateCardDataIds.Distinct().Count() != templates.TemplateCardDataIds.Count)
                            errors.Add($"{context}[{templates.Id}] 的模板 ID 重複");
                        foreach (var id in templates.TemplateCardDataIds)
                            if (string.IsNullOrWhiteSpace(id) || !templatesById.ContainsKey(id))
                                errors.Add($"{context}[{templates.Id}] 的模板必須是 Catalog 中的 StandardCardData：{id}");
                    }
                    if (!string.IsNullOrWhiteSpace(templates.Id)) precedingTemplates[templates.Id] = templates;
                }
                if (group is not CardEffectSelectionGroup effects) continue;
                var effectContext = $"{context}[{effects.Id}]";
                if (effects.OfferCount <= 0) errors.Add($"{effectContext}.OfferCount 必須大於零");
                if (string.IsNullOrWhiteSpace(effects.TemplateSelectionId) ||
                    !precedingTemplates.TryGetValue(effects.TemplateSelectionId, out var templateGroup))
                {
                    errors.Add($"{effectContext}.TemplateSelectionId 必須引用前面的模板群組");
                    continue;
                }
                var templateIds = templateGroup.TemplateCardDataIds ?? new List<string>();
                if (effects.PoolsByTemplateId == null) continue;
                foreach (var templateId in templateIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                    if (!effects.PoolsByTemplateId.ContainsKey(templateId))
                        errors.Add($"{effectContext} 缺少模板候選池：{templateId}");
                foreach (var pair in effects.PoolsByTemplateId)
                {
                    if (!templateIds.Contains(pair.Key)) errors.Add($"{effectContext} 含未提供的模板候選池：{pair.Key}");
                    var pool = pair.Value;
                    if (pool == null) continue;
                    if (pool.Count == 0) errors.Add($"{effectContext}[{pair.Key}] 候選池不可為空");
                    var ids = new HashSet<string>();
                    foreach (var option in pool.Where(option => option != null))
                    {
                        if (string.IsNullOrWhiteSpace(option.Id)) errors.Add($"{effectContext} 的選項 Id 為空");
                        else if (!ids.Add(option.Id)) errors.Add($"{effectContext} 的選項 Id 重複：{option.Id}");
                        if (string.IsNullOrWhiteSpace(option.DescriptionKey)) errors.Add($"{effectContext} 的 DescriptionKey 為空");
                        var effectValues = SerializedDataGraphUtility.Find<IIntegerValue>(option.Effects)
                            .Concat(SerializedDataGraphUtility.Find<IIntegerValue>(option.TriggeredEffects)).ToArray();
                        foreach (var parameter in option.DescriptionParameters ?? new())
                        {
                            if (!Regex.IsMatch(parameter.Key, @"^[a-zA-Z][a-zA-Z0-9_]*$") ||
                                parameter.Key is "cost" or "power")
                                errors.Add($"{effectContext}[{option.Id}] 的描述參數名稱無效或保留：{parameter.Key}");
                            if (parameter.Value == null || !effectValues.Any(value => ReferenceEquals(value, parameter.Value.Value)))
                                errors.Add($"{effectContext}[{option.Id}].DescriptionParameters[{parameter.Key}] 必須直接引用本選項效果的 Value");
                            else if (!ValueConditionValidator.TryEvaluateStaticInteger(parameter.Value.Value, out _) &&
                                     string.IsNullOrWhiteSpace(parameter.Value.RuleKey))
                                errors.Add($"{effectContext}[{option.Id}].DescriptionParameters[{parameter.Key}].RuleKey 不可為空");
                        }
                        if ((option.Effects?.Count ?? 0) == 0 &&
                            !(option.TriggeredEffects?.Any(entry => (entry.Value?.Length ?? 0) > 0) ?? false))
                            errors.Add($"{effectContext}[{option.Id}] 至少需要一個普通或觸發效果");
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

        private static void _ValidateCompositionDescriptions(CardData card, ExcelDatas localization, ICollection<string> errors)
        {
            var texts = (localization.LocalizeUI ?? new()).Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .GroupBy(row => row.Id).ToDictionary(group => group.Key, group => group.First().Info);
            foreach (var option in SerializedDataGraphUtility.Find<CardEffectOption>(card))
            {
                if (option.DescriptionKey == null || !texts.TryGetValue(option.DescriptionKey, out var text)) continue;
                var parameters = option.DescriptionParameters ?? new();
                foreach (Match match in Regex.Matches(text ?? string.Empty, @"\{:([a-zA-Z][a-zA-Z0-9_]*)\}"))
                    if (match.Groups[1].Value is not ("cost" or "power") && !parameters.ContainsKey(match.Groups[1].Value))
                        errors.Add($"CardData[{card.ID}] 的描述 {option.DescriptionKey} 缺少參數：{match.Groups[1].Value}");
                foreach (var parameter in parameters.Values.Where(parameter => parameter != null && !string.IsNullOrEmpty(parameter.RuleKey)))
                    if (!texts.ContainsKey(parameter.RuleKey))
                        errors.Add($"CardData[{card.ID}] 的描述規則鍵不存在：{parameter.RuleKey}");
            }
        }
    }
}
