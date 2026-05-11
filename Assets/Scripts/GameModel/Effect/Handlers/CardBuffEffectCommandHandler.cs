using System;
using System.Linq;

public class CardBuffEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        return command switch
        {
            AddCardBuffEffectCommand c         => _HandleAdd(context, c),
            RemoveCardBuffEffectCommand c      => _HandleRemove(context, c),
            ModifyCardBuffLevelEffectCommand c => _HandleModify(context, c),
            _ => throw new InvalidOperationException($"CardBuffEffectCommandHandler 不支援的命令類型：{command.GetType().Name}")
        };
    }

    private static CommandApplyResult _HandleAdd(TriggerContext context, AddCardBuffEffectCommand c)
    {
        var cardTarget = new CardTarget(c.Target);
        var addResult = c.Target.BuffManager.AddBuff(c.NewBuff);
        var resultAction = new AddCardBuffResultAction(context.Action.Source, cardTarget, addResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var cardBuffEvent = new AddCardBuffEvent(c.Target.Faction(context.Model), c.Target.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(cardBuffEvent));
    }

    private static CommandApplyResult _HandleRemove(TriggerContext context, RemoveCardBuffEffectCommand c)
    {
        var cardTarget = new CardTarget(c.Target);
        var removeResult = c.Target.BuffManager.RemoveBuff(c.ExistBuff);
        var resultAction = new RemoveCardBuffResultAction(context.Action.Source, cardTarget, removeResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var cardBuffEvent = new RemoveCardBuffEvent(c.Target.Faction(context.Model), c.Target.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(cardBuffEvent));
    }

    private static CommandApplyResult _HandleModify(TriggerContext context, ModifyCardBuffLevelEffectCommand c)
    {
        var cardTarget = new CardTarget(c.Target);
        var modifyResult = c.Target.BuffManager.ModifyBuffLevel(c.BuffId, c.Level);
        var resultAction = new ModifyCardBuffLevelResultAction(context.Action.Source, cardTarget, modifyResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(resultAction);
        var cardBuffEvent = new ModifyCardBuffLevelEvent(c.Target.Faction(context.Model), c.Target.ToInfo(context.Model));
        return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(cardBuffEvent));
    }
}
