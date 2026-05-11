using System;
using System.Collections.Generic;

public class GainEnergyEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not GainEnergyEffect gainEnergyEffect)
            throw new InvalidOperationException($"GainEnergyEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new GainEnergyIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = gainEnergyEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var playerTarget = new PlayerTarget(target);
            var targetIntent = new GainEnergyIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var gainEnergyPoint = gainEnergyEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new GainEnergyEffectCommand(target, gainEnergyPoint));
        }
        return new EffectCommandSet(effectCommands);
    }
}
