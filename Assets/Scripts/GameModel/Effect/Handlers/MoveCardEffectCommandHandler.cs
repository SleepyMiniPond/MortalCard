using System.Linq;

public class MoveCardEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        var c = (MoveCardEffectCommand)command;
        var moveResult = c.Target.CardManager.MoveCard(c.Card, c.Start, c.Destination);

        var resultAction = new MoveCardResultAction(
            context.Action.Source, new CardTarget(c.Card), moveResult, c.MoveType);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var moveCardEvent = new MoveCardEvent(
            c.Target.Faction,
            moveResult.Card.ToInfo(context.Model),
            c.Start,
            c.Destination,
            c.Target.CardManager.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(moveCardEvent));
    }
}
