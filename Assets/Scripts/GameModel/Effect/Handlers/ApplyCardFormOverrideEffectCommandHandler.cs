using System;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{
    public sealed class ApplyCardFormOverrideEffectCommandHandler : IEffectCommandHandler
    {
        public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
        {
            if (command is not ApplyCardFormOverrideEffectCommand applyCommand)
            {
                throw new InvalidOperationException(
                    $"ApplyCardFormOverrideEffectCommandHandler 不支援的命令類型：{command.GetType().Name}");
            }

            var reactionSessions = applyCommand.ReactionSessionDatas.ToDictionary(
                pair => pair.Key,
                pair => context.Model.ContextManager.ReactionSessionEntityFactory.Create(pair.Value));
            var operationResult = applyCommand.Target.TryApplyOverrideForm(
                applyCommand.OverrideKey,
                applyCommand.TargetCardDataId,
                context.Action.Source,
                applyCommand.ReleaseRules,
                reactionSessions);
            if (!operationResult.IsSuccess ||
                !applyCommand.Target.OverrideFormState.TryGetValue(out var overrideState))
            {
                return CommandApplyResult.Empty;
            }

            var target = new CardTarget(applyCommand.Target);
            var applyResult = new ApplyCardFormOverrideResult(operationResult, overrideState);
            var resultAction = new ApplyCardFormOverrideResultAction(
                context.Action.Source,
                target,
                applyResult);
            var events = context.Model.ObserveDerivedAction(context, resultAction).ToList();

            if (operationResult.BeforeCardDataId != operationResult.AfterCardDataId)
            {
                var source = new CardFormChangedSource(
                    applyCommand.Target,
                    operationResult.BeforeCardDataId,
                    operationResult.AfterCardDataId,
                    operationResult.TransformKey,
                    CardFormChangeCause.OverrideApplied);
                var formChangedContext = context with
                {
                    Triggered = new CardTrigger(applyCommand.Target),
                    Action = new CardFormChangedAction(source)
                };
                events.Add(new CardFormChangedEvent(
                    applyCommand.Target.Identity,
                    operationResult.BeforeCardDataId,
                    operationResult.AfterCardDataId,
                    operationResult.TransformKey,
                    CardFormChangeCause.OverrideApplied,
                    CardInfo.Create(applyCommand.Target, formChangedContext)));
            }

            return new CommandApplyResult(
                resultAction.WrapAsEnumerable(),
                events);
        }
    }
}
