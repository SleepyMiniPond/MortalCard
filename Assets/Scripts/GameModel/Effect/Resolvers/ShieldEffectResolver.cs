using System;
using System.Collections.Generic;

public class ShieldEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not ShieldEffect shieldEffect)
            throw new InvalidOperationException($"ShieldEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new ShieldIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = shieldEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new ShieldIntentTargetAction(context.Action.Source, characterTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var shieldPoint = shieldEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new ShieldEffectCommand(target, shieldPoint));
        }
        return new EffectCommandSet(effectCommands);
    }
}
