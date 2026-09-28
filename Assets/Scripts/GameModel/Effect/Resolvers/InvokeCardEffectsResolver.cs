using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public sealed class InvokeCardEffectsResolver :
        ICardEffectResolver, IPlayerBuffEffectResolver, ICharacterBuffEffectResolver, ICardBuffEffectResolver
    {
        private static readonly CardCollectionType[] _allowedZones =
        {
            CardCollectionType.HandCard, CardCollectionType.Deck, CardCollectionType.Graveyard
        };

        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect) => _ResolveCore(context, effect);
        EffectCommandSet IPlayerBuffEffectResolver.Resolve(TriggerContext context, IPlayerBuffEffect effect) => _ResolveCore(context, effect);
        EffectCommandSet ICharacterBuffEffectResolver.Resolve(TriggerContext context, ICharacterBuffEffect effect) => _ResolveCore(context, effect);
        EffectCommandSet ICardBuffEffectResolver.Resolve(TriggerContext context, ICardBuffEffect effect) => _ResolveCore(context, effect);

        private static EffectCommandSet _ResolveCore(TriggerContext context, object effect)
        {
            if (effect is not InvokeCardEffects invoke)
                throw new InvalidOperationException($"InvokeCardEffectsResolver 不支援的效果類型：{effect.GetType().Name}");

            if (!ReactionContextQuery.InvokeCaster(context).TryGetValue(out var caster))
                return EffectCommandSet.Empty;

            var commands = new List<IEffectCommand>();
            foreach (var card in invoke.TargetCards.Eval(context)
                         .GroupBy(card => card.Identity).Select(group => group.First()))
            {
                if (!card.Owner(context.Model).TryGetValue(out var owner) ||
                    !owner.CardManager.GetCardAndZoneOrNone(card, _allowedZones).TryGetValue(out var located))
                    continue;

                // 固定身分與原區域；輪到這張卡時才讀取形態、選取與普通效果。
                commands.Add(
                    new InvokeCardEffectsCommand(
                        new InvokeCardEffectsRequest(
                            card.Identity,
                            owner.Identity,
                            located.Zone,
                            caster.Identity,
                            context.Action.Source)));
            }

            return new EffectCommandSet(commands);
        }
    }
}
