using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public class ModifyPlayerBuffLevelEffectResolver : ICardEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            if (effect is not ModifyPlayerBuffLevelEffect modifyLevelEffect)
            {
                throw new InvalidOperationException($"ModifyPlayerBuffLevelEffectResolver 不支援的效果類型：{effect.GetType().Name}");
            }

            var effectCommands = new List<IEffectCommand>();
            var intent = new ModifyPlayerBuffLevelIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            var targets = modifyLevelEffect.Targets.Eval(triggerContext);

            foreach (var target in targets)
            {
                var playerTarget = new PlayerTarget(target);
                var targetIntent = new ModifyPlayerBuffLevelIntentTargetAction(context.Action.Source, playerTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                if (!modifyLevelEffect.DeltaLevel.Eval(targetTriggerContext).TryGetValue(out var deltaLevel) ||
                    target.BuffManager.Buffs.All(buff => buff.PlayerBuffDataId != modifyLevelEffect.BuffId))
                {
                    continue;
                }

                effectCommands.Add(new ModifyPlayerBuffLevelEffectCommand(
                    target,
                    modifyLevelEffect.BuffId,
                    deltaLevel));
            }

            return new EffectCommandSet(effectCommands);
        }
    }
}
