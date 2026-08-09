using System.Collections.Generic;
using MortalGame.GameData;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{

    public class AddCardBuffPlayerBuffEffectResolver : IPlayerBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, IPlayerBuffEffect effect)
        {
            var addCardBuffEffect = (AddCardBuffPlayerBuffEffect)effect;
            var effectCommands = new List<IEffectCommand>();
            var intent = new AddCardBuffIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            var cards = addCardBuffEffect.Targets.Eval(triggerContext);

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
                            card.BuffManager.ActiveLayerHandle,
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
                            context.Model.ContextManager.CardBuffLibrary,
                            context.Model.ContextManager.CardBuffPropertyEntityFactory,
                            context.Model.ContextManager.CardBuffLifeTimeEntityFactory,
                            context.Model.ContextManager.ReactionSessionEntityFactory);

                        effectCommands.Add(new AddCardBuffEffectCommand(
                            card,
                            card.BuffManager.ActiveLayerHandle,
                            newCardBuff));
                    }
                }
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
