using System;
using System.Collections.Generic;

public class DecreaseDispositionEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not DecreaseDispositionEffect decreaseDispositionEffect)
            throw new InvalidOperationException($"DecreaseDispositionEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new DecreaseDispositionIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = decreaseDispositionEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            if (target is not AllyEntity ally) continue;

            var playerTarget = new PlayerTarget(ally);
            var targetIntent = new DecreaseDispositionIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var decreasePoint = decreaseDispositionEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new DecreaseDispositionEffectCommand(ally, decreasePoint));
        }
        return new EffectCommandSet(effectCommands);
    }
}
