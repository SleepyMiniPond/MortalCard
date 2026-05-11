using System.Linq;

public class DamageEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        var c = (DamageEffectCommand)command;
        var damageResult = c.Target.HealthManager.TakeDamage(
            c.DamagePoint,
            context.Model.ContextManager.Context,
            c.DamageType);

        var resultAction = new DamageResultAction(context.Action.Source, new CharacterTarget(c.Target), damageResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var damageEvent = new DamageEvent(c.Target.Faction(context.Model), c.Target, damageResult);
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(damageEvent));
    }
}
