using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;

public class AddCardBuffEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not AddCardBuffEffect addCardBuffEffect)
            throw new InvalidOperationException($"AddCardBuffEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new AddCardBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = addCardBuffEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            foreach (var addCardBuff in addCardBuffEffect.AddCardBuffDatas)
            {
                var cardTarget = new CardTarget(card);
                var targetIntent = new AddCardBuffIntentTargetAction(context.Action.Source, cardTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };
                var addLevel = addCardBuff.Level.Eval(targetTriggerContext);

                if (card.BuffManager.Buffs.Any(buff => buff.CardBuffDataID == addCardBuff.CardBuffId))
                {
                    effectCommands.Add(new ModifyCardBuffLevelEffectCommand(
                        card,
                        addCardBuff.CardBuffId,
                        addLevel));
                }
                else
                {
                    var caster = targetTriggerContext.Action switch
                    {
                        CardPlaySource cardSource => cardSource.Card.Owner(targetTriggerContext.Model),
                        PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                        _ => Option.None<IPlayerEntity>()
                    };

                    var newCardBuff = CardBuffEntity.CreateFromData(
                        addCardBuff.CardBuffId,
                        addLevel,
                        caster,
                        targetTriggerContext,
                        context.Model.ContextManager.CardBuffLibrary);

                    effectCommands.Add(new AddCardBuffEffectCommand(card, newCardBuff));
                }
            }
        }
        return new EffectCommandSet(effectCommands);
    }
}
