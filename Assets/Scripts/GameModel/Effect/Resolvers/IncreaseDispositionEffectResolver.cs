using System;
using MortalGame.GameData;
using System.Collections.Generic;

namespace MortalGame.GameModel
{

    public class IncreaseDispositionEffectResolver :
        ICardEffectResolver,
        IPlayerBuffEffectResolver,
        ICharacterBuffEffectResolver,
        ICardBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet IPlayerBuffEffectResolver.Resolve(
            TriggerContext context,
            IPlayerBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet ICharacterBuffEffectResolver.Resolve(
            TriggerContext context,
            ICharacterBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet ICardBuffEffectResolver.Resolve(
            TriggerContext context,
            ICardBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        private static EffectCommandSet _ResolveCore(
            TriggerContext context,
            object effect)
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

                if (!increaseDispositionEffect.Value.Eval(targetTriggerContext).TryGetValue(out var increasePoint) ||
                    increasePoint < 0)
                {
                    continue;
                }

                effectCommands.Add(new IncreaseDispositionEffectCommand(ally, increasePoint));
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
