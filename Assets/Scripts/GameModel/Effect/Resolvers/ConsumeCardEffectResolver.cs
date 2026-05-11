using System;
using System.Collections.Generic;
using System.Linq;

public class ConsumeCardEffectResolver : ICardEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
    {
        if (effect is not ConsumeCardEffect consumeCardEffect)
            throw new InvalidOperationException($"ConsumeCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

        var effectCommands = new List<IEffectCommand>();
        var intent = new ConsumeCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = consumeCardEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            var destinationZone = card.IsDisposable() ?
                CardCollectionType.DisposeZone :
                CardCollectionType.ExclusionZone;

            card.Owner(context.Model).MatchSome(cardOwner =>
            {
                cardOwner.CardManager.HandCard.GetCardOrNone(c => c.Identity == card.Identity)
                    .Map(handCard => CardCollectionType.HandCard)
                    .Else(cardOwner.CardManager.Deck.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(deckCard => CardCollectionType.Deck))
                    .Else(cardOwner.CardManager.Graveyard.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(graveCard => CardCollectionType.Graveyard))
                    .MatchSome(cardStartZone =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            card,
                            cardStartZone,
                            destinationZone,
                            MoveCardType.Consume));
                    });
            });
        }
        return new EffectCommandSet(effectCommands);
    }
}
