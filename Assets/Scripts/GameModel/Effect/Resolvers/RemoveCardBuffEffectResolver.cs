using System;
using System.Collections.Generic;
using System.Linq;
using Optional.Collections;

public class RemoveCardBuffEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not RemoveCardBuffEffect removeCardBuffEffect)
            throw new InvalidOperationException($"RemoveCardBuffEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new RemoveCardBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = removeCardBuffEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            var existBuffOpt = OptionCollectionExtensions.FirstOrNone(
                card.BuffManager.Buffs,
                buff => buff.CardBuffDataID == removeCardBuffEffect.BuffId);
            existBuffOpt.MatchSome(existBuff =>
            {
                effectCommands.Add(new RemoveCardBuffEffectCommand(card, existBuff));
            });
        }
        return new EffectCommandSet(effectCommands);
    }
}
