using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.Utilities;

public class CreateCardEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not CreateCardEffect createCardEffect)
            throw new InvalidOperationException($"CreateCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new CreateCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var target = createCardEffect.Target.Eval(triggerContext);

        target.MatchSome(targetPlayer =>
        {
            foreach (var cardDataId in createCardEffect.CardDataIds)
            {
                var playerTarget = new PlayerTarget(targetPlayer);
                var targetIntent = new CreateCardIntentTargetAction(context.Action.Source, playerTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                var newCard = CardEntity.RuntimeCreateFromId(cardDataId, context.Model.ContextManager.CardLibrary);
                var createCardCaster = triggerContext.Action switch
                {
                    CardPlaySource cardSource => cardSource.Card.Owner(triggerContext.Model),
                    PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                    _ => Option.None<IPlayerEntity>()
                };
                createCardEffect.AddCardBuffDatas
                    .Select(addCardBuffData => CardBuffEntity.CreateFromData(
                        addCardBuffData.CardBuffId,
                        addCardBuffData.Level.Eval(targetTriggerContext),
                        createCardCaster,
                        targetTriggerContext,
                        context.Model.ContextManager.CardBuffLibrary))
                    .ForEach(cardBuff => newCard.BuffManager.AddBuff(cardBuff));

                effectCommands.Add(new CreateCardEffectCommand(
                    targetPlayer,
                    newCard,
                    createCardEffect.CreateDestination));
            }
        });
        return new EffectCommandSet(effectCommands);
    }
}
