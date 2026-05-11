using System.Linq;

public class HealEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        var c = (HealEffectCommand)command;
        var healResult = c.Target.HealthManager.GetHeal(
            c.HealPoint,
            context.Model.ContextManager.Context);

        var resultAction = new HealResultAction(context.Action.Source, new CharacterTarget(c.Target), healResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var healEvent = new GetHealEvent(c.Target.Faction(context.Model), c.Target, healResult);
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(healEvent));
    }
}
