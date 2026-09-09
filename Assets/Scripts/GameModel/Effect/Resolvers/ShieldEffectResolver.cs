using System;
using MortalGame.GameData;
using System.Collections.Generic;

namespace MortalGame.GameModel
{

    public class ShieldEffectResolver :
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
            if (effect is not ShieldEffect shieldEffect)
                throw new InvalidOperationException($"ShieldEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            var effectCommands = new List<IEffectCommand>();
            var intent = new ShieldIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            var targets = shieldEffect.Targets.Eval(triggerContext);

            foreach (var target in targets)
            {
                var characterTarget = new CharacterTarget(target);
                var targetIntent = new ShieldIntentTargetAction(context.Action.Source, characterTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                if (!shieldEffect.Value.Eval(targetTriggerContext).TryGetValue(out var shieldPoint) ||
                    shieldPoint < 0)
                {
                    continue;
                }

                effectCommands.Add(new ShieldEffectCommand(target, shieldPoint));
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
