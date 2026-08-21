using System.Collections.Generic;
using MortalGame.GameData;
using Optional;

namespace MortalGame.GameModel
{

    public class DamageCharacterBuffEffectResolver : ICharacterBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICharacterBuffEffect effect)
        {
            var e = (EffectiveDamageCharacterBuffEffect)effect;
            var effectCommands = new List<IEffectCommand>();
            var intent = new DamageIntentAction(context.Action.Source, DamageType.Effective);
            var triggerContext = context with { Action = intent };
            var targetEntities = e.Targets.Eval(triggerContext);

            foreach (var target in targetEntities)
            {
                var characterTarget = new CharacterTarget(target);
                var targetIntent = new DamageIntentTargetAction(context.Action.Source, characterTarget, DamageType.Effective);
                var targetTriggerContext = triggerContext with { Action = targetIntent };
                if (!e.Value.Eval(targetTriggerContext)
                        .FlatMap(damagePoint =>
                            GameFormula.EffectiveDamagePoint(targetTriggerContext, damagePoint))
                        .TryGetValue(out var damageFormulaPoint))
                {
                    continue;
                }
                effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, DamageType.Effective));
            }

            return new EffectCommandSet(effectCommands);
        }
    }

}
