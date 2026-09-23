using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public sealed class PlayCardEffectResolver :
        ICardEffectResolver, IPlayerBuffEffectResolver, ICharacterBuffEffectResolver, ICardBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect) => _ResolveCore(context, effect);
        EffectCommandSet IPlayerBuffEffectResolver.Resolve(TriggerContext context, IPlayerBuffEffect effect) => _ResolveCore(context, effect);
        EffectCommandSet ICharacterBuffEffectResolver.Resolve(TriggerContext context, ICharacterBuffEffect effect) => _ResolveCore(context, effect);
        EffectCommandSet ICardBuffEffectResolver.Resolve(TriggerContext context, ICardBuffEffect effect) => _ResolveCore(context, effect);

        private static EffectCommandSet _ResolveCore(TriggerContext context, object effect)
        {
            if (effect is not PlayCardEffect playCardEffect)
                throw new InvalidOperationException($"PlayCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            var commands = new List<IEffectCommand>();
            foreach (var card in playCardEffect.TargetCards.Eval(context)
                         .GroupBy(card => card.Identity).Select(group => group.First()))
            {
                if (card.Owner(context.Model).TryGetValue(out var owner))
                {
                    // 固定本次請求的身分；手牌、封印與選取等到實際執行時才判斷。
                    commands.Add(
                        new PlayCardEffectCommand(
                            new CardPlayRequest(
                                card.Identity, 
                                owner.Identity, 
                                context.Action.Source)));
                }
            }
            return new EffectCommandSet(commands);
        }
    }
}
