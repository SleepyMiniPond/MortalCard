using System;
using MortalGame.GameData;
using System.Collections.Generic;

namespace MortalGame.GameModel
{

    public class HealEffectResolver :
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
            if (effect is not HealEffect healEffect)
                throw new InvalidOperationException($"HealEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            var effectCommands = new List<IEffectCommand>();
            var intent = new HealIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            var targets = healEffect.Targets.Eval(triggerContext);

            foreach (var target in targets)
            {
                var characterTarget = new CharacterTarget(target);
                var targetIntent = new HealIntentTargetAction(context.Action.Source, characterTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                if (!healEffect.Value.Eval(targetTriggerContext).TryGetValue(out var healPoint) ||
                    healPoint < 0)
                {
                    continue;
                }

                effectCommands.Add(new HealEffectCommand(target, healPoint));
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
