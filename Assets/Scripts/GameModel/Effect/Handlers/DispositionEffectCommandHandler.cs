using System;
using System.Linq;

public class DispositionEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        return command switch
        {
            IncreaseDispositionEffectCommand c => _HandleIncrease(context, c),
            DecreaseDispositionEffectCommand c => _HandleDecrease(context, c),
            _ => throw new InvalidOperationException($"DispositionEffectCommandHandler 不支援的命令類型：{command.GetType().Name}")
        };
    }

    private static CommandApplyResult _HandleIncrease(TriggerContext context, IncreaseDispositionEffectCommand c)
    {
        var result = c.Target.DispositionManager.IncreaseDisposition(c.DispositionPoint);
        var resultAction = new IncreaseDispositionResultAction(context.Action.Source, new PlayerTarget(c.Target), result);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var dispositionEvent = new IncreaseDispositionEvent(c.Target.DispositionManager.ToInfo(), result.DeltaDisposition);
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(dispositionEvent));
    }

    private static CommandApplyResult _HandleDecrease(TriggerContext context, DecreaseDispositionEffectCommand c)
    {
        var result = c.Target.DispositionManager.DecreaseDisposition(c.DispositionPoint);
        var resultAction = new DecreaseDispositionResultAction(context.Action.Source, new PlayerTarget(c.Target), result);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var dispositionEvent = new DecreaseDispositionEvent(c.Target.DispositionManager.ToInfo(), result.DeltaDisposition);
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(dispositionEvent));
    }
}
