using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional.Collections;

namespace MortalGame.GameModel
{

    public class AddCardBuffEffectResolver :
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
                    if (!addCardBuff.Level.Eval(targetTriggerContext).TryGetValue(out var addLevel) ||
                        addLevel < 0)
                    {
                        continue;
                    }

                    if (card.BuffManager.Buffs.Any(buff => buff.CardBuffDataID == addCardBuff.CardBuffId))
                    {
                        effectCommands.Add(new ModifyCardBuffLevelEffectCommand(
                            card,
                            card.BuffManager.ActiveLayerHandle,
                            addCardBuff.CardBuffId,
                            addLevel));
                    }
                    else
                    {
                        var caster = ReactionContextQuery.Caster(targetTriggerContext);

                        var newCardBuff = CardBuffEntity.CreateFromData(
                            addCardBuff.CardBuffId,
                            addLevel,
                            caster,
                            targetTriggerContext,
                            context.Model.ContextManager.CardBuffLibrary,
                            context.Model.ContextManager.CardBuffPropertyEntityFactory,
                            context.Model.ContextManager.CardBuffLifeTimeEntityFactory,
                            context.Model.ContextManager.ReactionSessionEntityFactory);

                        newCardBuff.MatchSome(buff =>
                            effectCommands.Add(new AddCardBuffEffectCommand(
                                card,
                                card.BuffManager.ActiveLayerHandle,
                                buff)));
                    }
                }
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
