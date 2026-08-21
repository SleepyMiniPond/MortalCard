using System;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;

namespace MortalGame.GameModel
{

    public class DamageEffectResolver : ICardEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            return effect switch
            {
                DamageEffect e => _Resolve(context, e.Targets, e.Value, DamageType.Normal, GameFormula.NormalDamagePoint),
                PenetrateDamageEffect e => _Resolve(context, e.Targets, e.Value, DamageType.Penetrate, GameFormula.PenetrateDamagePoint),
                AdditionalAttackEffect e => _Resolve(context, e.Targets, e.Value, DamageType.Additional, GameFormula.AdditionalDamagePoint),
                EffectiveAttackEffect e => _Resolve(context, e.Targets, e.Value, DamageType.Effective, GameFormula.EffectiveDamagePoint),
                _ => throw new InvalidOperationException($"DamageEffectResolver 不支援的效果類型：{effect.GetType().Name}")
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
                        .TryGetValue(out var damageFormulaPoint))
                {
                    continue;
                }

                effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, damageType));
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
