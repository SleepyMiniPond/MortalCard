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
        IReadOnlyCollection<IEffectCommand> Commands)
    {
        public static EffectCommandSet Empty { get; } =
            new(Array.Empty<IEffectCommand>());
    }

    public static class EffectDataResolver
    {
        #region Registry
        private static readonly ModifyCardPlayAttributeEffectResolver
            _modifyCardPlayAttributeResolver = new();

        private static readonly DamageEffectResolver _damageResolver = new();
        private static readonly ShieldEffectResolver _shieldResolver = new();
        private static readonly HealEffectResolver _healResolver = new();
        private static readonly GainEnergyEffectResolver _gainEnergyResolver = new();
        private static readonly LoseEnergyEffectResolver _loseEnergyResolver = new();
        private static readonly IncreaseDispositionEffectResolver _increaseDispositionResolver = new();
        private static readonly DecreaseDispositionEffectResolver _decreaseDispositionResolver = new();
        private static readonly AddPlayerBuffEffectResolver _addPlayerBuffResolver = new();
        private static readonly ModifyPlayerBuffLevelEffectResolver _modifyPlayerBuffLevelResolver = new();
        private static readonly RemovePlayerBuffEffectResolver _removePlayerBuffResolver = new();
        private static readonly DrawCardEffectResolver _drawCardResolver = new();
        private static readonly AddCardBuffEffectResolver _addCardBuffResolver = new();
        private static readonly RemoveCardBuffEffectResolver _removeCardBuffResolver = new();
        private static readonly DiscardCardEffectResolver _discardCardResolver = new();
        private static readonly ConsumeCardEffectResolver _consumeCardResolver = new();
        private static readonly DisposeCardEffectResolver _disposeCardResolver = new();
        private static readonly CreateCardEffectResolver _createCardResolver = new();
        private static readonly CloneCardEffectResolver _cloneCardResolver = new();

        private static readonly Dictionary<Type, ICardEffectResolver> _resolverRegistry = new()
        {

            [typeof(DamageEffect)] = _damageResolver,
            [typeof(HealEffect)] = _healResolver,
            [typeof(ShieldEffect)] = _shieldResolver,
            [typeof(GainEnergyEffect)] = _gainEnergyResolver,
            [typeof(LoseEnegyEffect)] = _loseEnergyResolver,
            [typeof(IncreaseDispositionEffect)] = _increaseDispositionResolver,
            [typeof(DecreaseDispositionEffect)] = _decreaseDispositionResolver,
            [typeof(AddPlayerBuffEffect)] = _addPlayerBuffResolver,
            [typeof(ModifyPlayerBuffLevelEffect)] = _modifyPlayerBuffLevelResolver,
            [typeof(RemovePlayerBuffEffect)] = _removePlayerBuffResolver,
            [typeof(DrawCardEffect)] = _drawCardResolver,
            [typeof(DiscardCardEffect)] = _discardCardResolver,
            [typeof(ConsumeCardEffect)] = _consumeCardResolver,
            [typeof(DisposeCardEffect)] = _disposeCardResolver,
            [typeof(CreateCardEffect)] = _createCardResolver,
            [typeof(CloneCardEffect)] = _cloneCardResolver,
            [typeof(AddCardBuffEffect)] = _addCardBuffResolver,
            [typeof(RemoveCardBuffEffect)] = _removeCardBuffResolver,
            [typeof(ApplyCardFormOverrideEffect)] = new ApplyCardFormOverrideEffectResolver(),
        };

        private static readonly Dictionary<Type, IPlayerBuffEffectResolver> _playerBuffResolverRegistry = new()
        {
            [typeof(DamageEffect)] = _damageResolver,
            [typeof(ShieldEffect)] = _shieldResolver,
            [typeof(HealEffect)] = _healResolver,
            [typeof(GainEnergyEffect)] = _gainEnergyResolver,
            [typeof(LoseEnegyEffect)] = _loseEnergyResolver,
            [typeof(IncreaseDispositionEffect)] = _increaseDispositionResolver,
            [typeof(DecreaseDispositionEffect)] = _decreaseDispositionResolver,
            [typeof(DrawCardEffect)] = _drawCardResolver,
            [typeof(DiscardCardEffect)] = _discardCardResolver,
            [typeof(ConsumeCardEffect)] = _consumeCardResolver,
            [typeof(DisposeCardEffect)] = _disposeCardResolver,
            [typeof(CreateCardEffect)] = _createCardResolver,
            [typeof(CloneCardEffect)] = _cloneCardResolver,
            [typeof(AddPlayerBuffEffect)] = _addPlayerBuffResolver,
            [typeof(ModifyPlayerBuffLevelEffect)] = _modifyPlayerBuffLevelResolver,
            [typeof(RemovePlayerBuffEffect)] = _removePlayerBuffResolver,
            [typeof(AddCardBuffEffect)] = _addCardBuffResolver,
            [typeof(RemoveCardBuffEffect)] = _removeCardBuffResolver,
            [typeof(ModifyCardPlayAttributeEffect)] = _modifyCardPlayAttributeResolver,
        };

        private static readonly Dictionary<Type, ICharacterBuffEffectResolver> _characterBuffResolverRegistry = new()
        {
            [typeof(DamageEffect)] = _damageResolver,
            [typeof(ShieldEffect)] = _shieldResolver,
            [typeof(HealEffect)] = _healResolver,
            [typeof(GainEnergyEffect)] = _gainEnergyResolver,
            [typeof(LoseEnegyEffect)] = _loseEnergyResolver,
            [typeof(IncreaseDispositionEffect)] = _increaseDispositionResolver,
            [typeof(DecreaseDispositionEffect)] = _decreaseDispositionResolver,
            [typeof(DrawCardEffect)] = _drawCardResolver,
            [typeof(DiscardCardEffect)] = _discardCardResolver,
            [typeof(ConsumeCardEffect)] = _consumeCardResolver,
            [typeof(DisposeCardEffect)] = _disposeCardResolver,
            [typeof(CreateCardEffect)] = _createCardResolver,
            [typeof(CloneCardEffect)] = _cloneCardResolver,
            [typeof(AddPlayerBuffEffect)] = _addPlayerBuffResolver,
            [typeof(ModifyPlayerBuffLevelEffect)] = _modifyPlayerBuffLevelResolver,
            [typeof(RemovePlayerBuffEffect)] = _removePlayerBuffResolver,
            [typeof(AddCardBuffEffect)] = _addCardBuffResolver,
            [typeof(RemoveCardBuffEffect)] = _removeCardBuffResolver,
            [typeof(ModifyCardPlayAttributeEffect)] = _modifyCardPlayAttributeResolver,
        };

        private static readonly Dictionary<Type, ICardBuffEffectResolver> _cardBuffResolverRegistry = new()
        {
            [typeof(DamageEffect)] = _damageResolver,
            [typeof(ShieldEffect)] = _shieldResolver,
            [typeof(HealEffect)] = _healResolver,
            [typeof(GainEnergyEffect)] = _gainEnergyResolver,
            [typeof(LoseEnegyEffect)] = _loseEnergyResolver,
            [typeof(IncreaseDispositionEffect)] = _increaseDispositionResolver,
            [typeof(DecreaseDispositionEffect)] = _decreaseDispositionResolver,
            [typeof(DrawCardEffect)] = _drawCardResolver,
            [typeof(DiscardCardEffect)] = _discardCardResolver,
            [typeof(ConsumeCardEffect)] = _consumeCardResolver,
            [typeof(DisposeCardEffect)] = _disposeCardResolver,
            [typeof(CreateCardEffect)] = _createCardResolver,
            [typeof(CloneCardEffect)] = _cloneCardResolver,
            [typeof(AddPlayerBuffEffect)] = _addPlayerBuffResolver,
            [typeof(ModifyPlayerBuffLevelEffect)] = _modifyPlayerBuffLevelResolver,
            [typeof(RemovePlayerBuffEffect)] = _removePlayerBuffResolver,
            [typeof(AddCardBuffEffect)] = _addCardBuffResolver,
            [typeof(RemoveCardBuffEffect)] = _removeCardBuffResolver,
            [typeof(ModifyCardPlayAttributeEffect)] = _modifyCardPlayAttributeResolver,
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
            return EffectCommandSet.Empty;
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
            return EffectCommandSet.Empty;
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
            return EffectCommandSet.Empty;
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
            return EffectCommandSet.Empty;
        }
        #endregion

    }

}
