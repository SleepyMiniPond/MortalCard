using System;
using MortalGame.GameData;
using System.Collections.Generic;

namespace MortalGame.GameModel
{

    public class LoseEnergyEffectResolver :
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

                if (!loseEnergyEffect.Value.Eval(targetTriggerContext).TryGetValue(out var loseEnergyPoint) ||
                    loseEnergyPoint < 0)
                {
                    continue;
                }

                effectCommands.Add(new LoseEnergyEffectCommand(target, loseEnergyPoint));
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
