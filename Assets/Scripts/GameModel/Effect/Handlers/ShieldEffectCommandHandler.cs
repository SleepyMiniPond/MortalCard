using System.Linq;

public class ShieldEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        var c = (ShieldEffectCommand)command;
        var shieldResult = c.Target.HealthManager.GetShield(
            c.ShieldPoint,
            context.Model.ContextManager.Context);

        var resultAction = new ShieldResultAction(context.Action.Source, new CharacterTarget(c.Target), shieldResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var shieldEvent = new GetShieldEvent(c.Target.Faction(context.Model), c.Target, shieldResult);
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(shieldEvent));
    }
}
