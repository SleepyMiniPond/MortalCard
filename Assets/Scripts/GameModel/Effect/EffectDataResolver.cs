using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;
using Sirenix.Utilities;
using UnityEngine;

namespace MortalGame.GameModel
{

    public record EffectCommandSet(
        IReadOnlyCollection<IEffectCommand> Commands);

    public static class EffectDataResolver
    {
        #region Registry
        private static readonly Dictionary<Type, ICardEffectResolver> _resolverRegistry = new()
        {

            [typeof(DamageEffect)] = new DamageEffectResolver(),
            [typeof(PenetrateDamageEffect)] = new DamageEffectResolver(),
            [typeof(AdditionalAttackEffect)] = new DamageEffectResolver(),
            [typeof(EffectiveAttackEffect)] = new DamageEffectResolver(),
            [typeof(HealEffect)] = new HealEffectResolver(),
            [typeof(ShieldEffect)] = new ShieldEffectResolver(),
            [typeof(GainEnergyEffect)] = new GainEnergyEffectResolver(),
            [typeof(LoseEnegyEffect)] = new LoseEnergyEffectResolver(),
            [typeof(IncreaseDispositionEffect)] = new IncreaseDispositionEffectResolver(),
            [typeof(DecreaseDispositionEffect)] = new DecreaseDispositionEffectResolver(),
            [typeof(AddPlayerBuffEffect)] = new AddPlayerBuffEffectResolver(),
            [typeof(RemovePlayerBuffEffect)] = new RemovePlayerBuffEffectResolver(),
            [typeof(DrawCardEffect)] = new DrawCardEffectResolver(),
            [typeof(DiscardCardEffect)] = new DiscardCardEffectResolver(),
            [typeof(ConsumeCardEffect)] = new ConsumeCardEffectResolver(),
            [typeof(DisposeCardEffect)] = new DisposeCardEffectResolver(),
            [typeof(CreateCardEffect)] = new CreateCardEffectResolver(),
            [typeof(CloneCardEffect)] = new CloneCardEffectResolver(),
            [typeof(AddCardBuffEffect)] = new AddCardBuffEffectResolver(),
            [typeof(RemoveCardBuffEffect)] = new RemoveCardBuffEffectResolver(),
            [typeof(ApplyCardFormOverrideEffect)] = new ApplyCardFormOverrideEffectResolver(),
        };

        private static readonly Dictionary<Type, IPlayerBuffEffectResolver> _playerBuffResolverRegistry = new()
        {
            [typeof(AdditionalDamagePlayerBuffEffect)] = new DamagePlayerBuffEffectResolver(),
            [typeof(EffectiveDamagePlayerBuffEffect)] = new DamagePlayerBuffEffectResolver(),
            [typeof(AddCardBuffPlayerBuffEffect)] = new AddCardBuffPlayerBuffEffectResolver(),
            [typeof(RemoveCardBuffPlayerBuffEffect)] = new RemoveCardBuffPlayerBuffEffectResolver(),
            [typeof(CardPlayEffectAttributeAdditionPlayerBuffEffect)] = new CardPlayEffectAttributeAdditionPlayerBuffEffectResolver(),
        };

        private static readonly Dictionary<Type, ICharacterBuffEffectResolver> _characterBuffResolverRegistry = new()
        {
            [typeof(EffectiveDamageCharacterBuffEffect)] = new DamageCharacterBuffEffectResolver(),
        };

        private static readonly Dictionary<Type, ICardBuffEffectResolver> _cardBuffResolverRegistry = new()
        {
            // 待 CardBuff 效果類型定義後填入
            // 例：[typeof(DamageCardBuffEffect)] = new DamageCardBuffEffectResolver(),
        };
        #endregion

        public static bool HasCardEffectResolver(Type effectType)
        {
            return _resolverRegistry.ContainsKey(effectType);
        }

        public static bool HasPlayerBuffEffectResolver(Type effectType)
        {
            return _playerBuffResolverRegistry.ContainsKey(effectType);
        }

        public static bool HasCharacterBuffEffectResolver(Type effectType)
        {
            return _characterBuffResolverRegistry.ContainsKey(effectType);
        }

        public static bool HasCardBuffEffectResolver(Type effectType)
        {
            return _cardBuffResolverRegistry.ContainsKey(effectType);
        }

        #region CardEffect
        public static EffectCommandSet ResolveCardEffect(
            TriggerContext context,
            ICardEffect cardEffect)
        {
            if (_resolverRegistry.TryGetValue(cardEffect.GetType(), out var resolver))
                return resolver.Resolve(context, cardEffect);

            Debug.LogWarning($"[EffectDataResolver] 未知的 ICardEffect 類型：{cardEffect.GetType().Name}，回傳空 CommandSet");
            return new EffectCommandSet(Array.Empty<IEffectCommand>());
        }
        #endregion

        #region PlayBuffEffect
        public static EffectCommandSet ResolvePlayerBuffEffect(
            TriggerContext context,
            IPlayerBuffEffect buffEffect)
        {
            if (_playerBuffResolverRegistry.TryGetValue(buffEffect.GetType(), out var resolver))
                return resolver.Resolve(context, buffEffect);

            Debug.LogWarning($"[EffectDataResolver] 未知的 IPlayerBuffEffect 類型：{buffEffect.GetType().Name}，回傳空 CommandSet");
            return new EffectCommandSet(Array.Empty<IEffectCommand>());
        }
        #endregion

        #region CharacterBuffEffect
        public static EffectCommandSet ResolveCharacterBuffEffect(
            TriggerContext context,
            ICharacterBuffEffect buffEffect)
        {
            if (_characterBuffResolverRegistry.TryGetValue(buffEffect.GetType(), out var resolver))
                return resolver.Resolve(context, buffEffect);

            Debug.LogWarning($"[EffectDataResolver] 未知的 ICharacterBuffEffect 類型：{buffEffect.GetType().Name}，回傳空 CommandSet");
            return new EffectCommandSet(Array.Empty<IEffectCommand>());
        }
        #endregion

        #region CardBuffEffect
        public static EffectCommandSet ResolveCardBuffEffect(
            TriggerContext context,
            ICardBuffEffect buffEffect)
        {
            if (_cardBuffResolverRegistry.TryGetValue(buffEffect.GetType(), out var resolver))
                return resolver.Resolve(context, buffEffect);

            Debug.LogWarning($"[EffectDataResolver] 未知的 ICardBuffEffect 類型：{buffEffect.GetType().Name}，回傳空 CommandSet");
            return new EffectCommandSet(Array.Empty<IEffectCommand>());
        }
        #endregion
    }

}
