using System;
using System.Linq;

public class ModifyCardAttributeEffectCommandHandler : IEffectCommandHandler
{
    public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
    {
        var c = (ModifyCardAttributeEffectCommand)command;
        if (context.Action.Source is CardPlaySource cardPlaySource)
        {
            cardPlaySource.Attribute.ApplyModify(c.AdditionType, c.AdditionValue);
        }
        return new CommandApplyResult(Array.Empty<BaseResultAction>(), Enumerable.Empty<IGameEvent>());
    }
}
