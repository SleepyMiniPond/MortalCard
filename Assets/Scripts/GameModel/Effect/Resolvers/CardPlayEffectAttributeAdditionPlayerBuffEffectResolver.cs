using System.Collections.Generic;

public class CardPlayEffectAttributeAdditionPlayerBuffEffectResolver : IPlayerBuffEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, IPlayerBuffEffect effect)
    {
        var attributeEffect = (CardPlayEffectAttributeAdditionPlayerBuffEffect)effect;
        var effectCommands = new List<IEffectCommand>();
        var intent = new CardPlayEffectAttributeIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var value = attributeEffect.Value.Eval(triggerContext);

        effectCommands.Add(new ModifyCardAttributeEffectCommand(
            attributeEffect.Type,
            value));

        return new EffectCommandSet(effectCommands);
    }
}
