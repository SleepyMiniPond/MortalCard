using System;
using System.Collections.Generic;
using Optional.Collections;

public class RemovePlayerBuffEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not RemovePlayerBuffEffect removeBuffEffect)
            throw new InvalidOperationException($"RemovePlayerBuffEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new RemovePlayerBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = removeBuffEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var existBuffOpt = OptionCollectionExtensions.FirstOrNone(
                target.BuffManager.Buffs,
                buff => buff.PlayerBuffDataId == removeBuffEffect.BuffId);
            existBuffOpt.MatchSome(existBuff =>
            {
                effectCommands.Add(new RemovePlayerBuffEffectCommand(target, existBuff));
            });
        }
        return new EffectCommandSet(effectCommands);
    }
}
