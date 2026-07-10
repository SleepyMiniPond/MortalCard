using System;
using MortalGame.GameModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MortalGame.GameData;

namespace MortalGame.Tests
{

    public class ScriptableObjectDataValidationTests
    {
        [Test]
        public void AllEffectCommandTypes_HaveHandler()
        {
            var missingHandlers = GetConcreteTypes<IEffectCommand>()
                .Where(type => !EffectCommandExecutor.HasEffectCommandHandler(type))
                .Select(type => $"{type.Name} 缺少 IEffectCommandHandler 註冊")
                .ToArray();

            AssertNoErrors(missingHandlers);
        }

        [Test]
        public void ScriptableObjectEffects_HaveResolvers()
        {
            var errors = new List<string>();

            foreach (var cardAsset in LoadAssets<CardDataScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(cardAsset);
                ValidateCardEffects(cardAsset.Data, assetPath, errors);
            }

            foreach (var buffAsset in LoadAssets<PlayerBuffDataScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(buffAsset);
                ValidatePlayerBuffEffects(buffAsset.Data, assetPath, errors);
            }

            foreach (var buffAsset in LoadAssets<CharacterBuffDataScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(buffAsset);
                ValidateCharacterBuffEffects(buffAsset.Data, assetPath, errors);
            }

            foreach (var buffAsset in LoadAssets<CardBuffScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(buffAsset);
                ValidateCardBuffEffects(buffAsset.Data, assetPath, errors);
            }

            AssertNoErrors(errors);
        }

        [Test]
        public void ScriptableObjectReferenceIds_ExistInLibraries()
        {
            var cardIds = LoadAssets<CardDataScriptable>()
                .Where(asset => asset.Data != null)
                .Select(asset => asset.Data.ID)
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

            foreach (var cardAsset in LoadAssets<CardDataScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(cardAsset);
                ValidateCardReferenceIds(cardAsset.Data, assetPath, cardIds, cardBuffIds, playerBuffIds, errors);
            }

            foreach (var playerBuffAsset in LoadAssets<PlayerBuffDataScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(playerBuffAsset);
                ValidatePlayerBuffReferenceIds(playerBuffAsset.Data, assetPath, cardBuffIds, errors);
            }

            foreach (var cardBuffAsset in LoadAssets<CardBuffScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(cardBuffAsset);
                ValidateCardBuffReferenceIds(cardBuffAsset.Data, assetPath, cardBuffIds, errors);
            }

            foreach (var deckAsset in LoadAssets<DeckScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(deckAsset);
                ValidateDeckReferenceIds(deckAsset, assetPath, cardIds, errors);
            }

            foreach (var allyAsset in LoadAssets<AllyScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(allyAsset);
                ValidatePlayerDeck(allyAsset.Ally?.PlayerData, assetPath, errors);
            }

            foreach (var enemyAsset in LoadAssets<EnemyScriptable>())
            {
                var assetPath = AssetDatabase.GetAssetPath(enemyAsset);
                ValidatePlayerDeck(enemyAsset.Enemy?.PlayerData, assetPath, errors);
            }

            AssertNoErrors(errors);
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

            foreach (var triggeredEffect in cardData.TriggeredEffects ?? Enumerable.Empty<CardData.TriggeredCardEffect>())
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
                    if (effect == null)
                        continue;

                    if (!EffectDataResolver.HasPlayerBuffEffectResolver(effect.GetType()))
                        errors.Add($"{assetPath} / PlayerBuffData[{buffData.ID}].BuffEffects[{pair.Key}] 的 {effect.GetType().Name} 缺少 IPlayerBuffEffectResolver 註冊");
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
                    if (effect == null)
                        continue;

                    if (!EffectDataResolver.HasCharacterBuffEffectResolver(effect.GetType()))
                        errors.Add($"{assetPath} / CharacterBuffData[{buffData.ID}].BuffEffects[{pair.Key}] 的 {effect.GetType().Name} 缺少 ICharacterBuffEffectResolver 註冊");
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
                    ValidateCardBuffEffectResolver(conditionalEffect?.Effect, $"{assetPath} / CardBuffData[{buffData.ID}].Effects[{pair.Key}]", errors);
            }

            foreach (var pair in buffData.BuffEffects ?? new Dictionary<GameTiming, ConditionalCardBuffEffect[]>())
            {
                foreach (var conditionalEffect in pair.Value ?? Array.Empty<ConditionalCardBuffEffect>())
                    ValidateCardBuffEffectResolver(conditionalEffect?.Effect, $"{assetPath} / CardBuffData[{buffData.ID}].BuffEffects[{pair.Key}]", errors);
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

            foreach (var cardAsset in deck.Cards ?? Array.Empty<CardDataScriptable>())
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
            if (effect == null)
                return;

            if (!EffectDataResolver.HasCardEffectResolver(effect.GetType()))
                errors.Add($"{context} 的 {effect.GetType().Name} 缺少 ICardEffectResolver 註冊");
        }

        private static void ValidateCardBuffEffectResolver(ICardBuffEffect effect, string context, ICollection<string> errors)
        {
            if (effect == null)
                return;

            if (!EffectDataResolver.HasCardBuffEffectResolver(effect.GetType()))
                errors.Add($"{context} 的 {effect.GetType().Name} 缺少 ICardBuffEffectResolver 註冊");
        }

        private static IEnumerable<ICardEffect> EnumerateCardEffects(CardData cardData)
        {
            foreach (var effect in cardData.Effects ?? Enumerable.Empty<ICardEffect>())
                yield return effect;

            foreach (var triggeredEffect in cardData.TriggeredEffects ?? Enumerable.Empty<CardData.TriggeredCardEffect>())
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
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/ScriptableObjects" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null);
        }

        private static IEnumerable<Type> GetConcreteTypes<T>()
        {
            var targetType = typeof(T);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(type => type != null);
                    }
                })
                .Where(type => type != null)
                .Where(type => targetType.IsAssignableFrom(type))
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .Where(type => !type.ContainsGenericParameters)
                .Distinct();
        }

        private static void AssertNoErrors(IEnumerable<string> errors)
        {
            var errorList = errors.ToArray();
            Assert.IsEmpty(errorList, string.Join(Environment.NewLine, errorList));
        }
    }
}
