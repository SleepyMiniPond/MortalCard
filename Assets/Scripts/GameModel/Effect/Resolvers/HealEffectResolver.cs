using System;
using System.Collections.Generic;

public class HealEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not HealEffect healEffect)
            throw new InvalidOperationException($"HealEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new HealIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = healEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new HealIntentTargetAction(context.Action.Source, characterTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var healPoint = healEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new HealEffectCommand(target, healPoint));
        }
        return new EffectCommandSet(effectCommands);
    }
}
