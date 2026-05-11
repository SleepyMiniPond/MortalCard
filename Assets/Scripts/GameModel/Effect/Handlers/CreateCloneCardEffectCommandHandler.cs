using System;
using System.Linq;

public class CreateCloneCardEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        return command switch
        {
            CreateCardEffectCommand c => _HandleCreate(context, c),
            CloneCardEffectCommand  c => _HandleClone(context, c),
            _ => throw new InvalidOperationException($"CreateCloneCardEffectCommandHandler 不支援的命令類型：{command.GetType().Name}")
        };
    }

    private static CommandApplyResult _HandleCreate(TriggerContext context, CreateCardEffectCommand c)
    {
        var createResult = c.Target.CardManager.CreateNewCard(c.NewCard, c.Destination);
        var resultAction = new CreateCardResultAction(context.Action.Source, new PlayerTarget(c.Target), createResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var createCardEvent = new AddCardEvent(
            c.Target.Faction,
            createResult.Card.ToInfo(context.Model),
            c.Destination,
            c.Target.CardManager.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(createCardEvent));
    }

    private static CommandApplyResult _HandleClone(TriggerContext context, CloneCardEffectCommand c)
    {
        var cloneResult = c.Target.CardManager.CreateNewCard(c.ClonedCard, c.Destination);
        var resultAction = new CloneCardResultAction(
            context.Action.Source, new PlayerAndCardTarget(c.Target, c.ClonedCard), c.OriginCard, cloneResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var cloneCardEvent = new AddCardEvent(
            c.Target.Faction,
            cloneResult.Card.ToInfo(context.Model),
            c.Destination,
            c.Target.CardManager.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(cloneCardEvent));
    }
}
