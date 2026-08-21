using System;
using MortalGame.GameData;
using System.Collections.Generic;

namespace MortalGame.GameModel
{

    public class DrawCardEffectResolver : ICardEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            if (effect is not DrawCardEffect drawCardEffect)
                throw new InvalidOperationException($"DrawCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            var effectCommands = new List<IEffectCommand>();
            var intent = new DrawCardIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            var targets = drawCardEffect.Targets.Eval(triggerContext);

            foreach (var target in targets)
            {
                var playerTarget = new PlayerTarget(target);
                var targetIntent = new DrawCardIntentTargetAction(context.Action.Source, playerTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };
                if (!drawCardEffect.Value.Eval(targetTriggerContext).TryGetValue(out var drawCount))
                {
                    continue;
                }

                effectCommands.Add(new DrawCardEffectCommand(target, drawCount));
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
