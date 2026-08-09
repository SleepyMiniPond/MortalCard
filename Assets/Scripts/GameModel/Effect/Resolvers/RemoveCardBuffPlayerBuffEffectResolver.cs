using System.Collections.Generic;
using MortalGame.GameData;
using System.Linq;
using Optional.Collections;

namespace MortalGame.GameModel
{

    public class RemoveCardBuffPlayerBuffEffectResolver : IPlayerBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, IPlayerBuffEffect effect)
        {
            var removeCardBuffEffect = (RemoveCardBuffPlayerBuffEffect)effect;
            var effectCommands = new List<IEffectCommand>();
            var intent = new RemoveCardBuffIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            var cards = removeCardBuffEffect.Targets.Eval(triggerContext).ToList();

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
