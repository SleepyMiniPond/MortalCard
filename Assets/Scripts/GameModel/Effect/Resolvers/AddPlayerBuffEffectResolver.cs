using System;
using System.Collections.Generic;
using System.Linq;
using Optional;

public class AddPlayerBuffEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not AddPlayerBuffEffect addBuffEffect)
            throw new InvalidOperationException($"AddPlayerBuffEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new AddPlayerBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = addBuffEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var playerTarget = new PlayerTarget(target);
            var targetIntent = new AddPlayerBuffIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var level = addBuffEffect.Level.Eval(targetTriggerContext);

            if (target.BuffManager.Buffs.Any(buff => buff.PlayerBuffDataId == addBuffEffect.BuffId))
            {
                effectCommands.Add(new ModifyPlayerBuffLevelEffectCommand(
                    target,
                    addBuffEffect.BuffId,
                    level));
            }
            else
            {
                var caster = triggerContext.Action.Source switch
                {
                    PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                    CardPlaySource cardPlaySource => cardPlaySource.Card.Owner(triggerContext.Model),
                    _ => Option.None<IPlayerEntity>()
                };

                var buffLibrary = triggerContext.Model.ContextManager.PlayerBuffLibrary;
                var resultBuff = new PlayerBuffEntity(
                    addBuffEffect.BuffId,
                    Guid.NewGuid(),
                    level,
                    caster,
                    buffLibrary.GetBuffProperties(addBuffEffect.BuffId)
                        .Select(p => p.CreateEntity(triggerContext)),
                    buffLibrary.GetBuffLifeTime(addBuffEffect.BuffId)
                        .CreateEntity(triggerContext),
                    buffLibrary.GetBuffSessions(addBuffEffect.BuffId)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.CreateEntity(triggerContext)));

                effectCommands.Add(new AddPlayerBuffEffectCommand(target, resultBuff));
            }
        }
        return new EffectCommandSet(effectCommands);
    }
}
