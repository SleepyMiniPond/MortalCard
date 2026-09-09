using System;
using System.Collections.Generic;
using MortalGame.GameData;

namespace MortalGame.GameModel
{

    public class ModifyCardPlayAttributeEffectResolver :
        IPlayerBuffEffectResolver,
        ICharacterBuffEffectResolver,
        ICardBuffEffectResolver
    {
        EffectCommandSet IPlayerBuffEffectResolver.Resolve(
            TriggerContext context,
            IPlayerBuffEffect effect)
        {
            return _Resolve(context, effect);
        }

        EffectCommandSet ICharacterBuffEffectResolver.Resolve(
            TriggerContext context,
            ICharacterBuffEffect effect)
        {
            return _Resolve(context, effect);
        }

        EffectCommandSet ICardBuffEffectResolver.Resolve(
            TriggerContext context,
            ICardBuffEffect effect)
        {
            return _Resolve(context, effect);
        }

        private static EffectCommandSet _Resolve(
            TriggerContext context,
            IReactionEffect effect)
        {
            if (effect is not ModifyCardPlayAttributeEffect attributeEffect)
            {
                throw new InvalidOperationException(
                    $"ModifyCardPlayAttributeEffectResolver 不支援的效果類型：{effect.GetType().Name}");
            }
            var effectCommands = new List<IEffectCommand>();
            var intent = new CardPlayEffectAttributeIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            if (!attributeEffect.Value.Eval(triggerContext).TryGetValue(out var value))
            {
                return new EffectCommandSet(effectCommands);
            }

            effectCommands.Add(new ModifyCardAttributeEffectCommand(
                attributeEffect.Type,
                value));

            return new EffectCommandSet(effectCommands);
        }
    }

}
