using System;
using System.Collections.Generic;

public class IncreaseDispositionEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not IncreaseDispositionEffect increaseDispositionEffect)
            throw new InvalidOperationException($"IncreaseDispositionEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new IncreaseDispositionIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = increaseDispositionEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            if (target is not AllyEntity ally) continue;

            var playerTarget = new PlayerTarget(ally);
            var targetIntent = new IncreaseDispositionIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var increasePoint = increaseDispositionEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new IncreaseDispositionEffectCommand(ally, increasePoint));
        }
        return new EffectCommandSet(effectCommands);
    }
}
