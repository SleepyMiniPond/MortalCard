using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using UnityEditor;

namespace MortalGame.Editor
{
    public static class GameDataValidator
    {
        public static IReadOnlyList<string> ValidateAll()
        {
            var catalog = _LoadDefaultCatalog();
            if (catalog == null)
            {
                return new[] { _GetMissingCatalogError() }
                    .Concat(ValidateEffectCommandHandlers())
                    .Distinct()
                    .ToArray();
            }

            return ValidateCatalogCoverage(
                    catalog,
                    ProjectAssetPaths.GameContent.SearchFolders)
                .Concat(_ValidateContentIntegrity(catalog))
                .Concat(ValidateCardScriptableTypes(
                    catalog.CardAssets,
                    EditorAssetUtility.FindAssets<DeckScriptable>(
                        ProjectAssetPaths.GameContent.SearchFolders)))
                .Concat(ValidateEffectCommandHandlers())
                .Concat(_ValidateEffectResolvers(catalog))
                .Concat(_ValidateCardTransformRules(catalog))
                .Concat(_ValidateReactionSessionRules(catalog))
                .Distinct()
                .ToArray();
        }

        public static IReadOnlyList<string> ValidateContentIntegrity()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateContentIntegrity(catalog);
        }

        public static IReadOnlyList<string> ValidateNestedContent(
            GameContentCatalog catalog)
        {
            if (catalog == null)
                return new[] { "GameContentCatalog 為空，無法驗證巢狀內容" };

            var errors = new List<string>();

            foreach (var asset in catalog.CardAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / CardData";
                errors.AddRange(
                    SerializedDataGraphUtility.ValidateRequiredReferences(
                        asset.CardData,
                        context));
                _ValidateIntegerValueSemantics(asset.CardData, context, errors);
                _ValidateCardCollectionSemantics(asset.CardData, context, errors);
                _ValidateTargetSemantics(asset.CardData, context, errors);
                _ValidateCardNestedSemantics(asset.CardData, context, errors);
            }

            foreach (var asset in catalog.CardBuffAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / CardBuffData";
                errors.AddRange(
                    SerializedDataGraphUtility.ValidateRequiredReferences(
                        asset.Data,
                        context));
                _ValidateIntegerValueSemantics(asset.Data, context, errors);
                _ValidateCardCollectionSemantics(asset.Data, context, errors);
                _ValidateTargetSemantics(asset.Data, context, errors);
                _ValidateCardBuffNestedSemantics(asset.Data, context, errors);
            }

            foreach (var asset in catalog.PlayerBuffAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / PlayerBuffData";
                errors.AddRange(
                    SerializedDataGraphUtility.ValidateRequiredReferences(
                        asset.Data,
                        context));
                _ValidateIntegerValueSemantics(asset.Data, context, errors);
                _ValidateCardCollectionSemantics(asset.Data, context, errors);
                _ValidateTargetSemantics(asset.Data, context, errors);
                _ValidatePlayerBuffNestedSemantics(asset.Data, context, errors);
            }

            foreach (var asset in catalog.CharacterBuffAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / CharacterBuffData";
                errors.AddRange(
                    SerializedDataGraphUtility.ValidateRequiredReferences(
                        asset.Data,
                        context));
                _ValidateIntegerValueSemantics(asset.Data, context, errors);
                _ValidateCardCollectionSemantics(asset.Data, context, errors);
                _ValidateTargetSemantics(asset.Data, context, errors);
                _ValidateCharacterBuffNestedSemantics(asset.Data, context, errors);
            }

            _ValidateContentIds(catalog, errors);
            return errors.Distinct().ToArray();
        }

        public static IReadOnlyList<string> ValidateLocalization(
            GameContentCatalog catalog,
            ExcelDatas localizationData,
            IEnumerable<PlayerData> playerDatas)
        {
            if (catalog == null)
                return new[] { "GameContentCatalog 為空，無法驗證 Localization" };
            if (localizationData == null)
                return new[] { "ExcelDatas 為空，無法驗證 Localization" };

            var errors = new List<string>();
            var cardKeys = _ValidateTitleInfoTable(
                localizationData.LocalizeCard,
                nameof(ExcelDatas.LocalizeCard),
                true,
                errors);
            var cardBuffKeys = _ValidateTitleInfoTable(
                localizationData.LocalizeCardBuff,
                nameof(ExcelDatas.LocalizeCardBuff),
                true,
                errors);
            var playerBuffKeys = _ValidateTitleInfoTable(
                localizationData.LocalizePlayerBuff,
                nameof(ExcelDatas.LocalizePlayerBuff),
                true,
                errors);
            var playerKeys = _ValidateTitleInfoTable(
                localizationData.LocalizePlayer,
                nameof(ExcelDatas.LocalizePlayer),
                true,
                errors);
            _ValidateTitleInfoTable(
                localizationData.LocalizeKeyWord,
                nameof(ExcelDatas.LocalizeKeyWord),
                false,
                errors);
            _ValidateInfoTable(
                localizationData.LocalizeUI,
                nameof(ExcelDatas.LocalizeUI),
                errors);

            _ValidateLocalizationKeys(
                catalog.CardAssets
                    .Where(asset => asset != null && asset.CardData != null)
                    .Select(asset => (
                        asset.CardData.ID,
                        $"{AssetDatabase.GetAssetPath(asset)} / CardData[{asset.CardData.ID}]")),
                cardKeys,
                nameof(ExcelDatas.LocalizeCard),
                errors);
            _ValidateLocalizationKeys(
                catalog.CardBuffAssets
                    .Where(asset => asset != null && asset.Data != null)
                    .Select(asset => (
                        asset.Data.ID,
                        $"{AssetDatabase.GetAssetPath(asset)} / CardBuffData[{asset.Data.ID}]")),
                cardBuffKeys,
                nameof(ExcelDatas.LocalizeCardBuff),
                errors);
            _ValidateLocalizationKeys(
                catalog.PlayerBuffAssets
                    .Where(asset => asset != null && asset.Data != null)
                    .Select(asset => (
                        asset.Data.ID,
                        $"{AssetDatabase.GetAssetPath(asset)} / PlayerBuffData[{asset.Data.ID}]")),
                playerBuffKeys,
                nameof(ExcelDatas.LocalizePlayerBuff),
                errors);
            _ValidateLocalizationKeys(
                (playerDatas ?? Enumerable.Empty<PlayerData>())
                    .Where(data => data != null)
                    .Select(data => (
                        data.NameKey,
                        $"PlayerData[{data.ID}].NameKey")),
                playerKeys,
                nameof(ExcelDatas.LocalizePlayer),
                errors);

            return errors.Distinct().ToArray();
        }

