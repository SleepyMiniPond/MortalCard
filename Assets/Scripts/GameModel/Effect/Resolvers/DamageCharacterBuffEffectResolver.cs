using System.Collections.Generic;

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
            var damagePoint = e.Value.Eval(targetTriggerContext);
            var damageFormulaPoint = GameFormula.EffectiveDamagePoint(targetTriggerContext, damagePoint);
            effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, DamageType.Effective));
        }

        return new EffectCommandSet(effectCommands);
    }
}
