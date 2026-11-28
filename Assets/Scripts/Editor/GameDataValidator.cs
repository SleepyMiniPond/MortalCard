using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using UnityEditor;

namespace MortalGame.Editor
{
    public static partial class GameDataValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidateAll()
        {
            var catalog = _LoadDefaultCatalog();
            if (catalog == null)
            {
                return new[] { _GetMissingCatalogError() }
                    .Concat(ValidateEffectCommandHandlers())
                    .Distinct(ValidationIssueMessageComparer.Instance)
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
                .Distinct(ValidationIssueMessageComparer.Instance)
                .ToArray();
        }

        public static IReadOnlyList<ValidationIssue> ValidateContentIntegrity()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateContentIntegrity(catalog);
        }

        public static IReadOnlyList<ValidationIssue> ValidateNestedContent(
            GameContentCatalog catalog)
        {
            if (catalog == null)
                return new[] { new ValidationIssue.MissingCatalogForNestedContent() };

            var errors = new List<ValidationIssue>();

            foreach (var asset in catalog.CardAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / CardData";
                _ValidateCommonNestedSemantics(asset.CardData, context, errors);
                _ValidateCardNestedSemantics(asset.CardData, context, errors);
                _ValidateCardComposition(asset.CardData, context, catalog, errors);
            }

            foreach (var asset in catalog.CardBuffAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / CardBuffData";
                _ValidateCommonNestedSemantics(asset.Data, context, errors);
                _ValidateCardBuffNestedSemantics(asset.Data, context, errors);
            }

            foreach (var asset in catalog.PlayerBuffAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / PlayerBuffData";
                _ValidateCommonNestedSemantics(asset.Data, context, errors);
                _ValidatePlayerBuffNestedSemantics(asset.Data, context, errors);
            }

            foreach (var asset in catalog.CharacterBuffAssets.Where(asset => asset != null))
            {
                var context = $"{AssetDatabase.GetAssetPath(asset)} / CharacterBuffData";
                _ValidateCommonNestedSemantics(asset.Data, context, errors);
                _ValidateCharacterBuffNestedSemantics(asset.Data, context, errors);
            }

            _ValidateContentIds(catalog, errors);
            return errors.Distinct(ValidationIssueMessageComparer.Instance).ToArray();
        }

        private static void _ValidateCommonNestedSemantics(
            object data,
            string context,
            List<ValidationIssue> errors)
        {
            errors.AddRange(SerializedDataGraphUtility.ValidateRequiredReferences(data, context));
            ValueConditionValidator.ValidateConditions(data, context, errors);
            _ValidateDamageEffectSemantics(data, context, errors);
            ValueConditionValidator.ValidateIntegerValues(data, context, errors);
            _ValidateCardCollectionSemantics(data, context, errors);
            _ValidateCardStateSemantics(data, context, errors);
            _ValidateBuffStateSemantics(data, context, errors);
            _ValidateTargetSemantics(data, context, errors);
            _ValidateCardOperationSemantics(data, context, errors);
        }

        public static IReadOnlyList<ValidationIssue> ValidateLocalization(
            GameContentCatalog catalog,
            ExcelDatas localizationData,
            IEnumerable<PlayerData> playerDatas)
        {
            if (catalog == null)
                return new[] { new ValidationIssue.MissingCatalogForLocalization() };
            if (localizationData == null)
                return new[] { new ValidationIssue.MissingLocalizationData() };

            var errors = new List<ValidationIssue>();
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

            var uiKeys = (localizationData.LocalizeUI ?? new List<LocalizeExcelData>())
                .Where(row => row != null).Select(row => row.Id).ToHashSet();
            foreach (var asset in catalog.CardAssets.Where(asset => asset != null))
                _ValidateCompositionDescriptions(asset.CardData, localizationData, errors);
            foreach (var asset in catalog.CardAssets.Where(asset => asset != null))
                _ValidateLocalizationKeys(
                    SerializedDataGraphUtility.Find<CardEffectOption>(asset.CardData)
                        .Select(option => (option.DescriptionKey, $"CardData[{asset.CardData?.ID}] 的 CardEffectOption.DescriptionKey")),
                    uiKeys, nameof(ExcelDatas.LocalizeUI), errors);

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

            return errors.Distinct(ValidationIssueMessageComparer.Instance).ToArray();
        }

