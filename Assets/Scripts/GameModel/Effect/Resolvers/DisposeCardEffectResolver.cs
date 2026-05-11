using System;
using System.Collections.Generic;
using System.Linq;

public class DisposeCardEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not DisposeCardEffect disposeCardEffect)
            throw new InvalidOperationException($"DisposeCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new DisposeCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = disposeCardEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            card.Owner(context.Model).MatchSome(cardOwner =>
            {
                cardOwner.CardManager.HandCard.GetCardOrNone(c => c.Identity == card.Identity)
                    .Map(handCard => CardCollectionType.HandCard)
                    .Else(cardOwner.CardManager.Deck.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(deckCard => CardCollectionType.Deck))
                    .Else(cardOwner.CardManager.Graveyard.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(graveCard => CardCollectionType.Graveyard)
                    .Else(cardOwner.CardManager.ExclusionZone.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(exclusionCard => CardCollectionType.ExclusionZone)))
                    .MatchSome(cardStartZone =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            card,
                            cardStartZone,
                            CardCollectionType.DisposeZone,
                            MoveCardType.Dispose));
                    });
            });
        }
        return new EffectCommandSet(effectCommands);
    }
}
