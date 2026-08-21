using System.Collections.Generic;
using MortalGame.GameData;

namespace MortalGame.GameModel
{

    public class CardPlayEffectAttributeAdditionPlayerBuffEffectResolver : IPlayerBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, IPlayerBuffEffect effect)
        {
            var attributeEffect = (CardPlayEffectAttributeAdditionPlayerBuffEffect)effect;
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
