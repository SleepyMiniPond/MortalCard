using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public class DisposeCardEffectResolver :
        ICardEffectResolver,
        IPlayerBuffEffectResolver,
        ICharacterBuffEffectResolver,
        ICardBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
            => _ResolveCore(context, effect);

        EffectCommandSet IPlayerBuffEffectResolver.Resolve(
            TriggerContext context,
            IPlayerBuffEffect effect)
            => _ResolveCore(context, effect);

        EffectCommandSet ICharacterBuffEffectResolver.Resolve(
            TriggerContext context,
            ICharacterBuffEffect effect)
            => _ResolveCore(context, effect);

        EffectCommandSet ICardBuffEffectResolver.Resolve(
            TriggerContext context,
            ICardBuffEffect effect)
            => _ResolveCore(context, effect);

        private static EffectCommandSet _ResolveCore(TriggerContext context, object effect)
        {
            if (effect is not DisposeCardEffect disposeCardEffect)
                throw new InvalidOperationException(
                    $"DisposeCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            var effectCommands = new List<IEffectCommand>();
            var triggerContext = context with
            {
                Action = new DisposeCardIntentAction(context.Action.Source)
            };
            var cards = disposeCardEffect.TargetCards?
                .Eval(triggerContext)
                .GroupBy(card => card.Identity)
                .Select(group => group.First())
                ?? Enumerable.Empty<ICardEntity>();

            foreach (var card in cards)
            {
                card.Owner(context.Model).MatchSome(cardOwner =>
                {
                    cardOwner.CardManager.GetCardAndZoneOrNone(
                        card,
                        new[]
                        {
                            CardCollectionType.HandCard,
                            CardCollectionType.Deck,
                            CardCollectionType.Graveyard,
                            CardCollectionType.ExclusionZone
                        }).MatchSome(found =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            found.Card,
                            found.Zone,
                            CardCollectionType.DisposeZone,
                            MoveCardType.Dispose));
                    });
                });
            }

            return new EffectCommandSet(effectCommands);
        }
    }
}
