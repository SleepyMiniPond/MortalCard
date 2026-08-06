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
        private static readonly string[] AssetSearchFolders = { "Assets/ScriptableObjects" };

        public static IReadOnlyList<string> ValidateAll()
        {
            return ValidateCardScriptableTypes()
                .Concat(ValidateEffectCommandHandlers())
                .Concat(ValidateEffectResolvers())
                .Concat(ValidateReferenceIds())
                .Concat(ValidateCardTransformRules())
                .Concat(ValidateReactionSessionRules())
                .Distinct()
                .ToArray();
        }

        public static IReadOnlyList<string> ValidateReactionSessionRules()
        {
            var errors = new List<string>();

            foreach (var asset in LoadAssets<PlayerBuffDataScriptable>())
            {
                if (asset.Data == null)
                    continue;

                errors.AddRange(ValidateReactionSessionRules(
                    asset.Data.Sessions,
                    $"{AssetDatabase.GetAssetPath(asset)} / PlayerBuffData[{asset.Data.ID}].Sessions"));
            }

            foreach (var asset in LoadAssets<CharacterBuffDataScriptable>())
            {
                if (asset.Data == null)
                    continue;

                errors.AddRange(ValidateReactionSessionRules(
                    asset.Data.Sessions,
                    $"{AssetDatabase.GetAssetPath(asset)} / CharacterBuffData[{asset.Data.ID}].Sessions"));
            }

            foreach (var asset in LoadAssets<CardBuffScriptable>())
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
            return LoadAssets<StandardCardDataScriptable>()
                .SelectMany(asset => ValidateCardTransformRules(
                    asset.Data,
                    AssetDatabase.GetAssetPath(asset)))
                .ToArray();
        }

        public static IReadOnlyList<string> ValidateCardScriptableTypes()
        {
            return ValidateCardScriptableTypes(
                LoadAssets<CardDataScriptableBase>(),
                LoadAssets<DeckScriptable>());
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

                if (cardAsset is not StandardCardDataScriptable standardCardAsset)
                    continue;

                foreach (var applyOperation in (standardCardAsset.Data.TransformRules ?? new List<CardTransformRule>())
                    .Where(rule => rule?.Operation is ApplyCardTransformOperationData)
                    .Select(rule => (ApplyCardTransformOperationData)rule.Operation))
                {
                    if (assetsById.TryGetValue(applyOperation.TargetCardDataId, out var targetAsset) &&
                        targetAsset is not StandardCardDataScriptable)
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
            var errors = new List<string>();

            foreach (var cardAsset in LoadAssets<CardDataScriptableBase>())
                ValidateCardEffects(cardAsset.CardData, AssetDatabase.GetAssetPath(cardAsset), errors);

            foreach (var buffAsset in LoadAssets<PlayerBuffDataScriptable>())
                ValidatePlayerBuffEffects(buffAsset.Data, AssetDatabase.GetAssetPath(buffAsset), errors);

            foreach (var buffAsset in LoadAssets<CharacterBuffDataScriptable>())
                ValidateCharacterBuffEffects(buffAsset.Data, AssetDatabase.GetAssetPath(buffAsset), errors);

            foreach (var buffAsset in LoadAssets<CardBuffScriptable>())
                ValidateCardBuffEffects(buffAsset.Data, AssetDatabase.GetAssetPath(buffAsset), errors);

            return errors;
        }

        public static IReadOnlyList<string> ValidateReferenceIds()
        {
            var cardIds = LoadAssets<CardDataScriptableBase>()
                .Where(asset => asset.CardData != null)
                .Select(asset => asset.CardData.ID)
                .ToHashSet();
            var cardBuffIds = LoadAssets<CardBuffScriptable>()
                .Where(asset => asset.Data != null)
                .Select(asset => asset.Data.ID)
                .ToHashSet();
            var playerBuffIds = LoadAssets<PlayerBuffDataScriptable>()
                .Where(asset => asset.Data != null)
                .Select(asset => asset.Data.ID)
                .ToHashSet();
            var errors = new List<string>();

            foreach (var cardAsset in LoadAssets<CardDataScriptableBase>())
            {
                ValidateCardReferenceIds(
                    cardAsset.CardData,
                    AssetDatabase.GetAssetPath(cardAsset),
                    cardIds,
                    cardBuffIds,
                    playerBuffIds,
                    errors);
            }

            foreach (var playerBuffAsset in LoadAssets<PlayerBuffDataScriptable>())
            {
                ValidatePlayerBuffReferenceIds(
                    playerBuffAsset.Data,
                    AssetDatabase.GetAssetPath(playerBuffAsset),
                    cardBuffIds,
                    errors);
            }

            foreach (var cardBuffAsset in LoadAssets<CardBuffScriptable>())
            {
                ValidateCardBuffReferenceIds(
                    cardBuffAsset.Data,
                    AssetDatabase.GetAssetPath(cardBuffAsset),
                    cardBuffIds,
                    errors);
            }

            foreach (var deckAsset in LoadAssets<DeckScriptable>())
                ValidateDeckReferenceIds(deckAsset, AssetDatabase.GetAssetPath(deckAsset), cardIds, errors);

            foreach (var allyAsset in LoadAssets<AllyScriptable>())
                ValidatePlayerDeck(allyAsset.Ally?.PlayerData, AssetDatabase.GetAssetPath(allyAsset), errors);

            foreach (var enemyAsset in LoadAssets<EnemyScriptable>())
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

        private static IEnumerable<T> LoadAssets<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", AssetSearchFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null);
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
