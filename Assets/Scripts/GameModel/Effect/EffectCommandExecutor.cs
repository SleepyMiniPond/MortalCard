using System;
using System.Collections.Generic;

public record EffectResult(
    IReadOnlyCollection<BaseResultAction> Actions,
    IReadOnlyCollection<IGameEvent> Events);

public record CommandApplyResult(
    IEnumerable<BaseResultAction> Actions,
    IEnumerable<IGameEvent> Events);

public static class EffectCommandExecutor
{
    #region Registry
    private static readonly Dictionary<Type, IEffectCommandHandler> _handlerRegistry = new()
    {
        [typeof(DamageEffectCommand)]                   = new DamageEffectCommandHandler(),
        [typeof(HealEffectCommand)]                     = new HealEffectCommandHandler(),
        [typeof(ShieldEffectCommand)]                   = new ShieldEffectCommandHandler(),
        [typeof(GainEnergyEffectCommand)]               = new EnergyEffectCommandHandler(),
        [typeof(LoseEnergyEffectCommand)]               = new EnergyEffectCommandHandler(),
        [typeof(IncreaseDispositionEffectCommand)]      = new DispositionEffectCommandHandler(),
        [typeof(DecreaseDispositionEffectCommand)]      = new DispositionEffectCommandHandler(),
        [typeof(AddPlayerBuffEffectCommand)]            = new PlayerBuffEffectCommandHandler(),
        [typeof(RemovePlayerBuffEffectCommand)]         = new PlayerBuffEffectCommandHandler(),
        [typeof(ModifyPlayerBuffLevelEffectCommand)]    = new PlayerBuffEffectCommandHandler(),
        [typeof(DrawCardEffectCommand)]                 = new DrawCardEffectCommandHandler(),
        [typeof(MoveCardEffectCommand)]                 = new MoveCardEffectCommandHandler(),
        [typeof(CreateCardEffectCommand)]               = new CreateCloneCardEffectCommandHandler(),
        [typeof(CloneCardEffectCommand)]                = new CreateCloneCardEffectCommandHandler(),
        [typeof(AddCardBuffEffectCommand)]              = new CardBuffEffectCommandHandler(),
        [typeof(RemoveCardBuffEffectCommand)]           = new CardBuffEffectCommandHandler(),
        [typeof(ModifyCardBuffLevelEffectCommand)]      = new CardBuffEffectCommandHandler(),
        [typeof(ModifyCardAttributeEffectCommand)]      = new ModifyCardAttributeEffectCommandHandler(),
    };
    #endregion

    public static EffectResult ApplyEffectCommands(
        TriggerContext context,
        EffectCommandSet effectCommandSet)
    {
        var actionList = new List<BaseResultAction>();
        var eventList = new List<IGameEvent>();

        foreach (var command in effectCommandSet.Commands)
        {
            if (!_handlerRegistry.TryGetValue(command.GetType(), out var handler))
                throw new InvalidOperationException($"[EffectCommandExecutor] 未知的 IEffectCommand 類型：{command.GetType().Name}");

            var commandApplyResult = handler.Handle(context, command);
            actionList.AddRange(commandApplyResult.Actions);
            eventList.AddRange(commandApplyResult.Events);
        }

        return new EffectResult(actionList, eventList);
    }
}
