using System;
using System.Collections.Generic;

public class LoseEnergyEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not LoseEnegyEffect loseEnergyEffect)
            throw new InvalidOperationException($"LoseEnergyEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new LoseEnergyIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = loseEnergyEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var playerTarget = new PlayerTarget(target);
            var targetIntent = new LoseEnergyIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var loseEnergyPoint = loseEnergyEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new LoseEnergyEffectCommand(target, loseEnergyPoint));
        }
        return new EffectCommandSet(effectCommands);
    }
}
