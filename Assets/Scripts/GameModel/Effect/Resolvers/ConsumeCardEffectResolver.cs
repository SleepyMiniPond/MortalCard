using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public class ConsumeCardEffectResolver :
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
            if (effect is not ConsumeCardEffect consumeCardEffect)
                throw new InvalidOperationException(
                    $"ConsumeCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            var effectCommands = new List<IEffectCommand>();
            var triggerContext = context with
            {
                Action = new ConsumeCardIntentAction(context.Action.Source)
            };
            var cards = consumeCardEffect.TargetCards?
                .Eval(triggerContext)
                .GroupBy(card => card.Identity)
                .Select(group => group.First())
                ?? Enumerable.Empty<ICardEntity>();

            foreach (var card in cards)
            {
                var destinationZone = card.IsDisposable()
                    ? CardCollectionType.DisposeZone
                    : CardCollectionType.ExclusionZone;

                card.Owner(context.Model).MatchSome(cardOwner =>
                {
                    cardOwner.CardManager.GetCardAndZoneOrNone(
                        card,
                        new[]
                        {
                            CardCollectionType.HandCard,
                            CardCollectionType.Deck,
                            CardCollectionType.Graveyard
                        }).MatchSome(found =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            found.Card,
                            found.Zone,
                            destinationZone,
                            MoveCardType.Consume));
                    });
                });
            }

            return new EffectCommandSet(effectCommands);
        }
    }
}
