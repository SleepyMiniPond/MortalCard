using System;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional.Collections;

namespace MortalGame.GameModel
{

    public class RemovePlayerBuffEffectResolver :
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

}