        public static IReadOnlyList<ValidationIssue> ValidateCatalogCoverage()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : ValidateCatalogCoverage(
                    catalog,
                    ProjectAssetPaths.GameContent.SearchFolders);
        }

        public static IReadOnlyList<ValidationIssue> ValidateCatalogCoverage(
            GameContentCatalog catalog,
            IReadOnlyList<string> searchFolders)
        {
            if (catalog == null)
                return new[] { new ValidationIssue.MissingCatalogForCoverage() };

            var errors = new List<ValidationIssue>();
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

        public static IReadOnlyList<ValidationIssue> ValidateReactionSessionRules()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateReactionSessionRules(catalog);
        }

        private static IReadOnlyList<ValidationIssue> _ValidateReactionSessionRules(
            GameContentCatalog catalog)
        {
            var errors = new List<ValidationIssue>();

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

        public static IReadOnlyList<ValidationIssue> ValidateReactionSessionRules(
            IReadOnlyDictionary<string, IReactionSessionData> sessions,
            string context)
        {
            if (sessions == null)
                return Array.Empty<ValidationIssue>();

            return sessions
                .SelectMany(pair => GetDuplicateTimings(pair.Value)
                    .Select(timing =>
                        new ValidationIssue.DuplicateReactionTiming(context, pair.Key, timing)))
                .ToArray();
        }

        public static IReadOnlyList<ValidationIssue> ValidateCardTransformRules()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateCardTransformRules(catalog);
        }

        private static IReadOnlyList<ValidationIssue> _ValidateCardTransformRules(
            GameContentCatalog catalog)
        {
            return catalog.CardAssets
                .OfType<StandardCardDataScriptable>()
                .SelectMany(asset => ValidateCardTransformRules(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset)))
                .ToArray();
        }

        public static IReadOnlyList<ValidationIssue> ValidateCardScriptableTypes()
        {
            var catalog = _LoadDefaultCatalog();
            if (catalog == null)
                return new[] { _GetMissingCatalogError() };

            return ValidateCardScriptableTypes(
                catalog.CardAssets,
                EditorAssetUtility.FindAssets<DeckScriptable>(
                    ProjectAssetPaths.GameContent.SearchFolders));
        }

        public static IReadOnlyList<ValidationIssue> ValidateCardScriptableTypes(
            IEnumerable<CardDataScriptableBase> cardAssets,
            IEnumerable<DeckScriptable> deckAssets)
        {
            var cards = (cardAssets ?? Enumerable.Empty<CardDataScriptableBase>())
                .Where(asset => asset != null)
                .ToArray();
            var errors = new List<ValidationIssue>();

            foreach (var duplicateId in cards
                .Where(asset => asset.CardData != null)
                .GroupBy(asset => asset.CardData.ID)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1))
            {
                errors.Add(new ValidationIssue.DuplicateCardDataId(duplicateId.Key));
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
                    errors.Add(new ValidationIssue.MissingCardData(context));
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
                            new ValidationIssue.MissingSelfTransformTarget(context, cardData.ID, applyOperation.TargetCardDataId));
                    }
                    else if (targetAsset is not StandardCardDataScriptable)
                    {
                        errors.Add(
                            new ValidationIssue.WrongSelfTransformTargetType(context, cardData.ID, applyOperation.TargetCardDataId));
                    }
                }
            }

            foreach (var deck in deckAssets ?? Enumerable.Empty<DeckScriptable>())
            {
                if (deck == null)
                    continue;

                if ((deck.Cards ?? Array.Empty<StandardCardDataScriptable>()).Any(card => card == null))
                    errors.Add(new ValidationIssue.NullDeckCardReference(GetAssetContext(deck)));
            }

            return errors;
        }

        private static IReadOnlyList<ValidationIssue> ValidateCardFormOverrideEffects(
            CardData cardData,
            string context,
            IReadOnlyDictionary<string, CardDataScriptableBase> assetsById)
        {
            var errors = new List<ValidationIssue>();
            var effects = EnumerateCardEffects(cardData)
                .OfType<ApplyCardFormOverrideEffect>()
                .ToArray();

            for (var index = 0; index < effects.Length; index++)
            {
                var effect = effects[index];
                var effectContext =
                    $"{context} / CardData[{cardData.ID}].ApplyCardFormOverrideEffect[{index}]";

                if (effect.TargetCards == null)
                    errors.Add(new ValidationIssue.MissingOverrideTargets(effectContext));
                if (string.IsNullOrWhiteSpace(effect.OverrideKey))
                    errors.Add(new ValidationIssue.MissingOverrideKey(effectContext));

                if (string.IsNullOrWhiteSpace(effect.TargetCardDataId))
                {
                    errors.Add(new ValidationIssue.MissingOverrideTargetId(effectContext));
                }
                else if (!assetsById.TryGetValue(effect.TargetCardDataId, out var targetAsset))
                {
                    errors.Add(
                        new ValidationIssue.MissingOverrideTarget(effectContext, effect.TargetCardDataId));
                }
                else if (targetAsset is not OverrideCardDataScriptable)
                {
                    errors.Add(
                        new ValidationIssue.WrongOverrideTargetType(effectContext, effect.TargetCardDataId));
                }

                var releaseRules = effect.ReleaseRules;
                if (releaseRules == null || releaseRules.Count == 0)
                {
                    errors.Add(new ValidationIssue.MissingOverrideReleaseRules(effectContext));
                }
                else
                {
                    for (var ruleIndex = 0; ruleIndex < releaseRules.Count; ruleIndex++)
                    {
                        var rule = releaseRules[ruleIndex];
                        var ruleContext = $"{effectContext}.ReleaseRules[{ruleIndex}]";
                        if (rule == null)
                        {
                            errors.Add(new ValidationIssue.MissingRequiredValue(ruleContext));
                            continue;
                        }

                        if (rule.Timing == GameTiming.None)
                            errors.Add(new ValidationIssue.MissingTiming(ruleContext));
                        else if (!IsTimingDispatchSupported(rule.Timing))
                            errors.Add(new ValidationIssue.UndispatchedTiming(ruleContext, rule.Timing));

                        ValidateOverrideReleaseRuleConditions(
                            rule.Conditions,
                            ruleContext,
                            effect.ReactionSessions,
                            errors);
                    }
                }

                if (effect.ReactionSessions == null)
                {
                    errors.Add(new ValidationIssue.MissingOverrideSessions(effectContext));
                    continue;
                }

                foreach (var session in effect.ReactionSessions)
                {
                    if (string.IsNullOrWhiteSpace(session.Key))
                        errors.Add(new ValidationIssue.EmptyOverrideSessionKey(effectContext));
                    if (session.Value == null)
                        errors.Add(new ValidationIssue.MissingOverrideSessionData(effectContext, session.Key));
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
            ICollection<ValidationIssue> errors)
        {
            if (conditions == null)
            {
                errors.Add(new ValidationIssue.NullConditions(context));
                return;
            }

            for (var index = 0; index < conditions.Count; index++)
            {
                var condition = conditions[index];
                var conditionContext = $"{context}.Conditions[{index}]";
                if (condition == null)
                {
                    errors.Add(new ValidationIssue.NullConditions(context));
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
                            errors.Add(new ValidationIssue.MissingOverrideConditionSessionKey(conditionContext));
                        }
                        else if (reactionSessions == null ||
                            !reactionSessions.ContainsKey(sessionCondition.SessionKey))
                        {
                            errors.Add(
                                new ValidationIssue.UnknownOverrideConditionSession(conditionContext, sessionCondition.SessionKey));
                        }

                        if (sessionCondition.Conditions == null ||
                            sessionCondition.Conditions.Any(valueCondition => valueCondition == null))
                        {
                            errors.Add(new ValidationIssue.NullSessionConditions(conditionContext));
                        }
                        break;
                }
            }
        }

        public static IReadOnlyList<ValidationIssue> ValidateCardTransformRules(
            StandardCardData cardData,
            string context)
        {
            if (cardData == null)
                return new[] { new ValidationIssue.MissingCardData(context) };

            var rules = cardData.TransformRules ?? new List<CardTransformRule>();
            var errors = new List<ValidationIssue>();
            var validRules = rules.Where(rule => rule != null).ToArray();

            if (rules.Any(rule => rule == null))
                errors.Add(new ValidationIssue.NullTransformRule(context, cardData.ID));

            var transformKeys = validRules
                .Select(rule => rule.TransformKey)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct()
                .ToArray();
            if (transformKeys.Length > 1)
                errors.Add(new ValidationIssue.MultipleTransformKeys(context, cardData.ID));

            foreach (var ruleGroup in validRules
                .Where(rule => !string.IsNullOrWhiteSpace(rule.RuleId))
                .GroupBy(rule => rule.RuleId)
                .Where(group => group.Count() > 1))
            {
                errors.Add(new ValidationIssue.DuplicateTransformRuleId(context, cardData.ID, ruleGroup.Key));
            }

            foreach (var rule in validRules)
            {
                var ruleContext = $"{context} / CardData[{cardData.ID}].TransformRules[{rule.RuleId}]";
                if (string.IsNullOrWhiteSpace(rule.RuleId))
                    errors.Add(new ValidationIssue.MissingTransformRuleId(ruleContext));
                if (string.IsNullOrWhiteSpace(rule.TransformKey))
                    errors.Add(new ValidationIssue.MissingTransformKey(ruleContext));
                if (rule.Timing == GameTiming.None)
                    errors.Add(new ValidationIssue.MissingTiming(ruleContext));
                else if (!IsTimingDispatchSupported(rule.Timing))
                    errors.Add(new ValidationIssue.UndispatchedTiming(ruleContext, rule.Timing));
                if (rule.Conditions == null || rule.Conditions.Any(condition => condition == null))
                    errors.Add(new ValidationIssue.NullConditions(ruleContext));
                switch (rule.Operation)
                {
                    case null:
                        errors.Add(new ValidationIssue.MissingTransformOperation(ruleContext));
                        break;
                    case ApplyCardTransformOperationData apply
                        when string.IsNullOrWhiteSpace(apply.TargetCardDataId):
                        errors.Add(new ValidationIssue.MissingApplyTargetId(ruleContext));
                        break;
                }
            }

            return errors;
        }

        public static IReadOnlyList<ValidationIssue> ValidateEffectCommandHandlers()
        {
            return GetConcreteTypes<IEffectCommand>()
                .Where(type => !EffectCommandExecutor.HasEffectCommandHandler(type))
                .Select(type => new ValidationIssue.MissingCommandHandler(type))
                .ToArray();
        }

        public static IReadOnlyList<ValidationIssue> ValidateEffectResolvers()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateEffectResolvers(catalog);
        }

        public static IReadOnlyList<ValidationIssue> ValidateEffectResolvers(
            GameContentCatalog catalog)
        {
            return catalog == null
                ? new[] { new ValidationIssue.MissingCatalogForResolvers() }
                : _ValidateEffectResolvers(catalog);
        }

        private static IReadOnlyList<ValidationIssue> _ValidateEffectResolvers(
            GameContentCatalog catalog)
        {
            var errors = new List<ValidationIssue>();

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

        public static IReadOnlyList<ValidationIssue> ValidateReferenceIds()
        {
            var catalog = _LoadDefaultCatalog();
            return catalog == null
                ? new[] { _GetMissingCatalogError() }
                : _ValidateReferenceIds(catalog);
        }

        public static IReadOnlyList<ValidationIssue> ValidateReferenceIds(
            GameContentCatalog catalog)
        {
            return catalog == null
                ? new[] { new ValidationIssue.MissingCatalogForReferenceIds() }
                : _ValidateReferenceIds(catalog);
        }

        private static IReadOnlyList<ValidationIssue> _ValidateReferenceIds(
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
            var characterBuffIds = catalog.CharacterBuffAssets
                .Where(asset => asset != null)
                .Where(asset => asset.Data != null)
                .Select(asset => asset.Data.ID)
                .ToHashSet();
            var errors = new List<ValidationIssue>();

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
                    cardIds,
                    cardBuffIds,
                    errors);

            }

            foreach (var characterBuffAsset in catalog.CharacterBuffAssets.Where(asset => asset != null))
            {
                ValidateCharacterBuffReferenceIds(
                    characterBuffAsset.Data,
                    AssetDatabase.GetAssetPath(characterBuffAsset),
                    cardIds,
                    cardBuffIds,
                    errors);
            }

            foreach (var cardBuffAsset in catalog.CardBuffAssets.Where(asset => asset != null))
            {
                ValidateCardBuffReferenceIds(
                    cardBuffAsset.Data,
                    AssetDatabase.GetAssetPath(cardBuffAsset),
                    cardIds,
                    cardBuffIds,
                    errors);
            }

            foreach (var asset in catalog.PlayerBuffAssets.Where(asset => asset != null))
                _ValidateCoreBuffWriteReferenceIds(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset),
                    playerBuffIds,
                    cardBuffIds,
                    errors);

            foreach (var asset in catalog.CharacterBuffAssets.Where(asset => asset != null))
                _ValidateCoreBuffWriteReferenceIds(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset),
                    playerBuffIds,
                    cardBuffIds,
                    errors);

            foreach (var asset in catalog.CardBuffAssets.Where(asset => asset != null))
                _ValidateCoreBuffWriteReferenceIds(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset),
                    playerBuffIds,
                    cardBuffIds,
                    errors);

            foreach (var asset in catalog.CardAssets.Where(asset => asset != null))
                _ValidateBuffQueryReferenceIds(
                    asset.CardData,
                    AssetDatabase.GetAssetPath(asset),
                    playerBuffIds,
                    characterBuffIds,
                    cardBuffIds,
                    errors);

            foreach (var asset in catalog.PlayerBuffAssets.Where(asset => asset != null))
                _ValidateBuffQueryReferenceIds(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset),
                    playerBuffIds,
                    characterBuffIds,
                    cardBuffIds,
                    errors);

            foreach (var asset in catalog.CharacterBuffAssets.Where(asset => asset != null))
                _ValidateBuffQueryReferenceIds(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset),
                    playerBuffIds,
                    characterBuffIds,
                    cardBuffIds,
                    errors);

            foreach (var asset in catalog.CardBuffAssets.Where(asset => asset != null))
                _ValidateBuffQueryReferenceIds(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset),
                    playerBuffIds,
                    characterBuffIds,
                    cardBuffIds,
                    errors);

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

        private static void _ValidateCoreBuffWriteReferenceIds(
            object data,
            string assetPath,
            ISet<string> playerBuffIds,
            ISet<string> cardBuffIds,
            ICollection<ValidationIssue> errors)
        {
            foreach (var effect in SerializedDataGraphUtility.Find<AddPlayerBuffEffect>(data))
            {
                ValidateId(
                    playerBuffIds,
                    effect.BuffId,
                    $"{assetPath} 的 AddPlayerBuffEffect.BuffId",
                    errors);
            }

            foreach (var effect in SerializedDataGraphUtility.Find<ModifyPlayerBuffLevelEffect>(data))
            {
                ValidateId(
                    playerBuffIds,
                    effect.BuffId,
                    $"{assetPath} 的 ModifyPlayerBuffLevelEffect.BuffId",
                    errors);
            }

            foreach (var effect in SerializedDataGraphUtility.Find<RemovePlayerBuffEffect>(data))
            {
                ValidateId(
                    playerBuffIds,
                    effect.BuffId,
                    $"{assetPath} 的 RemovePlayerBuffEffect.BuffId",
                    errors);
            }

            foreach (var effect in SerializedDataGraphUtility.Find<AddCardBuffEffect>(data))
            {
                ValidateAddCardBuffDataIds(
                    effect.AddCardBuffDatas,
                    $"{assetPath} 的 AddCardBuffEffect.AddCardBuffDatas",
                    cardBuffIds,
                    errors);
            }

            foreach (var effect in SerializedDataGraphUtility.Find<RemoveCardBuffEffect>(data))
            {
                ValidateId(
                    cardBuffIds,
                    effect.BuffId,
                    $"{assetPath} 的 RemoveCardBuffEffect.BuffId",
                    errors);
            }
        }

        private static void _ValidateBuffQueryReferenceIds(
            object data,
            string assetPath,
            ISet<string> playerBuffIds,
            ISet<string> characterBuffIds,
            ISet<string> cardBuffIds,
            ICollection<ValidationIssue> errors)
        {
            _ValidateBuffQueryIds(
                SerializedDataGraphUtility.Find<PlayerBuffById>(data).Select(value => value.BuffId)
                    .Concat(SerializedDataGraphUtility.Find<PlayerBuffDataIdCondition>(data).Select(value => value.BuffId))
                    .Concat(SerializedDataGraphUtility.Find<PlayerBuffCollectionContainsIdCondition>(data).Select(value => value.BuffId)),
                playerBuffIds,
                "PlayerBuff",
                assetPath,
                errors);
            _ValidateBuffQueryIds(
                SerializedDataGraphUtility.Find<CharacterBuffById>(data).Select(value => value.BuffId)
                    .Concat(SerializedDataGraphUtility.Find<CharacterBuffDataIdCondition>(data).Select(value => value.BuffId))
                    .Concat(SerializedDataGraphUtility.Find<CharacterBuffCollectionContainsIdCondition>(data).Select(value => value.BuffId)),
                characterBuffIds,
                "CharacterBuff",
                assetPath,
                errors);
            _ValidateBuffQueryIds(
                SerializedDataGraphUtility.Find<CardBuffById>(data).Select(value => value.BuffId)
                    .Concat(SerializedDataGraphUtility.Find<CardBuffDataIdCondition>(data).Select(value => value.BuffId))
                    .Concat(SerializedDataGraphUtility.Find<CardBuffCollectionContainsIdCondition>(data).Select(value => value.BuffId)),
                cardBuffIds,
                "CardBuff",
                assetPath,
                errors);
        }

        private static void _ValidateBuffQueryIds(
            IEnumerable<string> queryIds,
            ISet<string> validIds,
            string buffType,
            string assetPath,
            ICollection<ValidationIssue> errors)
        {
            foreach (var queryId in queryIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
            {
                if (!validIds.Contains(queryId))
                    errors.Add(new ValidationIssue.UnknownBuffQueryId(assetPath, buffType, queryId));
            }
        }

        private static void ValidateCardEffects(CardData cardData, string assetPath, ICollection<ValidationIssue> errors)
        {
            if (cardData == null)
            {
                errors.Add(new ValidationIssue.MissingCardData(assetPath));
                return;
            }

            foreach (var option in SerializedDataGraphUtility.Find<CardEffectOption>(cardData))
            {
                foreach (var effect in option.Effects ?? Enumerable.Empty<ICardEffect>())
                    ValidateCardEffectResolver(effect, $"{assetPath} / 選項[{option.Id}].Effects", errors);
                foreach (var pair in option.TriggeredEffects ?? new Dictionary<CardTriggeredTiming, ConditionalCardEffect[]>())
                    foreach (var item in pair.Value ?? Array.Empty<ConditionalCardEffect>())
                        ValidateCardEffectResolver(item?.Effect, $"{assetPath} / 選項[{option.Id}].TriggeredEffects", errors);
            }
            foreach (var effect in cardData.Effects ?? Enumerable.Empty<ICardEffect>())
                ValidateCardEffectResolver(effect, $"{assetPath} / CardData[{cardData.ID}].Effects", errors);

            foreach (var pair in cardData.TriggeredEffects ??
                new Dictionary<CardTriggeredTiming, ConditionalCardEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCardEffect>())
                {
                    ValidateCardEffectResolver(
                        conditionalEffect?.Effect,
                        $"{assetPath} / CardData[{cardData.ID}].TriggeredEffects[{pair.Key}]",
                        errors);
                }
            }
        }

        private static void ValidatePlayerBuffEffects(PlayerBuffData buffData, string assetPath, ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
            {
                errors.Add(new ValidationIssue.MissingPlayerBuffData(assetPath));
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
                            new ValidationIssue.MissingPlayerBuffResolver(assetPath, buffData.ID, pair.Key, effect.GetType()));
                    }
                }
            }
        }

        private static void ValidateCharacterBuffEffects(CharacterBuffData buffData, string assetPath, ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
            {
                errors.Add(new ValidationIssue.MissingCharacterBuffData(assetPath));
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
                            new ValidationIssue.MissingCharacterBuffResolver(assetPath, buffData.ID, pair.Key, effect.GetType()));
                    }
                }
            }
        }

        private static void ValidateCardBuffEffects(CardBuffData buffData, string assetPath, ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
            {
                errors.Add(new ValidationIssue.MissingCardBuffData(assetPath));
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
            ICollection<ValidationIssue> errors)
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
            ISet<string> cardIds,
            ISet<string> cardBuffIds,
            ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
                return;

            ValidateCardOperationReferenceIds(
                buffData,
                $"{assetPath} / PlayerBuffData[{buffData.ID}]",
                cardIds,
                cardBuffIds,
                errors);
        }

        private static void ValidateCharacterBuffReferenceIds(
            CharacterBuffData buffData,
            string assetPath,
            ISet<string> cardIds,
            ISet<string> cardBuffIds,
            ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
                return;

            ValidateCardOperationReferenceIds(
                buffData,
                $"{assetPath} / CharacterBuffData[{buffData.ID}]",
                cardIds,
                cardBuffIds,
                errors);
        }

        private static void ValidateCardBuffReferenceIds(
            CardBuffData buffData,
            string assetPath,
            ISet<string> cardIds,
            ISet<string> cardBuffIds,
            ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
                return;

            ValidateCardOperationReferenceIds(
                buffData,
                $"{assetPath} / CardBuffData[{buffData.ID}]",
                cardIds,
                cardBuffIds,
                errors);
        }

        private static void ValidateDeckReferenceIds(
            DeckScriptable deck,
            string assetPath,
            ISet<string> cardIds,
            ICollection<ValidationIssue> errors)
        {
            if (deck == null)
                return;

            foreach (var cardAsset in deck.Cards ?? Array.Empty<StandardCardDataScriptable>())
            {
                if (cardAsset == null)
                {
                    errors.Add(new ValidationIssue.NullDeckCardReference(assetPath));
                    continue;
                }

                ValidateId(cardIds, cardAsset.Data?.ID, $"{assetPath} 的 DeckScriptable.Cards", errors);
            }
        }

        private static void ValidatePlayerDeck(PlayerData playerData, string assetPath, ICollection<ValidationIssue> errors)
        {
            if (playerData == null)
            {
                errors.Add(new ValidationIssue.MissingPlayerData(assetPath));
                return;
            }

            if (playerData.Deck == null)
                errors.Add(new ValidationIssue.MissingPlayerDeck(assetPath, playerData.ID));
        }

        private static void ValidateCardEffectResolver(ICardEffect effect, string context, ICollection<ValidationIssue> errors)
        {
            if (effect != null && !EffectDataResolver.HasCardEffectResolver(effect.GetType()))
                errors.Add(new ValidationIssue.MissingCardResolver(context, effect.GetType()));
        }

        private static void ValidateCardBuffEffectResolver(ICardBuffEffect effect, string context, ICollection<ValidationIssue> errors)
        {
            if (effect != null && !EffectDataResolver.HasCardBuffEffectResolver(effect.GetType()))
                errors.Add(new ValidationIssue.MissingCardBuffResolver(context, effect.GetType()));
        }

        private static void _ValidateDamageEffectSemantics(
            object data,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var effect in SerializedDataGraphUtility.Find<DamageEffect>(data))
            {
                if (!Enum.IsDefined(typeof(DamageType), effect.Type))
                    errors.Add(new ValidationIssue.InvalidDamageType(context, effect.Type));
            }
        }

        private static IEnumerable<ICardEffect> EnumerateCardEffects(CardData cardData)
        {
            foreach (var option in SerializedDataGraphUtility.Find<CardEffectOption>(cardData))
            {
                foreach (var effect in option.Effects ?? Enumerable.Empty<ICardEffect>())
                    yield return effect;
                foreach (var pair in option.TriggeredEffects ?? new Dictionary<CardTriggeredTiming, ConditionalCardEffect[]>())
                    foreach (var effect in pair.Value ?? Array.Empty<ConditionalCardEffect>())
                        if (effect?.Effect != null) yield return effect.Effect;
            }
            foreach (var effect in cardData.Effects ?? Enumerable.Empty<ICardEffect>())
                yield return effect;

            foreach (var pair in cardData.TriggeredEffects ??
                new Dictionary<CardTriggeredTiming, ConditionalCardEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCardEffect>())
                {
                    if (conditionalEffect?.Effect != null)
                        yield return conditionalEffect.Effect;
                }
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
            ICollection<ValidationIssue> errors)
        {
            foreach (var addCardBuffData in addCardBuffDatas ?? Enumerable.Empty<AddCardBuffData>())
            {
                if (addCardBuffData == null)
                {
                    errors.Add(new ValidationIssue.NullAddedCardBuff(context));
                    continue;
                }

                ValidateId(cardBuffIds, addCardBuffData.CardBuffId, $"{context}.CardBuffId", errors);
            }
        }

        private static void ValidateId(ISet<string> validIds, string id, string context, ICollection<ValidationIssue> errors)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add(new ValidationIssue.MissingRequiredValue(context));
                return;
            }

            if (!validIds.Contains(id))
                errors.Add(new ValidationIssue.UnknownReferenceId(context, id));
        }

        private static IReadOnlyList<ValidationIssue> _ValidateContentIntegrity(
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
                .Distinct(ValidationIssueMessageComparer.Instance)
                .ToArray();
        }

        private static void _ValidateCardNestedSemantics(
            CardData cardData,
            string context,
            ICollection<ValidationIssue> errors)
        {
            if (cardData == null)
                return;

            var subSelections = cardData.SubSelects ?? new List<ISubSelectionGroup>();
            foreach (var create in SerializedDataGraphUtility.Find<CreateComposedCardEffect>(
                cardData, value => value is not CardEffectOption))
            {
                if (string.IsNullOrWhiteSpace(create.TemplateSelectionId) ||
                    !subSelections.OfType<CardTemplateSelectionGroup>().Any(group => group.Id == create.TemplateSelectionId))
                    errors.Add(new ValidationIssue.UnknownCompositionTemplate(context));
            }
            foreach (var pair in subSelections
                .Select((selection, index) => (Selection: selection, Index: index))
                .Where(pair => pair.Selection != null))
            {
                if (string.IsNullOrWhiteSpace(pair.Selection.Id))
                    errors.Add(new ValidationIssue.EmptySubSelectionId(context, pair.Index));
            }

            foreach (var duplicateId in subSelections
                .Where(selection => selection != null)
                .Select(selection => selection.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .GroupBy(id => id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add(new ValidationIssue.DuplicateSubSelectionId(context, duplicateId));
            }

            var existCardIds = subSelections
                .OfType<ExistCardSelectionGroup>()
                .Select(selection => selection.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet();

            var precedingIds = new HashSet<string>();
            for (var index = 0; index < subSelections.Count; index++)
            {
                if (subSelections[index] is not ExistCardSelectionGroup group)
                    continue;
                var groupContext = $"{context}.SubSelects[{index}]";
                if (!Enum.IsDefined(typeof(CardSelectionCountRule), group.CountRule))
                    errors.Add(new ValidationIssue.InvalidSelectionCountRule(groupContext, group.CountRule));
                if (ValueConditionValidator.TryEvaluateStaticInteger(group.SelectCount, out var count) && count < 0)
                    errors.Add(new ValidationIssue.NegativeSelectionCount(groupContext, count));

                foreach (var reference in SerializedDataGraphUtility.Find<SubSelectedCardCollection>(group))
                {
                    if (!string.IsNullOrWhiteSpace(reference.SelectionId) && !precedingIds.Contains(reference.SelectionId))
                        errors.Add(new ValidationIssue.InvalidPriorSelectionReference(groupContext, reference.SelectionId));
                }
                if (!string.IsNullOrWhiteSpace(group.Id))
                    precedingIds.Add(group.Id);
            }
            foreach (var selectedCards in SerializedDataGraphUtility.Find<SubSelectedCardCollection>(
                cardData, value => value is not CardEffectOption))
            {
                if (!string.IsNullOrWhiteSpace(selectedCards.SelectionId) &&
                    !existCardIds.Contains(selectedCards.SelectionId))
                {
                    errors.Add(new ValidationIssue.UnknownExistCardSelection(context, selectedCards.SelectionId));
                }
            }

            _ValidateAddCardBuffDataSemantics(cardData, context, errors);

            _ValidateTimingKeys(
                cardData.TriggeredEffects,
                $"{context}.TriggeredEffects",
                errors);

            foreach (var pair in cardData.TriggeredEffects ??
                new Dictionary<CardTriggeredTiming, ConditionalCardEffect[]>())
            {
                var conditionalEffects = pair.Value ?? Array.Empty<ConditionalCardEffect>();
                for (var index = 0; index < conditionalEffects.Length; index++)
                {
                    if (conditionalEffects[index] == null)
                        errors.Add(new ValidationIssue.NullTriggeredEffect(context, pair.Key, index));
                }
            }

        }

        private static void ValidateCardOperationReferenceIds(
            object data,
            string context,
            ISet<string> cardIds,
            ISet<string> cardBuffIds,
            ICollection<ValidationIssue> errors)
        {
            foreach (var createCard in SerializedDataGraphUtility.Find<CreateCardEffect>(data))
            {
                foreach (var cardId in createCard.CardDataIds ?? Enumerable.Empty<string>())
                {
                    ValidateId(cardIds, cardId, $"{context} 的 CreateCardEffect.CardDataIds", errors);
                }

                ValidateAddCardBuffDataIds(
                    createCard.AddCardBuffDatas,
                    $"{context} 的 CreateCardEffect.AddCardBuffDatas",
                    cardBuffIds,
                    errors);
            }

            foreach (var cloneCard in SerializedDataGraphUtility.Find<CloneCardEffect>(data))
            {
                ValidateAddCardBuffDataIds(
                    cloneCard.AddCardBuffDatas,
                    $"{context} 的 CloneCardEffect.AddCardBuffDatas",
                    cardBuffIds,
                    errors);
            }
        }

        private static void _ValidateCardCollectionSemantics(
            object data,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var collection in SerializedDataGraphUtility.Find<CombinedCardCollection>(data))
            {
                if (!Enum.IsDefined(typeof(SetOperationType), collection.Operation))
                    errors.Add(new ValidationIssue.InvalidCardSetOperation(context, collection.Operation));
            }

            foreach (var selectedCards in SerializedDataGraphUtility.Find<SubSelectedCardCollection>(data))
            {
                if (string.IsNullOrWhiteSpace(selectedCards.SelectionId))
                    errors.Add(new ValidationIssue.MissingSubSelectionId(context));
            }

            foreach (var cards in SerializedDataGraphUtility.Find<CardsOfPlayer>(data))
            {
                if (cards.Zone == CardCollectionType.None ||
                    !Enum.IsDefined(typeof(CardCollectionType), cards.Zone))
                {
                    errors.Add(
                        new ValidationIssue.InvalidCardZone(context, cards.Zone));
                }
            }

            foreach (var cards in SerializedDataGraphUtility.Find<FilteredCardCollection>(data))
            {
                if (cards.Conditions == null || cards.Conditions.Count == 0)
                {
                    errors.Add(
                        new ValidationIssue.EmptyFilteredCardConditions(context));
                }
            }

            foreach (var condition in SerializedDataGraphUtility.Find<CardCollectionAnyCondition>(data))
            {
                if (condition.Conditions == null || condition.Conditions.Count == 0)
                {
                    errors.Add(
                        new ValidationIssue.EmptyAnyCardConditions(context));
                }
            }

            foreach (var condition in SerializedDataGraphUtility.Find<CardCollectionAllCondition>(data))
            {
                if (condition.Conditions == null || condition.Conditions.Count == 0)
                {
                    errors.Add(
                        new ValidationIssue.EmptyAllCardConditions(context));
                }
            }
        }

        private static void _ValidateCardOperationSemantics(
            object data,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var effect in SerializedDataGraphUtility.Find<CreateComposedCardEffect>(data))
            {
                if (effect.Target == null) errors.Add(new ValidationIssue.MissingComposedCardTarget(context));
                if (!effect.CreateDestination.IsNormalCardZone())
                    errors.Add(new ValidationIssue.InvalidComposedCardDestination(context));
            }
            foreach (var effect in SerializedDataGraphUtility.Find<CreateCardEffect>(data))
            {
                if (effect.Target == null)
                    errors.Add(new ValidationIssue.MissingCreateCardTarget(context));
                if (!effect.CreateDestination.IsNormalCardZone())
                {
                    errors.Add(
                        new ValidationIssue.InvalidCreateCardDestination(context, effect.CreateDestination));
                }
                if (effect.CardDataIds == null || effect.CardDataIds.Count == 0)
                    errors.Add(new ValidationIssue.EmptyCreatedCardIds(context));
            }

            foreach (var effect in SerializedDataGraphUtility.Find<CloneCardEffect>(data))
            {
                if (effect.Target == null)
                    errors.Add(new ValidationIssue.MissingCloneCardTarget(context));
                if (effect.ClonedCards == null)
                    errors.Add(new ValidationIssue.MissingClonedCards(context));
                if (!effect.CloneDestination.IsNormalCardZone())
                {
                    errors.Add(
                        new ValidationIssue.InvalidCloneCardDestination(context, effect.CloneDestination));
                }
            }
        }

        private static void _ValidateCardStateSemantics(
            object data,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var condition in SerializedDataGraphUtility.Find<CardTypesCondition>(data))
                _ValidateCardStateSet(
                    condition.CardTypes, condition.Condition, nameof(CardTypesCondition), context, errors);

            foreach (var condition in SerializedDataGraphUtility.Find<CardThemesCondition>(data))
                _ValidateCardStateSet(
                    condition.CardThemes, condition.Condition, nameof(CardThemesCondition), context, errors);

            foreach (var condition in SerializedDataGraphUtility.Find<CardRaritiesCondition>(data))
                _ValidateCardStateSet(
                    condition.CardRarities, condition.Condition, nameof(CardRaritiesCondition), context, errors);

            foreach (var condition in SerializedDataGraphUtility.Find<CardPropertiesCondition>(data))
                _ValidateCardStateSet(
                    condition.CardProperties, condition.Condition, nameof(CardPropertiesCondition), context, errors);
        }

        private static void _ValidateCardStateSet<T>(
            IReadOnlyCollection<T> values,
            SetConditionType setCondition,
            string conditionName,
            string context,
            ICollection<ValidationIssue> errors)
            where T : Enum
        {
            if (setCondition == SetConditionType.None ||
                !Enum.IsDefined(typeof(SetConditionType), setCondition))
            {
                errors.Add(new ValidationIssue.InvalidSetCondition(context, conditionName, setCondition));
            }

            if (values == null || values.Count == 0)
            {
                errors.Add(new ValidationIssue.EmptyComparisonValues(context, conditionName));
                return;
            }

            foreach (var value in values)
            {
                if (!Enum.IsDefined(typeof(T), value) || Convert.ToInt64(value) == 0)
                    errors.Add(new ValidationIssue.InvalidComparisonValue<T>(context, conditionName, value));
            }
        }

        private static void _ValidateBuffStateSemantics(
            object data,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var value in SerializedDataGraphUtility.Find<PlayerBuffIntegerProperty>(data))
            {
                if (!Enum.IsDefined(
                    typeof(PlayerBuffIntegerProperty.PlayerBuffIntegerValueType),
                    value.Property))
                {
                    errors.Add(
                        new ValidationIssue.InvalidPlayerBuffProperty(context, value.Property));
                }
            }

            foreach (var value in SerializedDataGraphUtility.Find<CharacterBuffIntegerProperty>(data))
            {
                if (!Enum.IsDefined(
                    typeof(CharacterBuffIntegerProperty.CharacterBuffIntegerValueType),
                    value.Property))
                {
                    errors.Add(
                        new ValidationIssue.InvalidCharacterBuffProperty(context, value.Property));
                }
            }

            foreach (var value in SerializedDataGraphUtility.Find<CardBuffIntegerProperty>(data))
            {
                if (!Enum.IsDefined(
                    typeof(CardBuffIntegerProperty.CardBuffIntegerValueType),
                    value.Property))
                {
                    errors.Add(
                        new ValidationIssue.InvalidCardBuffProperty(context, value.Property));
                }
            }

            _ValidateBuffIds(
                SerializedDataGraphUtility.Find<PlayerBuffById>(data).Select(value => value.BuffId)
                    .Concat(SerializedDataGraphUtility.Find<PlayerBuffDataIdCondition>(data).Select(value => value.BuffId))
                    .Concat(SerializedDataGraphUtility.Find<PlayerBuffCollectionContainsIdCondition>(data).Select(value => value.BuffId)),
                "PlayerBuff",
                context,
                errors);
            _ValidateBuffIds(
                SerializedDataGraphUtility.Find<CharacterBuffById>(data).Select(value => value.BuffId)
                    .Concat(SerializedDataGraphUtility.Find<CharacterBuffDataIdCondition>(data).Select(value => value.BuffId))
                    .Concat(SerializedDataGraphUtility.Find<CharacterBuffCollectionContainsIdCondition>(data).Select(value => value.BuffId)),
                "CharacterBuff",
                context,
                errors);
            _ValidateBuffIds(
                SerializedDataGraphUtility.Find<CardBuffById>(data).Select(value => value.BuffId)
                    .Concat(SerializedDataGraphUtility.Find<CardBuffDataIdCondition>(data).Select(value => value.BuffId))
                    .Concat(SerializedDataGraphUtility.Find<CardBuffCollectionContainsIdCondition>(data).Select(value => value.BuffId)),
                "CardBuff",
                context,
                errors);
        }

        private static void _ValidateBuffIds(
            IEnumerable<string> buffIds,
            string buffType,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var buffId in buffIds)
            {
                if (string.IsNullOrWhiteSpace(buffId))
                    errors.Add(new ValidationIssue.MissingBuffQueryId(context, buffType));
            }
        }

        private static void _ValidateTargetSemantics(
            object data,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var player in SerializedDataGraphUtility.Find<PlayerByFaction>(data))
            {
                if (player.Faction == Faction.None ||
                    !Enum.IsDefined(typeof(Faction), player.Faction))
                {
                    errors.Add(
                        new ValidationIssue.InvalidPlayerFaction(context, player.Faction));
                }
            }

            foreach (var index in SerializedDataGraphUtility.Find<IndexOfCardCollection>(data))
            {
                if (index.Order is not (OrderType.Ascending or OrderType.Descending))
                {
                    errors.Add(
                        new ValidationIssue.InvalidCardOrder(context, index.Order));
                }
            }
        }

        private static void _ValidateCardBuffNestedSemantics(
            CardBuffData buffData,
            string context,
            ICollection<ValidationIssue> errors)
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
                    new ValidationIssue.UnregisteredLifeTime(context, buffData.LifeTimeData.GetType()));
            }
        }

        private static void _ValidatePlayerBuffNestedSemantics(
            PlayerBuffData buffData,
            string context,
            ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
                return;

            if (buffData.MaxLevel <= 0)
                errors.Add(new ValidationIssue.NonPositiveMaxLevel(context));

            _ValidateSessionSemantics(buffData.Sessions, $"{context}.Sessions", errors);
            _ValidateTimingKeys(buffData.BuffEffects, $"{context}.BuffEffects", errors);
            _ValidateAddCardBuffDataSemantics(buffData, context, errors);

            if (buffData.LifeTimeData != null &&
                buffData.LifeTimeData is not AlwaysLifeTimePlayerBuffData &&
                buffData.LifeTimeData is not PlayerBuffTurnLifeTimeData)
            {
                errors.Add(
                    new ValidationIssue.UnregisteredLifeTime(context, buffData.LifeTimeData.GetType()));
            }

            var sessions = buffData.Sessions ??
                new Dictionary<string, IReactionSessionData>();
            foreach (var condition in SerializedDataGraphUtility.Find<PlayerBuffSessionCondition>(buffData))
            {
                if (string.IsNullOrWhiteSpace(condition.SessionKey))
                {
                    errors.Add(new ValidationIssue.MissingPlayerBuffConditionSession(context));
                }
                else if (!sessions.ContainsKey(condition.SessionKey))
                {
                    errors.Add(
                        new ValidationIssue.UnknownPlayerBuffConditionSession(context, condition.SessionKey));
                }
            }

            foreach (var value in SerializedDataGraphUtility.Find<PlayerBuffSessionInteger>(buffData))
            {
                if (string.IsNullOrWhiteSpace(value.SessionIntegerId))
                {
                    errors.Add(new ValidationIssue.MissingPlayerBuffIntegerSession(context));
                }
                else if (!sessions.TryGetValue(value.SessionIntegerId, out var session))
                {
                    errors.Add(
                        new ValidationIssue.UnknownPlayerBuffIntegerSession(context, value.SessionIntegerId));
                }
                else if (session is not SessionInteger)
                {
                    errors.Add(
                        new ValidationIssue.WrongPlayerBuffIntegerSessionType(context, value.SessionIntegerId));
                }
            }
        }

        private static void _ValidateCharacterBuffNestedSemantics(
            CharacterBuffData buffData,
            string context,
            ICollection<ValidationIssue> errors)
        {
            if (buffData == null)
                return;

            if (buffData.MaxLevel <= 0)
                errors.Add(new ValidationIssue.NonPositiveMaxLevel(context));

            _ValidateSessionSemantics(buffData.Sessions, $"{context}.Sessions", errors);
            _ValidateTimingKeys(buffData.BuffEffects, $"{context}.BuffEffects", errors);

            if (buffData.LifeTimeData != null &&
                buffData.LifeTimeData is not AlwaysLifeTimeCharacterBuffData &&
                buffData.LifeTimeData is not TurnLifeTimeCharacterBuffData)
            {
                errors.Add(
                    new ValidationIssue.UnregisteredLifeTime(context, buffData.LifeTimeData.GetType()));
            }

            if (buffData.LifeTimeData is TurnLifeTimeCharacterBuffData turnLifeTime &&
                turnLifeTime.Turn <= 0)
            {
                errors.Add(new ValidationIssue.NonPositiveLifeTimeTurn(context));
            }
        }

        private static void _ValidateAddCardBuffDataSemantics(
            object data,
            string context,
            ICollection<ValidationIssue> errors)
        {
            foreach (var addCardBuffData in SerializedDataGraphUtility.Find<AddCardBuffData>(data))
            {
                if (addCardBuffData.Level is ConstInteger { Value: < 0 })
                    errors.Add(new ValidationIssue.NegativeAddedCardBuffLevel(context));
            }
        }


        private static void _ValidateSessionSemantics(
            IReadOnlyDictionary<string, IReactionSessionData> sessions,
            string context,
            ICollection<ValidationIssue> errors)
        {
            if (sessions == null)
                return;

            foreach (var pair in sessions)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    errors.Add(new ValidationIssue.EmptySessionKey(context));

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
                            new ValidationIssue.UnregisteredSessionType(context, pair.Key, pair.Value.GetType()));
                        break;
                }
            }
        }

        private static void _ValidateSessionTimings<T>(
            IReadOnlyList<T> rules,
            string context,
            Func<T, GameTiming> getTiming,
            ICollection<ValidationIssue> errors)
            where T : class
        {
            if (rules == null)
                return;

            for (var index = 0; index < rules.Count; index++)
            {
                var rule = rules[index];
                if (rule != null && getTiming(rule) == GameTiming.None)
                    errors.Add(new ValidationIssue.MissingIndexedTiming(context, index));
            }
        }

        private static void _ValidateTimingKeys<T>(
            IReadOnlyDictionary<T, ConditionalCardEffect[]> effects,
            string context,
            ICollection<ValidationIssue> errors)
            where T : struct, Enum
        {
            if (effects == null)
                return;

            foreach (var timing in effects.Keys)
            {
                if (Convert.ToInt32(timing) == 0)
                    errors.Add(new ValidationIssue.MissingTiming(context));
                else if (!Enum.IsDefined(typeof(T), timing))
                    errors.Add(new ValidationIssue.InvalidTiming<T>(context, timing));
            }
        }

        private static void _ValidateTimingKeys<T>(
            IReadOnlyDictionary<T, ConditionalCardBuffEffect[]> effects,
            string context,
            ICollection<ValidationIssue> errors)
            where T : struct, Enum
        {
            if (effects == null)
                return;

            foreach (var timing in effects.Keys)
            {
                if (Convert.ToInt32(timing) == 0)
                    errors.Add(new ValidationIssue.MissingTiming(context));
            }
        }

        private static void _ValidateTimingKeys<T>(
            IReadOnlyDictionary<T, ConditionalPlayerBuffEffect[]> effects,
            string context,
            ICollection<ValidationIssue> errors)
            where T : struct, Enum
        {
            if (effects == null)
                return;

            foreach (var timing in effects.Keys)
            {
                if (Convert.ToInt32(timing) == 0)
                    errors.Add(new ValidationIssue.MissingTiming(context));
            }
        }

        private static void _ValidateTimingKeys<T>(
            IReadOnlyDictionary<T, ConditionalCharacterBuffEffect[]> effects,
            string context,
            ICollection<ValidationIssue> errors)
            where T : struct, Enum
        {
            if (effects == null)
                return;

            foreach (var timing in effects.Keys)
            {
                if (Convert.ToInt32(timing) == 0)
                    errors.Add(new ValidationIssue.MissingTiming(context));
            }
        }

        private static void _ValidateContentIds(
            GameContentCatalog catalog,
            ICollection<ValidationIssue> errors)
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
            ICollection<ValidationIssue> errors)
        {
            var values = entries.ToArray();
            foreach (var entry in values.Where(entry => string.IsNullOrWhiteSpace(entry.Id)))
                errors.Add(new ValidationIssue.MissingContentId(entry.Context, dataType));

            foreach (var duplicateId in values
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Id))
                .GroupBy(entry => entry.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add(new ValidationIssue.DuplicateContentId(dataType, duplicateId));
            }
        }

        private static HashSet<string> _ValidateTitleInfoTable(
            IReadOnlyList<LocalizeExcelTitleData> rows,
            string tableName,
            bool requireInfo,
            ICollection<ValidationIssue> errors)
        {
            if (rows == null)
            {
                errors.Add(new ValidationIssue.MissingLocalizationTable(tableName));
                return new HashSet<string>();
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row == null)
                {
                    errors.Add(new ValidationIssue.NullLocalizationRow(tableName, index));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Id))
                    errors.Add(new ValidationIssue.EmptyLocalizationId(tableName, index));
                if (string.IsNullOrWhiteSpace(row.Title))
                    errors.Add(new ValidationIssue.EmptyLocalizationTitle(tableName, index));
                if (requireInfo && string.IsNullOrWhiteSpace(row.Info))
                    errors.Add(new ValidationIssue.EmptyLocalizationInfo(tableName, index));
            }

            foreach (var duplicateId in rows
                .Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .GroupBy(row => row.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add(new ValidationIssue.DuplicateLocalizationId(tableName, duplicateId));
            }

            return rows
                .Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .Select(row => row.Id)
                .ToHashSet();
        }

        private static void _ValidateInfoTable(
            IReadOnlyList<LocalizeExcelData> rows,
            string tableName,
            ICollection<ValidationIssue> errors)
        {
            if (rows == null)
            {
                errors.Add(new ValidationIssue.MissingLocalizationTable(tableName));
                return;
            }

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row == null)
                {
                    errors.Add(new ValidationIssue.NullLocalizationRow(tableName, index));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Id))
                    errors.Add(new ValidationIssue.EmptyLocalizationId(tableName, index));
                if (string.IsNullOrWhiteSpace(row.Info))
                    errors.Add(new ValidationIssue.EmptyLocalizationInfo(tableName, index));
            }

            foreach (var duplicateId in rows
                .Where(row => row != null && !string.IsNullOrWhiteSpace(row.Id))
                .GroupBy(row => row.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                errors.Add(new ValidationIssue.DuplicateLocalizationId(tableName, duplicateId));
            }
        }

        private static void _ValidateLocalizationKeys(
            IEnumerable<(string Key, string Context)> references,
            ISet<string> validKeys,
            string tableName,
            ICollection<ValidationIssue> errors)
        {
            foreach (var reference in references)
            {
                if (string.IsNullOrWhiteSpace(reference.Key))
                {
                    errors.Add(new ValidationIssue.MissingRequiredValue(reference.Context));
                }
                else if (!validKeys.Contains(reference.Key))
                {
                    errors.Add(
                        new ValidationIssue.MissingLocalizationKey(reference.Context, tableName));
                }
            }
        }

        private static void _ValidateCatalogCollection<T>(
            IReadOnlyList<T> catalogAssets,
            IReadOnlyList<T> scannedAssets,
            string collectionName,
            ICollection<ValidationIssue> errors)
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
                    errors.Add(new ValidationIssue.NullCatalogAsset(collectionName, index));
                    continue;
                }

                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    errors.Add(
                        new ValidationIssue.UnsavedCatalogAsset(collectionName, index, asset.name));
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
                    new ValidationIssue.DuplicateCatalogAsset(collectionName, duplicatePath));
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
                    new ValidationIssue.MissingCatalogAsset(collectionName, missingPath));
            }

            foreach (var extraPath in catalogPaths
                .Distinct(StringComparer.Ordinal)
                .Where(path => !scannedPathSet.Contains(path)))
            {
                errors.Add(
                    new ValidationIssue.OutOfScopeCatalogAsset(collectionName, extraPath));
            }

            var hasValidSet = catalogPaths.Count == catalogPathSet.Count &&
                catalogPathSet.SetEquals(scannedPathSet);
            if (hasValidSet && !catalogPaths.SequenceEqual(scannedPaths, StringComparer.Ordinal))
            {
                errors.Add(
                    new ValidationIssue.UnsortedCatalogAssets(collectionName));
            }
        }

        private static GameContentCatalog _LoadDefaultCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                ProjectAssetPaths.GameContent.Catalog);
        }

        private static ValidationIssue _GetMissingCatalogError()
        {
            return
                new ValidationIssue.MissingDefaultCatalog(ProjectAssetPaths.GameContent.Catalog);
        }

        private static string GetAssetContext(UnityEngine.Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(assetPath) ? asset.name : assetPath;
        }

        private static bool IsTimingDispatchSupported(GameTiming timing)
        {
            return timing is GameTiming.BeforeGameStart or
                GameTiming.AfterGameStart or
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
