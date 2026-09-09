using System;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;

namespace MortalGame.GameModel
{

    public class DamageEffectResolver :
        ICardEffectResolver,
        IPlayerBuffEffectResolver,
        ICharacterBuffEffectResolver,
        ICardBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet IPlayerBuffEffectResolver.Resolve(
            TriggerContext context,
            IPlayerBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet ICharacterBuffEffectResolver.Resolve(
            TriggerContext context,
            ICharacterBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet ICardBuffEffectResolver.Resolve(
            TriggerContext context,
            ICardBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        private static EffectCommandSet _ResolveCore(
            TriggerContext context,
            object effect)
        {
            if (effect is not DamageEffect damageEffect)
            {
                throw new InvalidOperationException($"DamageEffectResolver 不支援的效果類型：{effect.GetType().Name}");
            }

            if (damageEffect.Targets == null || damageEffect.Value == null)
            {
                return EffectCommandSet.Empty;
            }

            return damageEffect.Type switch
            {
                DamageType.Normal => _Resolve(context, damageEffect.Targets, damageEffect.Value, DamageType.Normal, GameFormula.NormalDamagePoint),
                DamageType.Penetrate => _Resolve(context, damageEffect.Targets, damageEffect.Value, DamageType.Penetrate, GameFormula.PenetrateDamagePoint),
                DamageType.Additional => _Resolve(context, damageEffect.Targets, damageEffect.Value, DamageType.Additional, GameFormula.AdditionalDamagePoint),
                DamageType.Effective => _Resolve(context, damageEffect.Targets, damageEffect.Value, DamageType.Effective, GameFormula.EffectiveDamagePoint),
                _ => EffectCommandSet.Empty
            };
        }

        private static EffectCommandSet _Resolve(
            TriggerContext context,
            ITargetCharacterCollectionValue targets,
            IIntegerValue value,
            DamageType damageType,
            Func<TriggerContext, int, Option<int>> formulaFunc)
        {
            var effectCommands = new List<IEffectCommand>();
            var intent = new DamageIntentAction(context.Action.Source, damageType);
            var triggerContext = context with { Action = intent };
            var targetEntities = targets.Eval(triggerContext);

            foreach (var target in targetEntities)
            {
                var characterTarget = new CharacterTarget(target);
                var targetIntent = new DamageIntentTargetAction(context.Action.Source, characterTarget, damageType);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                if (!value.Eval(targetTriggerContext)
                        .FlatMap(damagePoint => formulaFunc(targetTriggerContext, damagePoint))
                        .TryGetValue(out var damageFormulaPoint) ||
                    damageFormulaPoint < 0)
                {
                    continue;
                }

                effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, damageType));
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
