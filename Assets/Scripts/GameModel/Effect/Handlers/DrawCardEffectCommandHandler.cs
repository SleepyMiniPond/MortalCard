using System.Collections.Generic;

public class DrawCardEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        var c = (DrawCardEffectCommand)command;
        var resultActions = new List<BaseResultAction>();
        var events = new List<IGameEvent>();
        var cardManager = c.Target.CardManager;

        for (var i = 0; i < c.DrawCount; i++)
        {
            if (cardManager.Deck.Cards.Count == 0 &&
                cardManager.Graveyard.Cards.Count > 0)
            {
                var graveyardCards = cardManager.Graveyard.PopAllCards();
                cardManager.Deck.EnqueueCardsThenShuffle(graveyardCards);

                var recycleDeckResultAction = new RecycleDeckResultAction(new PlayerTarget(c.Target));
                var reactorEvents = context.Model.UpdateReactorSessionAction(recycleDeckResultAction);
                var recycleEvent = new RecycleGraveyardToDeckEvent(
                    Faction: c.Target.Faction,
                    CardManagerInfo: cardManager.ToInfo(context.Model));

                resultActions.Add(recycleDeckResultAction);
                events.AddRange(reactorEvents);
                events.Add(recycleEvent);
            }

            if (cardManager.Deck.PopCardOrNone().TryGetValue(out var drawCard))
            {
                cardManager.HandCard.AddCard(drawCard);

                var drawCardResultAction = new DrawCardResultAction(context.Action.Source, new PlayerTarget(c.Target), drawCard);
                var reactorEvents = context.Model.UpdateReactorSessionAction(drawCardResultAction);
                var drawCardEvent = new DrawCardEvent(
                    c.Target.Faction,
                    drawCard.ToInfo(context.Model),
                    c.Target.CardManager.ToInfo(context.Model));

                resultActions.Add(drawCardResultAction);
                events.AddRange(reactorEvents);
                events.Add(drawCardEvent);
            }
        }
        return new CommandApplyResult(resultActions, events);
    }
}
