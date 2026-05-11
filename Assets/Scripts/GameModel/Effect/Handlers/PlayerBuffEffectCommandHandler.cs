using System;
using System.Linq;

public class PlayerBuffEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        return command switch
        {
            AddPlayerBuffEffectCommand c            => _HandleAdd(context, c),
            RemovePlayerBuffEffectCommand c         => _HandleRemove(context, c),
            ModifyPlayerBuffLevelEffectCommand c    => _HandleModify(context, c),
            _ => throw new InvalidOperationException($"PlayerBuffEffectCommandHandler 不支援的命令類型：{command.GetType().Name}")
        };
    }

    private static CommandApplyResult _HandleAdd(TriggerContext context, AddPlayerBuffEffectCommand c)
    {
        var playerTarget = new PlayerTarget(c.Target);
        var addResult = c.Target.BuffManager.AddBuff(c.NewBuff);
        var resultAction = new AddPlayerBuffResultAction(context.Action.Source, playerTarget, addResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var buffEvent = new AddPlayerBuffEvent(c.Target.Faction, addResult.PlayerBuff.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(buffEvent));
    }

    private static CommandApplyResult _HandleRemove(TriggerContext context, RemovePlayerBuffEffectCommand c)
    {
        var playerTarget = new PlayerTarget(c.Target);
        var removeResult = c.Target.BuffManager.RemoveBuff(c.ExistBuff);
        var resultAction = new RemovePlayerBuffResultAction(context.Action.Source, playerTarget, removeResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var buffEvent = new RemovePlayerBuffEvent(c.Target.Faction, c.ExistBuff.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(buffEvent));
    }

    private static CommandApplyResult _HandleModify(TriggerContext context, ModifyPlayerBuffLevelEffectCommand c)
    {
        var playerTarget = new PlayerTarget(c.Target);
        var modifyResult = c.Target.BuffManager.ModifyBuffLevel(c.BuffId, c.Level);
        var resultAction = new ModifyPlayerBuffLevelResultAction(context.Action.Source, playerTarget, modifyResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var buffEvent = new ModifyPlayerBuffLevelEvent(
            c.Target.Faction,
            c.Target.BuffManager.Buffs.First(b => b.PlayerBuffDataId == c.BuffId).ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(buffEvent));
    }
}