        public static IReadOnlyList<string> ValidateCatalogCoverage()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : ValidateCatalogCoverage(
                    catalog,
                    ProjectAssetPaths.GameContent.SearchFolders);
        }

        public static IReadOnlyList<string> ValidateCatalogCoverage(
            GameContentCatalog catalog,
            IReadOnlyList<string> searchFolders)
        {
            if (catalog == null)
                return new[] { "GameContentCatalog 為空，無法驗證內容覆蓋" };

            var errors = new List<string>();
            _ValidateCatalogCollection(
                catalog.CardAssets,
                EditorAssetUtility.FindAssets<CardDataScriptableBase>(searchFolders),
                nameof(GameContentCatalog.CardAssets),
                errors);
            _ValidateCatalogCollection(
                catalog.CardBuffAssets,
                EditorAssetUtility.FindAssets<CardBuffScriptable>(searchFolders),
                nameof(GameContentCatalog.CardBuffAssets),
                errors);
            _ValidateCatalogCollection(
                catalog.PlayerBuffAssets,
                EditorAssetUtility.FindAssets<PlayerBuffDataScriptable>(searchFolders),
                nameof(GameContentCatalog.PlayerBuffAssets),
                errors);
            _ValidateCatalogCollection(
                catalog.CharacterBuffAssets,
                EditorAssetUtility.FindAssets<CharacterBuffDataScriptable>(searchFolders),
                nameof(GameContentCatalog.CharacterBuffAssets),
                errors);
            return errors;
        }

        public static IReadOnlyList<string> ValidateReactionSessionRules()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateReactionSessionRules(catalog);
        }

        private static IReadOnlyList<string> _ValidateReactionSessionRules(
            GameContentCatalog catalog)
        {
            var errors = new List<string>();

            foreach (var asset in catalog.PlayerBuffAssets.Where(asset => asset != null))
            {
                if (asset.Data == null)
                    continue;

                errors.AddRange(ValidateReactionSessionRules(
                    asset.Data.Sessions,
                    $"{AssetDatabase.GetAssetPath(asset)} / PlayerBuffData[{asset.Data.ID}].Sessions"));
            }

            foreach (var asset in catalog.CharacterBuffAssets.Where(asset => asset != null))
            {
                if (asset.Data == null)
                    continue;

                errors.AddRange(ValidateReactionSessionRules(
                    asset.Data.Sessions,
                    $"{AssetDatabase.GetAssetPath(asset)} / CharacterBuffData[{asset.Data.ID}].Sessions"));
            }

            foreach (var asset in catalog.CardBuffAssets.Where(asset => asset != null))
            {
                if (asset.Data == null)
                    continue;

                errors.AddRange(ValidateReactionSessionRules(
                    asset.Data.Sessions,
                    $"{AssetDatabase.GetAssetPath(asset)} / CardBuffData[{asset.Data.ID}].Sessions"));
            }

            return errors;
        }

        public static IReadOnlyList<string> ValidateReactionSessionRules(
            IReadOnlyDictionary<string, IReactionSessionData> sessions,
            string context)
        {
            if (sessions == null)
                return Array.Empty<string>();

            return sessions
                .SelectMany(pair => GetDuplicateTimings(pair.Value)
                    .Select(timing =>
                        $"{context}[{pair.Key}] 的 TimingRule 重複：{timing}"))
                .ToArray();
        }

        public static IReadOnlyList<string> ValidateCardTransformRules()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateCardTransformRules(catalog);
        }

        private static IReadOnlyList<string> _ValidateCardTransformRules(
            GameContentCatalog catalog)
        {
            return catalog.CardAssets
                .OfType<StandardCardDataScriptable>()
                .SelectMany(asset => ValidateCardTransformRules(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset)))
                .ToArray();
        }

        public static IReadOnlyList<string> ValidateCardScriptableTypes()
        {
            var catalog = _LoadDefaultCatalog();
            if (catalog == null)
                return new[] { _GetMissingCatalogError() };

            return ValidateCardScriptableTypes(
                catalog.CardAssets,
                EditorAssetUtility.FindAssets<DeckScriptable>(
                    ProjectAssetPaths.GameContent.SearchFolders));
        }

        public static IReadOnlyList<string> ValidateCardScriptableTypes(
            IEnumerable<CardDataScriptableBase> cardAssets,
            IEnumerable<DeckScriptable> deckAssets)
        {
            var cards = (cardAssets ?? Enumerable.Empty<CardDataScriptableBase>())
                .Where(asset => asset != null)
                .ToArray();
            var errors = new List<string>();

            foreach (var duplicateId in cards
                .Where(asset => asset.CardData != null)
                .GroupBy(asset => asset.CardData.ID)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1))
            {
                errors.Add($"CardData ID 在 Standard／Override 資產間重複：{duplicateId.Key}");
            }

            var assetsById = cards
                .Where(asset => asset.CardData != null)
                .Where(asset => !string.IsNullOrWhiteSpace(asset.CardData.ID))
                .GroupBy(asset => asset.CardData.ID)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var cardAsset in cards)
            {
                var context = GetAssetContext(cardAsset);
                var cardData = cardAsset.CardData;
                if (cardData == null)
                {
                    errors.Add($"{context} 的 CardData 為空");
                    continue;
                }

                errors.AddRange(ValidateCardFormOverrideEffects(
                    cardData,
                    context,
                    assetsById));

                if (cardAsset is not StandardCardDataScriptable standardCardAsset)
                    continue;

                foreach (var applyOperation in (standardCardAsset.Data.TransformRules ?? new List<CardTransformRule>())
                    .Where(rule => rule?.Operation is ApplyCardTransformOperationData)
                    .Select(rule => (ApplyCardTransformOperationData)rule.Operation))
                {
                    if (!assetsById.TryGetValue(applyOperation.TargetCardDataId, out var targetAsset))
                    {
                        errors.Add(
                            $"{context} / CardData[{cardData.ID}] 的 Self Transform Target " +
                            $"指向不存在的 CardData：{applyOperation.TargetCardDataId}");
                    }
                    else if (targetAsset is not StandardCardDataScriptable)
                    {
                        errors.Add(
                            $"{context} / CardData[{cardData.ID}] 的 Self Transform Target " +
                            $"必須是 Standard CardData：{applyOperation.TargetCardDataId}");
                    }
                }
            }

            foreach (var deck in deckAssets ?? Enumerable.Empty<DeckScriptable>())
            {
                if (deck == null)
                    continue;

                if ((deck.Cards ?? Array.Empty<StandardCardDataScriptable>()).Any(card => card == null))
                    errors.Add($"{GetAssetContext(deck)} 的 DeckScriptable.Cards 含有空卡牌引用");
            }

            return errors;
        }

        private static IReadOnlyList<string> ValidateCardFormOverrideEffects(
            CardData cardData,
            string context,
            IReadOnlyDictionary<string, CardDataScriptableBase> assetsById)
        {
            var errors = new List<string>();
            var effects = EnumerateCardEffects(cardData)
                .OfType<ApplyCardFormOverrideEffect>()
                .ToArray();

            for (var index = 0; index < effects.Length; index++)
            {
                var effect = effects[index];
                var effectContext =
                    $"{context} / CardData[{cardData.ID}].ApplyCardFormOverrideEffect[{index}]";

                if (effect.TargetCards == null)
                    errors.Add($"{effectContext} 的 TargetCards 為空");
                if (string.IsNullOrWhiteSpace(effect.OverrideKey))
                    errors.Add($"{effectContext} 的 OverrideKey 為空");

                if (string.IsNullOrWhiteSpace(effect.TargetCardDataId))
                {
                    errors.Add($"{effectContext} 的 TargetCardDataId 為空");
                }
                else if (!assetsById.TryGetValue(effect.TargetCardDataId, out var targetAsset))
                {
                    errors.Add(
                        $"{effectContext} 指向不存在的 Override CardData：{effect.TargetCardDataId}");
                }
                else if (targetAsset is not OverrideCardDataScriptable)
                {
                    errors.Add(
                        $"{effectContext} 的 Target 必須是 Override CardData：{effect.TargetCardDataId}");
                }

                var releaseRules = effect.ReleaseRules;
                if (releaseRules == null || releaseRules.Count == 0)
                {
                    errors.Add($"{effectContext} 的 ReleaseRules 不可為空");
                }
                else
                {
                    for (var ruleIndex = 0; ruleIndex < releaseRules.Count; ruleIndex++)
                    {
                        var rule = releaseRules[ruleIndex];
                        var ruleContext = $"{effectContext}.ReleaseRules[{ruleIndex}]";
                        if (rule == null)
                        {
                            errors.Add($"{ruleContext} 為空");
                            continue;
                        }

                        if (rule.Timing == GameTiming.None)
                            errors.Add($"{ruleContext} 的 Timing 不可為 None");
                        else if (!IsTimingDispatchSupported(rule.Timing))
                            errors.Add($"{ruleContext} 的 Timing 不會進入 Timing Dispatch：{rule.Timing}");

                        ValidateOverrideReleaseRuleConditions(
                            rule.Conditions,
                            ruleContext,
                            effect.ReactionSessions,
                            errors);
                    }
                }

                if (effect.ReactionSessions == null)
                {
                    errors.Add($"{effectContext} 的 ReactionSessions 為空");
                    continue;
                }

                foreach (var session in effect.ReactionSessions)
                {
                    if (string.IsNullOrWhiteSpace(session.Key))
                        errors.Add($"{effectContext} 的 ReactionSession Key 為空");
                    if (session.Value == null)
                        errors.Add($"{effectContext}.ReactionSessions[{session.Key}] 的資料為空");
                }

                errors.AddRange(ValidateReactionSessionRules(
                    effect.ReactionSessions,
                    $"{effectContext}.ReactionSessions"));
            }

            return errors;
        }

        private static void ValidateOverrideReleaseRuleConditions(
            IReadOnlyList<ICondition> conditions,
            string context,
            IReadOnlyDictionary<string, IReactionSessionData> reactionSessions,
            ICollection<string> errors)
        {
            if (conditions == null)
            {
                errors.Add($"{context} 的 Conditions 含有空值");
                return;
            }

            for (var index = 0; index < conditions.Count; index++)
            {
                var condition = conditions[index];
                var conditionContext = $"{context}.Conditions[{index}]";
                if (condition == null)
                {
                    errors.Add($"{context} 的 Conditions 含有空值");
                    continue;
                }

                switch (condition)
                {
                    case AllCondition all:
                        ValidateOverrideReleaseRuleConditions(
                            all.Conditions,
                            conditionContext,
                            reactionSessions,
                            errors);
                        break;
                    case AnyCondition any:
                        ValidateOverrideReleaseRuleConditions(
                            any.Conditions,
                            conditionContext,
                            reactionSessions,
                            errors);
                        break;
                    case InverseCondition inverse:
                        ValidateOverrideReleaseRuleConditions(
                            new[] { inverse.Condition },
                            conditionContext,
                            reactionSessions,
                            errors);
                        break;
                    case CardFormOverrideSessionCondition sessionCondition:
                        if (string.IsNullOrWhiteSpace(sessionCondition.SessionKey))
                        {
                            errors.Add($"{conditionContext} 的 Override SessionKey 為空");
                        }
                        else if (reactionSessions == null ||
                            !reactionSessions.ContainsKey(sessionCondition.SessionKey))
                        {
                            errors.Add(
                                $"{conditionContext} 引用不存在的 Override SessionKey：" +
                                sessionCondition.SessionKey);
                        }

                        if (sessionCondition.Conditions == null ||
                            sessionCondition.Conditions.Any(valueCondition => valueCondition == null))
                        {
                            errors.Add($"{conditionContext} 的 Session Conditions 含有空值");
                        }
                        break;
                }
            }
        }

        public static IReadOnlyList<string> ValidateCardTransformRules(
            StandardCardData cardData,
            string context)
        {
            if (cardData == null)
                return new[] { $"{context} 的 CardData 為空" };

            var rules = cardData.TransformRules ?? new List<CardTransformRule>();
            var errors = new List<string>();
            var validRules = rules.Where(rule => rule != null).ToArray();

            if (rules.Any(rule => rule == null))
                errors.Add($"{context} / CardData[{cardData.ID}].TransformRules 含有空規則");

            var transformKeys = validRules
                .Select(rule => rule.TransformKey)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct()
                .ToArray();
            if (transformKeys.Length > 1)
                errors.Add($"{context} / CardData[{cardData.ID}].TransformRules 第一版只能使用一個 TransformKey");

            foreach (var ruleGroup in validRules
                .Where(rule => !string.IsNullOrWhiteSpace(rule.RuleId))
                .GroupBy(rule => rule.RuleId)
                .Where(group => group.Count() > 1))
            {
                errors.Add($"{context} / CardData[{cardData.ID}].TransformRules 的 RuleId 重複：{ruleGroup.Key}");
            }

            foreach (var rule in validRules)
            {
                var ruleContext = $"{context} / CardData[{cardData.ID}].TransformRules[{rule.RuleId}]";
                if (string.IsNullOrWhiteSpace(rule.RuleId))
                    errors.Add($"{ruleContext} 的 RuleId 為空");
                if (string.IsNullOrWhiteSpace(rule.TransformKey))
                    errors.Add($"{ruleContext} 的 TransformKey 為空");
                if (rule.Timing == GameTiming.None)
                    errors.Add($"{ruleContext} 的 Timing 不可為 None");
                else if (!IsTimingDispatchSupported(rule.Timing))
                    errors.Add($"{ruleContext} 的 Timing 不會進入 Timing Dispatch：{rule.Timing}");
                if (rule.Conditions == null || rule.Conditions.Any(condition => condition == null))
                    errors.Add($"{ruleContext} 的 Conditions 含有空值");
                switch (rule.Operation)
                {
                    case null:
                        errors.Add($"{ruleContext} 的 Operation 為空");
                        break;
                    case ApplyCardTransformOperationData apply
                        when string.IsNullOrWhiteSpace(apply.TargetCardDataId):
                        errors.Add($"{ruleContext} 的 Apply 缺少 TargetCardDataId");
                        break;
                }
            }

            return errors;
        }

        public static IReadOnlyList<string> ValidateEffectCommandHandlers()
        {
            return GetConcreteTypes<IEffectCommand>()
                .Where(type => !EffectCommandExecutor.HasEffectCommandHandler(type))
                .Select(type => $"{type.Name} 缺少 IEffectCommandHandler 註冊")
                .ToArray();
        }

        public static IReadOnlyList<string> ValidateEffectResolvers()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateEffectResolvers(catalog);
        }

        private static IReadOnlyList<string> _ValidateEffectResolvers(
            GameContentCatalog catalog)
        {
            var errors = new List<string>();

            foreach (var cardAsset in catalog.CardAssets.Where(asset => asset != null))
                ValidateCardEffects(cardAsset.CardData, AssetDatabase.GetAssetPath(cardAsset), errors);

            foreach (var buffAsset in catalog.PlayerBuffAssets.Where(asset => asset != null))
                ValidatePlayerBuffEffects(buffAsset.Data, AssetDatabase.GetAssetPath(buffAsset), errors);

            foreach (var buffAsset in catalog.CharacterBuffAssets.Where(asset => asset != null))
                ValidateCharacterBuffEffects(buffAsset.Data, AssetDatabase.GetAssetPath(buffAsset), errors);

            foreach (var buffAsset in catalog.CardBuffAssets.Where(asset => asset != null))
                ValidateCardBuffEffects(buffAsset.Data, AssetDatabase.GetAssetPath(buffAsset), errors);

            return errors;
        }

        public static IReadOnlyList<string> ValidateReferenceIds()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateReferenceIds(catalog);
        }

        private static IReadOnlyList<string> _ValidateReferenceIds(
            GameContentCatalog catalog)
        {
            var cardIds = catalog.CardAssets
                .Where(asset => asset != null)
                .Where(asset => asset.CardData != null)
                .Select(asset => asset.CardData.ID)
                .ToHashSet();
            var cardBuffIds = catalog.CardBuffAssets
                .Where(asset => asset != null)
                .Where(asset => asset.Data != null)
                .Select(asset => asset.Data.ID)
                .ToHashSet();
            var playerBuffIds = catalog.PlayerBuffAssets
                .Where(asset => asset != null)
                .Where(asset => asset.Data != null)
                .Select(asset => asset.Data.ID)
                .ToHashSet();
            var errors = new List<string>();

            foreach (var cardAsset in catalog.CardAssets.Where(asset => asset != null))
            {
                ValidateCardReferenceIds(
                    cardAsset.CardData,
                    AssetDatabase.GetAssetPath(cardAsset),
                    cardIds,
                    cardBuffIds,
                    playerBuffIds,
                    errors);
            }

            foreach (var playerBuffAsset in catalog.PlayerBuffAssets.Where(asset => asset != null))
            {
                ValidatePlayerBuffReferenceIds(
                    playerBuffAsset.Data,
                    AssetDatabase.GetAssetPath(playerBuffAsset),
                    cardBuffIds,
                    errors);
            }

            foreach (var cardBuffAsset in catalog.CardBuffAssets.Where(asset => asset != null))
            {
                ValidateCardBuffReferenceIds(
                    cardBuffAsset.Data,
                    AssetDatabase.GetAssetPath(cardBuffAsset),
                    cardBuffIds,
                    errors);
            }

            foreach (var deckAsset in EditorAssetUtility.FindAssets<DeckScriptable>(
                ProjectAssetPaths.GameContent.SearchFolders))
                ValidateDeckReferenceIds(deckAsset, AssetDatabase.GetAssetPath(deckAsset), cardIds, errors);

            foreach (var allyAsset in EditorAssetUtility.FindAssets<AllyScriptable>(
                ProjectAssetPaths.GameContent.SearchFolders))
                ValidatePlayerDeck(allyAsset.Ally?.PlayerData, AssetDatabase.GetAssetPath(allyAsset), errors);

            foreach (var enemyAsset in EditorAssetUtility.FindAssets<EnemyScriptable>(
                ProjectAssetPaths.GameContent.SearchFolders))
                ValidatePlayerDeck(enemyAsset.Enemy?.PlayerData, AssetDatabase.GetAssetPath(enemyAsset), errors);

            return errors;
        }

        private static void ValidateCardEffects(CardData cardData, string assetPath, ICollection<string> errors)
        {
            if (cardData == null)
            {
                errors.Add($"{assetPath} 的 CardData 為空");
                return;
            }

            foreach (var effect in cardData.Effects ?? Enumerable.Empty<ICardEffect>())
                ValidateCardEffectResolver(effect, $"{assetPath} / CardData[{cardData.ID}].Effects", errors);

            foreach (var triggeredEffect in cardData.TriggeredEffects ?? Enumerable.Empty<TriggeredCardEffect>())
            {
                foreach (var effect in triggeredEffect?.Effects ?? Array.Empty<ICardEffect>())
                {
                    ValidateCardEffectResolver(
                        effect,
                        $"{assetPath} / CardData[{cardData.ID}].TriggeredEffects[{triggeredEffect.Timing}]",
                        errors);
                }
            }
        }

        private static void ValidatePlayerBuffEffects(PlayerBuffData buffData, string assetPath, ICollection<string> errors)
        {
            if (buffData == null)
            {
                errors.Add($"{assetPath} 的 PlayerBuffData 為空");
                return;
            }

            foreach (var pair in buffData.BuffEffects ?? new Dictionary<GameTiming, ConditionalPlayerBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalPlayerBuffEffect>())
                {
                    var effect = conditionalEffect?.Effect;
                    if (effect != null && !EffectDataResolver.HasPlayerBuffEffectResolver(effect.GetType()))
                    {
                        errors.Add(
                            $"{assetPath} / PlayerBuffData[{buffData.ID}].BuffEffects[{pair.Key}] 的 {effect.GetType().Name} 缺少 IPlayerBuffEffectResolver 註冊");
                    }
                }
            }
        }

        private static void ValidateCharacterBuffEffects(CharacterBuffData buffData, string assetPath, ICollection<string> errors)
        {
            if (buffData == null)
            {
                errors.Add($"{assetPath} 的 CharacterBuffData 為空");
                return;
            }

            foreach (var pair in buffData.BuffEffects ?? new Dictionary<GameTiming, ConditionalCharacterBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCharacterBuffEffect>())
                {
                    var effect = conditionalEffect?.Effect;
                    if (effect != null && !EffectDataResolver.HasCharacterBuffEffectResolver(effect.GetType()))
                    {
                        errors.Add(
                            $"{assetPath} / CharacterBuffData[{buffData.ID}].BuffEffects[{pair.Key}] 的 {effect.GetType().Name} 缺少 ICharacterBuffEffectResolver 註冊");
                    }
                }
            }
        }

        private static void ValidateCardBuffEffects(CardBuffData buffData, string assetPath, ICollection<string> errors)
        {
            if (buffData == null)
            {
                errors.Add($"{assetPath} 的 CardBuffData 為空");
                return;
            }

            foreach (var pair in buffData.Effects ?? new Dictionary<CardTriggeredTiming, ConditionalCardBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCardBuffEffect>())
                {
                    ValidateCardBuffEffectResolver(
                        conditionalEffect?.Effect,
                        $"{assetPath} / CardBuffData[{buffData.ID}].Effects[{pair.Key}]",
                        errors);
                }
            }

            foreach (var pair in buffData.BuffEffects ?? new Dictionary<GameTiming, ConditionalCardBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCardBuffEffect>())
                {
                    ValidateCardBuffEffectResolver(
                        conditionalEffect?.Effect,
                        $"{assetPath} / CardBuffData[{buffData.ID}].BuffEffects[{pair.Key}]",
                        errors);
                }
            }
        }

        private static void ValidateCardReferenceIds(
            CardData cardData,
            string assetPath,
            ISet<string> cardIds,
            ISet<string> cardBuffIds,
            ISet<string> playerBuffIds,
            ICollection<string> errors)
        {
            if (cardData == null)
                return;

            foreach (var effect in EnumerateCardEffects(cardData))
            {
                switch (effect)
                {
                    case AddPlayerBuffEffect addPlayerBuff:
                        ValidateId(playerBuffIds, addPlayerBuff.BuffId, $"{assetPath} / CardData[{cardData.ID}] 的 AddPlayerBuffEffect.BuffId", errors);
                        break;
                    case ModifyPlayerBuffLevelEffect modifyPlayerBuffLevel:
                        ValidateId(playerBuffIds, modifyPlayerBuffLevel.BuffId, $"{assetPath} / CardData[{cardData.ID}] 的 ModifyPlayerBuffLevelEffect.BuffId", errors);
                        break;
                    case RemovePlayerBuffEffect removePlayerBuff:
                        ValidateId(playerBuffIds, removePlayerBuff.BuffId, $"{assetPath} / CardData[{cardData.ID}] 的 RemovePlayerBuffEffect.BuffId", errors);
                        break;
                    case CreateCardEffect createCard:
                        foreach (var cardId in createCard.CardDataIds ?? Enumerable.Empty<string>())
                            ValidateId(cardIds, cardId, $"{assetPath} / CardData[{cardData.ID}] 的 CreateCardEffect.CardDataIds", errors);
                        ValidateAddCardBuffDataIds(createCard.AddCardBuffDatas, $"{assetPath} / CardData[{cardData.ID}] 的 CreateCardEffect.AddCardBuffDatas", cardBuffIds, errors);
                        break;
                    case CloneCardEffect cloneCard:
                        ValidateAddCardBuffDataIds(cloneCard.AddCardBuffDatas, $"{assetPath} / CardData[{cardData.ID}] 的 CloneCardEffect.AddCardBuffDatas", cardBuffIds, errors);
                        break;
                    case AddCardBuffEffect addCardBuff:
                        ValidateAddCardBuffDataIds(addCardBuff.AddCardBuffDatas, $"{assetPath} / CardData[{cardData.ID}] 的 AddCardBuffEffect.AddCardBuffDatas", cardBuffIds, errors);
                        break;
                    case RemoveCardBuffEffect removeCardBuff:
                        ValidateId(cardBuffIds, removeCardBuff.BuffId, $"{assetPath} / CardData[{cardData.ID}] 的 RemoveCardBuffEffect.BuffId", errors);
                        break;
                }
            }
        }

        private static void ValidatePlayerBuffReferenceIds(
            PlayerBuffData buffData,
            string assetPath,
            ISet<string> cardBuffIds,
            ICollection<string> errors)
        {
            if (buffData == null)
                return;

            foreach (var pair in buffData.BuffEffects ?? new Dictionary<GameTiming, ConditionalPlayerBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalPlayerBuffEffect>())
                {
                    switch (conditionalEffect?.Effect)
                    {
                        case AddCardBuffPlayerBuffEffect addCardBuff:
                            ValidateAddCardBuffDataIds(addCardBuff.AddCardBuffDatas, $"{assetPath} / PlayerBuffData[{buffData.ID}].BuffEffects[{pair.Key}] 的 AddCardBuffPlayerBuffEffect.AddCardBuffDatas", cardBuffIds, errors);
                            break;
                        case RemoveCardBuffPlayerBuffEffect removeCardBuff:
                            ValidateId(cardBuffIds, removeCardBuff.BuffId, $"{assetPath} / PlayerBuffData[{buffData.ID}].BuffEffects[{pair.Key}] 的 RemoveCardBuffPlayerBuffEffect.BuffId", errors);
                            break;
                    }
                }
            }
        }

        private static void ValidateCardBuffReferenceIds(
            CardBuffData buffData,
            string assetPath,
            ISet<string> cardBuffIds,
            ICollection<string> errors)
        {
            if (buffData == null)
                return;

            foreach (var effect in EnumerateCardBuffEffects(buffData))
            {
                switch (effect)
                {
                    case AddCardBuffPlayerBuffEffect addCardBuff:
                        ValidateAddCardBuffDataIds(addCardBuff.AddCardBuffDatas, $"{assetPath} / CardBuffData[{buffData.ID}] 的 AddCardBuffPlayerBuffEffect.AddCardBuffDatas", cardBuffIds, errors);
                        break;
                    case RemoveCardBuffPlayerBuffEffect removeCardBuff:
                        ValidateId(cardBuffIds, removeCardBuff.BuffId, $"{assetPath} / CardBuffData[{buffData.ID}] 的 RemoveCardBuffPlayerBuffEffect.BuffId", errors);
                        break;
                }
            }
        }

        private static void ValidateDeckReferenceIds(
            DeckScriptable deck,
            string assetPath,
            ISet<string> cardIds,
            ICollection<string> errors)
        {
            if (deck == null)
                return;

            foreach (var cardAsset in deck.Cards ?? Array.Empty<StandardCardDataScriptable>())
            {
                if (cardAsset == null)
                {
                    errors.Add($"{assetPath} 的 DeckScriptable.Cards 含有空卡牌引用");
                    continue;
                }

                ValidateId(cardIds, cardAsset.Data?.ID, $"{assetPath} 的 DeckScriptable.Cards", errors);
            }
        }

        private static void ValidatePlayerDeck(PlayerData playerData, string assetPath, ICollection<string> errors)
        {
            if (playerData == null)
            {
                errors.Add($"{assetPath} 的 PlayerData 為空");
                return;
            }

            if (playerData.Deck == null)
                errors.Add($"{assetPath} / PlayerData[{playerData.ID}] 缺少 Deck 引用");
        }

        private static void ValidateCardEffectResolver(ICardEffect effect, string context, ICollection<string> errors)
        {
            if (effect != null && !EffectDataResolver.HasCardEffectResolver(effect.GetType()))
                errors.Add($"{context} 的 {effect.GetType().Name} 缺少 ICardEffectResolver 註冊");
        }

        private static void ValidateCardBuffEffectResolver(ICardBuffEffect effect, string context, ICollection<string> errors)
        {
            if (effect != null && !EffectDataResolver.HasCardBuffEffectResolver(effect.GetType()))
                errors.Add($"{context} 的 {effect.GetType().Name} 缺少 ICardBuffEffectResolver 註冊");
        }

        private static IEnumerable<ICardEffect> EnumerateCardEffects(CardData cardData)
        {
            foreach (var effect in cardData.Effects ?? Enumerable.Empty<ICardEffect>())
                yield return effect;

            foreach (var triggeredEffect in cardData.TriggeredEffects ?? Enumerable.Empty<TriggeredCardEffect>())
            {
                foreach (var effect in triggeredEffect?.Effects ?? Array.Empty<ICardEffect>())
                    yield return effect;
            }
        }

        private static IEnumerable<ICardBuffEffect> EnumerateCardBuffEffects(CardBuffData buffData)
        {
            foreach (var pair in buffData.Effects ?? new Dictionary<CardTriggeredTiming, ConditionalCardBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCardBuffEffect>())
                {
                    if (conditionalEffect?.Effect != null)
                        yield return conditionalEffect.Effect;
                }
            }

            foreach (var pair in buffData.BuffEffects ?? new Dictionary<GameTiming, ConditionalCardBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCardBuffEffect>())
                {
                    if (conditionalEffect?.Effect != null)
                        yield return conditionalEffect.Effect;
                }
            }
        }

        private static void ValidateAddCardBuffDataIds(
            IEnumerable<AddCardBuffData> addCardBuffDatas,
            string context,
            ISet<string> cardBuffIds,
            ICollection<string> errors)
        {
            foreach (var addCardBuffData in addCardBuffDatas ?? Enumerable.Empty<AddCardBuffData>())
            {
                if (addCardBuffData == null)
                {
                    errors.Add($"{context} 含有空 AddCardBuffData");
                    continue;
                }

                ValidateId(cardBuffIds, addCardBuffData.CardBuffId, $"{context}.CardBuffId", errors);
            }
        }

        private static void ValidateId(ISet<string> validIds, string id, string context, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"{context} 為空");
                return;
            }

            if (!validIds.Contains(id))
                errors.Add($"{context} 指向不存在的 ID：{id}");
        }

        private static IReadOnlyList<string> _ValidateContentIntegrity(
            GameContentCatalog catalog)
        {
            var localizationData = AssetDatabase.LoadAssetAtPath<ExcelDatas>(
                ProjectAssetPaths.GameContent.LocalizationData);
            var playerDatas = EditorAssetUtility.FindAssets<AllyScriptable>(
                    ProjectAssetPaths.GameContent.SearchFolders)
                .Select(asset => asset.Ally?.PlayerData)
                .Concat(EditorAssetUtility.FindAssets<EnemyScriptable>(
                        ProjectAssetPaths.GameContent.SearchFolders)
                    .Select(asset => asset.Enemy?.PlayerData))
                .Where(data => data != null)
                .ToArray();

            return ValidateNestedContent(catalog)
                .Concat(_ValidateReferenceIds(catalog))
                .Concat(ValidateLocalization(catalog, localizationData, playerDatas))
                .Distinct()
                .ToArray();
        }

        private static void _ValidateCardNestedSemantics(
            CardData cardData,
            string context,
            ICollection<string> errors)
        {
            if (cardData == null)
                return;

            var subSelections = cardData.SubSelects ?? new List<ISubSelectionGroup>();
            foreach (var pair in subSelections
                .Select((selection, index) => (Selection: selection, Index: index))
                .Where(pair => pair.Selection != null))
            {
                if (string.IsNullOrWhiteSpace(pair.Selection.Id))
                    errors.Add($"{context}.SubSelects[{pair.Index}].Id 為空");
            }

            foreach (var duplicateId in subSelections
                .Where(selection => selection != null)
                .Select(selection => selection.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .GroupBy(id => id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add($"{context}.SubSelects 的 Id 重複：{duplicateId}");
            }

            _ValidateAddCardBuffDataSemantics(cardData, context, errors);

            var triggeredEffects = cardData.TriggeredEffects ?? new List<TriggeredCardEffect>();
            for (var index = 0; index < triggeredEffects.Count; index++)
            {
                var triggeredEffect = triggeredEffects[index];
                if (triggeredEffect != null && triggeredEffect.Timing == CardTriggeredTiming.None)
                {
                    errors.Add($"{context}.TriggeredEffects[{index}].Timing 不可為 None");
                }
            }
        }

        private static void _ValidateCardCollectionSemantics(
            object data,
            string context,
            ICollection<string> errors)
        {
            foreach (var cards in SerializedDataGraphUtility.Find<CardsOfPlayer>(data))
            {
                if (cards.Zone == CardCollectionType.None ||
                    !Enum.IsDefined(typeof(CardCollectionType), cards.Zone))
                {
                    errors.Add(
                        $"{context} 的 CardsOfPlayer.Zone 必須是有效的一般卡片區域：{cards.Zone}");
                }
            }
        }

        private static void _ValidateTargetSemantics(
            object data,
            string context,
            ICollection<string> errors)
        {
            foreach (var player in SerializedDataGraphUtility.Find<PlayerByFaction>(data))
            {
                if (player.Faction == Faction.None ||
                    !Enum.IsDefined(typeof(Faction), player.Faction))
                {
                    errors.Add(
                        $"{context} 的 PlayerByFaction.Faction 必須是 Ally 或 Enemy：{player.Faction}");
                }
            }
        }

        private static void _ValidateCardBuffNestedSemantics(
            CardBuffData buffData,
            string context,
            ICollection<string> errors)
        {
            if (buffData == null)
                return;

            _ValidateSessionSemantics(buffData.Sessions, $"{context}.Sessions", errors);
            _ValidateTimingKeys(buffData.Effects, $"{context}.Effects", errors);
            _ValidateTimingKeys(buffData.BuffEffects, $"{context}.BuffEffects", errors);
            _ValidateAddCardBuffDataSemantics(buffData, context, errors);

            if (buffData.LifeTimeData != null &&
                buffData.LifeTimeData is not AlwaysLifeTimeCardBuffData &&
                buffData.LifeTimeData is not TurnLifeTimeCardBuffData &&
                buffData.LifeTimeData is not HandCardLifeTimeCardBuffData)
            {
                errors.Add(
                    $"{context}.LifeTimeData 使用未註冊型別：{buffData.LifeTimeData.GetType().Name}");
            }
        }

        private static void _ValidatePlayerBuffNestedSemantics(
            PlayerBuffData buffData,
            string context,
            ICollection<string> errors)
        {
            if (buffData == null)
                return;

            if (buffData.MaxLevel <= 0)
                errors.Add($"{context}.MaxLevel 必須大於 0");

            _ValidateSessionSemantics(buffData.Sessions, $"{context}.Sessions", errors);
            _ValidateTimingKeys(buffData.BuffEffects, $"{context}.BuffEffects", errors);
            _ValidateAddCardBuffDataSemantics(buffData, context, errors);

            if (buffData.LifeTimeData != null &&
                buffData.LifeTimeData is not AlwaysLifeTimePlayerBuffData &&
                buffData.LifeTimeData is not PlayerBuffTurnLifeTimeData)
            {
                errors.Add(
                    $"{context}.LifeTimeData 使用未註冊型別：{buffData.LifeTimeData.GetType().Name}");
            }

            var sessions = buffData.Sessions ??
                new Dictionary<string, IReactionSessionData>();
            foreach (var condition in SerializedDataGraphUtility.Find<PlayerBuffSessionCondition>(buffData))
            {
                if (string.IsNullOrWhiteSpace(condition.SessionKey))
                {
                    errors.Add($"{context} 的 PlayerBuffSessionCondition.SessionKey 為空");
                }
                else if (!sessions.ContainsKey(condition.SessionKey))
                {
                    errors.Add(
                        $"{context} 的 PlayerBuffSessionCondition 引用不存在的 Session：{condition.SessionKey}");
                }
            }

            foreach (var value in SerializedDataGraphUtility.Find<PlayerBuffSessionInteger>(buffData))
            {
                if (string.IsNullOrWhiteSpace(value.SessionIntegerId))
                {
                    errors.Add($"{context} 的 PlayerBuffSessionInteger.SessionIntegerId 為空");
                }
                else if (!sessions.TryGetValue(value.SessionIntegerId, out var session))
                {
                    errors.Add(
                        $"{context} 的 PlayerBuffSessionInteger 引用不存在的 Session：{value.SessionIntegerId}");
                }
                else if (session is not SessionInteger)
                {
                    errors.Add(
                        $"{context} 的 PlayerBuffSessionInteger 必須引用 SessionInteger：{value.SessionIntegerId}");
                }
            }
        }

        private static void _ValidateCharacterBuffNestedSemantics(
            CharacterBuffData buffData,
            string context,
            ICollection<string> errors)
        {
            if (buffData == null)
                return;

            if (buffData.MaxLevel <= 0)
                errors.Add($"{context}.MaxLevel 必須大於 0");

            _ValidateSessionSemantics(buffData.Sessions, $"{context}.Sessions", errors);
            _ValidateTimingKeys(buffData.BuffEffects, $"{context}.BuffEffects", errors);

            if (buffData.LifeTimeData != null &&
                buffData.LifeTimeData is not AlwaysLifeTimeCharacterBuffData &&
                buffData.LifeTimeData is not TurnLifeTimeCharacterBuffData)
            {
                errors.Add(
                    $"{context}.LifeTimeData 使用未註冊型別：{buffData.LifeTimeData.GetType().Name}");
            }

            if (buffData.LifeTimeData is TurnLifeTimeCharacterBuffData turnLifeTime &&
                turnLifeTime.Turn <= 0)
            {
                errors.Add($"{context}.LifeTimeData.Turn 必須大於 0");
            }
        }

        private static void _ValidateAddCardBuffDataSemantics(
            object data,
            string context,
            ICollection<string> errors)
        {
            foreach (var addCardBuffData in SerializedDataGraphUtility.Find<AddCardBuffData>(data))
            {
                if (addCardBuffData.Level is ConstInteger { Value: < 0 })
                    errors.Add($"{context} 的 AddCardBuffData.Level 不可為負數");
            }
        }

        private static void _ValidateIntegerValueSemantics(
            object data,
            string context,
            ICollection<string> errors)
        {
            foreach (var arithmetic in SerializedDataGraphUtility.Find<ArithmeticInteger>(data))
            {
                if (arithmetic.Operation == ArithmeticType.None)
                {
                    errors.Add($"{context} 的 ArithmeticInteger.Operation 不可為 None");
                }
                else if (arithmetic.Operation == ArithmeticType.Overwrite)
                {
                    errors.Add(
                        $"{context} 的 ArithmeticInteger.Operation 不支援 Overwrite");
                }

                if (arithmetic.Operation is ArithmeticType.Divide or ArithmeticType.Remainder &&
                    arithmetic.Right is ConstInteger { Value: 0 })
                {
                    errors.Add(
                        $"{context} 的 ArithmeticInteger.{arithmetic.Operation} 除數不可為 0");
                }

                if (_HasStaticArithmeticOverflow(arithmetic))
                {
                    errors.Add(
                        $"{context} 的 ArithmeticInteger.{arithmetic.Operation} 常數運算結果超出 Int32 範圍");
                }
            }

            foreach (var minimum in SerializedDataGraphUtility.Find<MinimumInteger>(data))
            {
                if (minimum.Values == null || minimum.Values.Count == 0)
                    errors.Add($"{context} 的 MinimumInteger.Values 至少需要一項");
            }

            foreach (var maximum in SerializedDataGraphUtility.Find<MaximumInteger>(data))
            {
                if (maximum.Values == null || maximum.Values.Count == 0)
                    errors.Add($"{context} 的 MaximumInteger.Values 至少需要一項");
            }
        }

        private static bool _HasStaticArithmeticOverflow(ArithmeticInteger arithmetic)
        {
            if (!_TryEvaluateStaticInteger(arithmetic.Left, out var left) ||
                !_TryEvaluateStaticInteger(arithmetic.Right, out var right))
            {
                return false;
            }

            return arithmetic.Operation switch
            {
                ArithmeticType.Add => (long)left + right is > int.MaxValue or < int.MinValue,
                ArithmeticType.Subtract => (long)left - right is > int.MaxValue or < int.MinValue,
                ArithmeticType.Multiply => (long)left * right is > int.MaxValue or < int.MinValue,
                ArithmeticType.Divide => left == int.MinValue && right == -1,
                _ => false
            };
        }

        private static bool _TryEvaluateStaticInteger(IIntegerValue value, out int result)
        {
            switch (value)
            {
                case ConstInteger constant:
                    result = constant.Value;
                    return true;
                case ArithmeticInteger arithmetic
                    when _TryEvaluateStaticInteger(arithmetic.Left, out var left) &&
                         _TryEvaluateStaticInteger(arithmetic.Right, out var right):
                    return _TryEvaluateStaticArithmetic(
                        arithmetic.Operation,
                        left,
                        right,
                        out result);
                case MinimumInteger minimum
                    when minimum.Values != null && minimum.Values.Count > 0:
                    return _TryEvaluateStaticExtremum(minimum.Values, true, out result);
                case MaximumInteger maximum
                    when maximum.Values != null && maximum.Values.Count > 0:
                    return _TryEvaluateStaticExtremum(maximum.Values, false, out result);
                default:
                    result = default;
                    return false;
            }
        }

        private static bool _TryEvaluateStaticArithmetic(
            ArithmeticType operation,
            int left,
            int right,
            out int result)
        {
            var evaluated = operation switch
            {
                ArithmeticType.Add => GameplayIntegerMath.Add(left, right),
                ArithmeticType.Subtract => GameplayIntegerMath.Subtract(left, right),
                ArithmeticType.Multiply => GameplayIntegerMath.Multiply(left, right),
                ArithmeticType.Divide => GameplayIntegerMath.Divide(left, right),
                ArithmeticType.Remainder => GameplayIntegerMath.Remainder(left, right),
                _ => Optional.Option.None<int>()
            };
            return evaluated.TryGetValue(out result);
        }

        private static bool _TryEvaluateStaticExtremum(
            IReadOnlyList<IIntegerValue> values,
            bool minimum,
            out int result)
        {
            if (!_TryEvaluateStaticInteger(values[0], out result))
                return false;

            for (var index = 1; index < values.Count; index++)
            {
                if (!_TryEvaluateStaticInteger(values[index], out var current))
                    return false;

                result = minimum
                    ? Math.Min(result, current)
                    : Math.Max(result, current);
            }

            return true;
        }

        private static void _ValidateSessionSemantics(
            IReadOnlyDictionary<string, IReactionSessionData> sessions,
            string context,
            ICollection<string> errors)
        {
            if (sessions == null)
                return;

            foreach (var pair in sessions)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    errors.Add($"{context} 含有空 Session Key");

                switch (pair.Value)
                {
                    case SessionBoolean booleanSession:
                        _ValidateSessionTimings(
                            booleanSession.UpdateRules,
                            $"{context}[{pair.Key}].UpdateRules",
                            rule => rule.Timing,
                            errors);
                        break;
                    case SessionInteger integerSession:
                        _ValidateSessionTimings(
                            integerSession.UpdateRules,
                            $"{context}[{pair.Key}].UpdateRules",
                            rule => rule.Timing,
                            errors);
                        break;
                    case null:
                        break;
                    default:
                        errors.Add(
                            $"{context}[{pair.Key}] 使用未註冊型別：{pair.Value.GetType().Name}");
                        break;
                }
            }
        }

        private static void _ValidateSessionTimings<T>(
            IReadOnlyList<T> rules,
            string context,
            Func<T, GameTiming> getTiming,
            ICollection<string> errors)
            where T : class
        {
            if (rules == null)
                return;

            for (var index = 0; index < rules.Count; index++)
            {
                var rule = rules[index];
                if (rule != null && getTiming(rule) == GameTiming.None)
                    errors.Add($"{context}[{index}].Timing 不可為 None");
            }
        }

        private static void _ValidateTimingKeys<T>(
            IReadOnlyDictionary<T, ConditionalCardBuffEffect[]> effects,
            string context,
            ICollection<string> errors)
            where T : struct, Enum
        {
            if (effects == null)
                return;

            foreach (var timing in effects.Keys)
            {
                if (Convert.ToInt32(timing) == 0)
                    errors.Add($"{context} 的 Timing 不可為 None");
            }
        }

        private static void _ValidateTimingKeys<T>(
            IReadOnlyDictionary<T, ConditionalPlayerBuffEffect[]> effects,
            string context,
            ICollection<string> errors)
            where T : struct, Enum
        {
            if (effects == null)
                return;

            foreach (var timing in effects.Keys)
            {
                if (Convert.ToInt32(timing) == 0)
                    errors.Add($"{context} 的 Timing 不可為 None");
            }
        }

        private static void _ValidateTimingKeys<T>(
            IReadOnlyDictionary<T, ConditionalCharacterBuffEffect[]> effects,
            string context,
            ICollection<string> errors)
            where T : struct, Enum
        {
            if (effects == null)
                return;

            foreach (var timing in effects.Keys)
            {
                if (Convert.ToInt32(timing) == 0)
                    errors.Add($"{context} 的 Timing 不可為 None");
            }
        }

        private static void _ValidateContentIds(
            GameContentCatalog catalog,
            ICollection<string> errors)
        {
            _ValidateIdCollection(
                catalog.CardAssets
                    .Where(asset => asset != null && asset.CardData != null)
                    .Select(asset => (
                        asset.CardData.ID,
                        AssetDatabase.GetAssetPath(asset))),
                "CardData",
                errors);
            _ValidateIdCollection(
                catalog.CardBuffAssets
                    .Where(asset => asset != null && asset.Data != null)
                    .Select(asset => (
                        asset.Data.ID,
                        AssetDatabase.GetAssetPath(asset))),
                "CardBuffData",
                errors);
            _ValidateIdCollection(
                catalog.PlayerBuffAssets
                    .Where(asset => asset != null && asset.Data != null)
                    .Select(asset => (
                        asset.Data.ID,
                        AssetDatabase.GetAssetPath(asset))),
                "PlayerBuffData",
                errors);
            _ValidateIdCollection(
                catalog.CharacterBuffAssets
                    .Where(asset => asset != null && asset.Data != null)
                    .Select(asset => (
                        asset.Data.ID,
                        AssetDatabase.GetAssetPath(asset))),
                "CharacterBuffData",
                errors);
        }

        private static void _ValidateIdCollection(
            IEnumerable<(string Id, string Context)> entries,
            string dataType,
            ICollection<string> errors)
        {
            var values = entries.ToArray();
            foreach (var entry in values.Where(entry => string.IsNullOrWhiteSpace(entry.Id)))
                errors.Add($"{entry.Context} 的 {dataType}.ID 為空");

            foreach (var duplicateId in values
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Id))
                .GroupBy(entry => entry.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add($"{dataType}.ID 重複：{duplicateId}");
            }
        }

        private static HashSet<string> _ValidateTitleInfoTable(
            IReadOnlyList<LocalizeExcelTitleData> rows,
            string tableName,
            bool requireInfo,
            ICollection<string> errors)
        {
            if (rows == null)
            {
                errors.Add($"ExcelDatas.{tableName} 為空");
                return new HashSet<string>();
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row == null)
                {
                    errors.Add($"ExcelDatas.{tableName}[{index}] 為空");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Id))
                    errors.Add($"ExcelDatas.{tableName}[{index}].Id 為空");
                if (string.IsNullOrWhiteSpace(row.Title))
                    errors.Add($"ExcelDatas.{tableName}[{index}].Title 為空");
                if (requireInfo && string.IsNullOrWhiteSpace(row.Info))
                    errors.Add($"ExcelDatas.{tableName}[{index}].Info 為空");
            }

            foreach (var duplicateId in rows
                .Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .GroupBy(row => row.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add($"ExcelDatas.{tableName} 的 Id 重複：{duplicateId}");
            }

            return rows
                .Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .Select(row => row.Id)
                .ToHashSet();
        }

        private static void _ValidateInfoTable(
            IReadOnlyList<LocalizeExcelData> rows,
            string tableName,
            ICollection<string> errors)
        {
            if (rows == null)
            {
                errors.Add($"ExcelDatas.{tableName} 為空");
                return;
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row == null)
                {
                    errors.Add($"ExcelDatas.{tableName}[{index}] 為空");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Id))
                    errors.Add($"ExcelDatas.{tableName}[{index}].Id 為空");
                if (string.IsNullOrWhiteSpace(row.Info))
                    errors.Add($"ExcelDatas.{tableName}[{index}].Info 為空");
            }

            foreach (var duplicateId in rows
                .Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .GroupBy(row => row.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add($"ExcelDatas.{tableName} 的 Id 重複：{duplicateId}");
            }
        }

        private static void _ValidateLocalizationKeys(
            IEnumerable<(string Key, string Context)> references,
            ISet<string> validKeys,
            string tableName,
            ICollection<string> errors)
        {
            foreach (var reference in references)
            {
                if (string.IsNullOrWhiteSpace(reference.Key))
                {
                    errors.Add($"{reference.Context} 為空");
                }
                else if (!validKeys.Contains(reference.Key))
                {
                    errors.Add(
                        $"{reference.Context} 在 ExcelDatas.{tableName} 找不到 Localization Key");
                }
            }
        }

        private static void _ValidateCatalogCollection<T>(
            IReadOnlyList<T> catalogAssets,
            IReadOnlyList<T> scannedAssets,
            string collectionName,
            ICollection<string> errors)
            where T : UnityEngine.Object
        {
            var catalogItems = catalogAssets ?? Array.Empty<T>();
            var scannedItems = scannedAssets ?? Array.Empty<T>();
            var catalogPaths = new List<string>();

            for (var index = 0; index < catalogItems.Count; index++)
            {
                var asset = catalogItems[index];
                if (asset == null)
                {
                    errors.Add($"GameContentCatalog.{collectionName}[{index}] 是空資產引用");
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    errors.Add(
                        $"GameContentCatalog.{collectionName}[{index}] 不是已儲存的專案資產：{asset.name}");
                    continue;
                }

                catalogPaths.Add(assetPath);
            }

            foreach (var duplicatePath in catalogPaths
                .GroupBy(path => path, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add(
                    $"GameContentCatalog.{collectionName} 重複收錄資產：{duplicatePath}");
            }

            var scannedPaths = scannedItems
                .Where(asset => asset != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            var catalogPathSet = catalogPaths.ToHashSet(StringComparer.Ordinal);
            var scannedPathSet = scannedPaths.ToHashSet(StringComparer.Ordinal);

            foreach (var missingPath in scannedPaths.Where(path => !catalogPathSet.Contains(path)))
            {
                errors.Add(
                    $"GameContentCatalog.{collectionName} 未收錄掃描到的資產：{missingPath}");
            }

            foreach (var extraPath in catalogPaths
                .Distinct(StringComparer.Ordinal)
                .Where(path => !scannedPathSet.Contains(path)))
            {
                errors.Add(
                    $"GameContentCatalog.{collectionName} 收錄了掃描範圍外的資產：{extraPath}");
            }

            var hasValidSet = catalogPaths.Count == catalogPathSet.Count &&
                catalogPathSet.SetEquals(scannedPathSet);
            if (hasValidSet && !catalogPaths.SequenceEqual(scannedPaths, StringComparer.Ordinal))
            {
                errors.Add(
                    $"GameContentCatalog.{collectionName} 的順序與資產路徑排序不一致，請重新產生內容目錄");
            }
        }

        private static GameContentCatalog _LoadDefaultCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                ProjectAssetPaths.GameContent.Catalog);
        }

        private static string _GetMissingCatalogError()
        {
            return
                $"找不到遊戲內容目錄：{ProjectAssetPaths.GameContent.Catalog}，" +
                "請執行 MortalGame/遊戲內容/重新產生內容目錄";
        }

        private static string GetAssetContext(UnityEngine.Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(assetPath) ? asset.name : assetPath;
        }

        private static bool IsTimingDispatchSupported(GameTiming timing)
        {
            return timing is GameTiming.GameStart or
                GameTiming.BeforeTurnStart or GameTiming.AfterTurnStart or
                GameTiming.BeforeDrawCard or GameTiming.AfterDrawCard or
                GameTiming.BeforeExecuteStart or GameTiming.AfterExecuteStart or
                GameTiming.BeforeExecuteEnd or GameTiming.AfterExecuteEnd or
                GameTiming.BeforeTurnEnd or GameTiming.AfterTurnEnd or
                GameTiming.BeforePlayCardStart or GameTiming.AfterPlayCardStart or
                GameTiming.BeforePlayCardEnd or GameTiming.AfterPlayCardEnd or
                GameTiming.BeforeTriggerBuffEffect or GameTiming.AfterTriggerBuffEffect;
        }

        private static IEnumerable<GameTiming> GetDuplicateTimings(
            IReactionSessionData session)
        {
            return session switch
            {
                SessionBoolean booleanSession => FindDuplicates(
                    booleanSession.UpdateRules?
                        .Where(rule => rule != null)
                        .Select(rule => rule.Timing) ??
                    Enumerable.Empty<GameTiming>()),
                SessionInteger integerSession => FindDuplicates(
                    integerSession.UpdateRules?
                        .Where(rule => rule != null)
                        .Select(rule => rule.Timing) ??
                    Enumerable.Empty<GameTiming>()),
                _ => Enumerable.Empty<GameTiming>()
            };

            static IEnumerable<GameTiming> FindDuplicates(
                IEnumerable<GameTiming> timings)
            {
                return timings
                    .GroupBy(timing => timing)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key);
            }
        }

        private static IEnumerable<Type> GetConcreteTypes<T>()
        {
            var targetType = typeof(T);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(GetAssemblyTypes)
                .Where(type => type != null)
                .Where(type => targetType.IsAssignableFrom(type))
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .Where(type => !type.ContainsGenericParameters)
                .Distinct();
        }

        private static IEnumerable<Type> GetAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }

}
