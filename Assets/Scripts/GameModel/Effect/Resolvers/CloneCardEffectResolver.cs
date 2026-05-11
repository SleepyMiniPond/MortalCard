using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.Utilities;

public class CloneCardEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not CloneCardEffect cloneCardEffect)
            throw new InvalidOperationException($"CloneCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new CloneCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var target = cloneCardEffect.Target.Eval(triggerContext);

        target.MatchSome(targetPlayer =>
        {
            var playerTarget = new PlayerTarget(targetPlayer);
            var targetIntent = new CloneCardIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var cards = cloneCardEffect.ClonedCards.Eval(targetTriggerContext);

            foreach (var originCard in cards)
            {
                var playerCardTarget = new PlayerAndCardTarget(targetPlayer, originCard);
                targetIntent = new CloneCardIntentTargetAction(context.Action.Source, playerCardTarget);
                targetTriggerContext = targetTriggerContext with { Action = targetIntent };

                var cloneCard = originCard.Clone(includeCardBuffs: false, includeCardProperties: false);
                var cloneCardCaster = triggerContext.Action switch
                {
                    CardPlaySource cardSource => cardSource.Card.Owner(triggerContext.Model),
                    PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                    _ => Option.None<IPlayerEntity>()
                };
                cloneCardEffect.AddCardBuffDatas
                    .Select(addCardBuffData => CardBuffEntity.CreateFromData(
                        addCardBuffData.CardBuffId,
                        addCardBuffData.Level.Eval(targetTriggerContext),
                        cloneCardCaster,
                        targetTriggerContext,
                        context.Model.ContextManager.CardBuffLibrary))
                    .ForEach(cardBuff => cloneCard.BuffManager.AddBuff(cardBuff));

                effectCommands.Add(new CloneCardEffectCommand(
                    targetPlayer,
                    originCard,
                    cloneCard,
                    cloneCardEffect.CloneDestination));
            }
        });
        return new EffectCommandSet(effectCommands);
    }
}
