using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional.Collections;

namespace MortalGame.GameModel
{

    public class RemoveCardBuffEffectResolver :
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
                    effectCommands.Add(new RemoveCardBuffEffectCommand(
                        card,
                        card.BuffManager.ActiveLayerHandle,
                        existBuff));
                });
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
