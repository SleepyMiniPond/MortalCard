using System;
using System.Collections.Generic;
using System.Linq;

public class DiscardCardEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not DiscardCardEffect discardCardEffect)
            throw new InvalidOperationException($"DiscardCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new DiscardCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = discardCardEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            var destinationZone = card.IsConsumable() ?
                CardCollectionType.ExclusionZone :
                card.IsDisposable() ?
                    CardCollectionType.DisposeZone :
                    CardCollectionType.Graveyard;

            card.Owner(context.Model).MatchSome(cardOwner =>
            {
                cardOwner.CardManager.HandCard.GetCardOrNone(c => c.Identity == card.Identity)
                    .Map(handCard => CardCollectionType.HandCard)
                    .Else(cardOwner.CardManager.Deck.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(deckCard => CardCollectionType.Deck))
                    .MatchSome(cardStartZone =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            card,
                            cardStartZone,
                            destinationZone,
                            MoveCardType.Discard));
                    });
            });
        }
        return new EffectCommandSet(effectCommands);
    }
}
