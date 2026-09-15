using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using Optional;
namespace MortalGame.GameModel
{

    public class DrawCardEffectCommandHandler : IEffectCommandHandler
    {
        public CommandApplyResult Handle(TriggerContext context, IEffectCommand command, IEffectQueueContext queue)
        {
            var c = (DrawCardEffectCommand)command;

            if (c.DrawCount > 0)
            {
                queue.EnqueueImmediate(Enumerable.Range(0, c.DrawCount)
                    .Select(_ => (EffectQueueItem)new DrawCardQueueItem(
                        context,
                        c.Target,
                        c.IsSystemInitiated)));
            }

            return CommandApplyResult.Empty;
        }

        internal static DrawSingleCardExecutionResult DrawSingleCard(
            TriggerContext context,
            IPlayerEntity target)
        {
            var resultActions = new List<BaseResultAction>();
            var events = new List<IGameEvent>();
            var cardManager = target.CardManager;
            var drawnCard = Option.None<ICardEntity>();

            if (cardManager.Deck.Cards.Count == 0 &&
                cardManager.Graveyard.Cards.Count > 0)
            {
                var graveyardCards = cardManager.Graveyard.PopAllCards();
                cardManager.Deck.EnqueueCardsThenShuffle(graveyardCards);

                var recycleDeckResultAction = new RecycleDeckResultAction(new PlayerTarget(target));
                var reactorEvents = context.Model.ObserveDerivedAction(context, recycleDeckResultAction);
                var recycleEvent = new RecycleGraveyardToDeckEvent(
                    Faction: target.Faction,
                    CardManagerInfo: cardManager.ToInfo());

                resultActions.Add(recycleDeckResultAction);
                events.AddRange(reactorEvents);
                events.Add(recycleEvent);
            }

            if (cardManager.Deck.PopCardOrNone().TryGetValue(out var drawCard))
            {
                cardManager.HandCard.AddCard(drawCard);

                var drawCardResultAction = new DrawCardResultAction(
                    context.Action.Source,
                    new PlayerTarget(target),
                    drawCard);
                var reactorEvents = context.Model.ObserveDerivedAction(context, drawCardResultAction);
                var drawCardEvent = new DrawCardEvent(
                    target.Faction,
                    drawCard.ToInfo(context.Model),
                    target.CardManager.ToInfo());

                resultActions.Add(drawCardResultAction);
                events.AddRange(reactorEvents);
                events.Add(drawCardEvent);
                drawnCard = drawCard.Some();
            }

            return new DrawSingleCardExecutionResult(
                new EffectResult(resultActions, events),
                drawnCard);
        }
    }

    internal sealed record DrawSingleCardExecutionResult(
        EffectResult EffectResult,
        Option<ICardEntity> DrawnCard);

    internal sealed record DrawCardQueueItem(
        TriggerContext Context,
        IPlayerEntity Target,
        bool IsSystemInitiated) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var drawResult = DrawCardEffectCommandHandler.DrawSingleCard(Context, Target);

            if (drawResult.DrawnCard.TryGetValue(out var drawnCard))
            {
                var timing = IsSystemInitiated
                    ? CardTriggeredTiming.Drawed
                    : CardTriggeredTiming.EffectDrawed;
                queue.EnqueueImmediate(CardTriggeredEffectDispatch.CreateItems(
                    Context,
                    drawnCard,
                    timing));
            }

            return drawResult.EffectResult;
        }
    }

}
