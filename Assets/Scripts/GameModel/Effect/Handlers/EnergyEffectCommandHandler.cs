using System;
using System.Linq;

public class EnergyEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        return command switch
        {
            GainEnergyEffectCommand c => _HandleGain(context, c),
            LoseEnergyEffectCommand c => _HandleLose(context, c),
            _ => throw new InvalidOperationException($"EnergyEffectCommandHandler 不支援的命令類型：{command.GetType().Name}")
        };
    }

    private static CommandApplyResult _HandleGain(TriggerContext context, GainEnergyEffectCommand c)
    {
        var result = c.Target.EnergyManager.GainEnergy(c.EnergyPoint);
        var resultAction = new GainEnergyResultAction(context.Action.Source, new PlayerTarget(c.Target), result);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var energyEvent = new GainEnergyEvent(c.Target.Faction, c.Target.EnergyManager.ToInfo(), result);
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(energyEvent));
    }

    private static CommandApplyResult _HandleLose(TriggerContext context, LoseEnergyEffectCommand c)
    {
        var result = c.Target.EnergyManager.LoseEnergy(c.EnergyPoint);
        var resultAction = new LoseEnergyResultAction(context.Action.Source, new PlayerTarget(c.Target), result);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var energyEvent = new LoseEnergyEvent(c.Target.Faction, c.Target.EnergyManager.ToInfo(), result);
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(energyEvent));
    }
}
