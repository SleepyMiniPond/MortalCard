using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using Optional;

namespace MortalGame.GameModel
{
    internal sealed record SelfTransformQueueItem(
        GameplayManager Manager,
        ICardEntity Card,
        UpdateTimingAction TimingAction) : EffectQueueItem((TriggerContext)null)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            if (Card.OverrideFormState.HasValue)
            {
                return EffectResult.Empty;
            }

            var ruleContext = new CardFormRuleContext(
                Manager,
                Card,
                TimingAction);
            var operation = CardTransformRuleEvaluator.Evaluate(
                TimingAction.Timing,
                ruleContext);
            if (!operation.TryGetValue(out var formOperation))
            {
                return EffectResult.Empty;
            }

            var operationResult = formOperation switch
            {
                ApplyCardFormOperation apply => Card.TryApplySelfForm(
                    apply.TransformKey,
                    apply.TargetCardDataId,
                    apply.Persistence),
                RevertCardFormOperation revert => Card.TryRevertSelfForm(
                    revert.TransformKey),
                _ => throw new InvalidOperationException(
                    $"不支援的卡片形態操作：{formOperation.GetType().Name}")
            };

            if (!operationResult.IsSuccess)
            {
                return EffectResult.Empty;
            }

            var cause = operationResult.Status switch
            {
                CardFormOperationStatus.Applied => CardFormChangeCause.SelfTransformApplied,
                CardFormOperationStatus.Reverted => CardFormChangeCause.SelfTransformReverted,
                _ => throw new InvalidOperationException(
                    $"成功的形態操作具有無效狀態：{operationResult.Status}")
            };
            var source = new CardFormChangedSource(
                Card,
                operationResult.BeforeCardDataId,
                operationResult.AfterCardDataId,
                operationResult.TransformKey,
                cause);
            var formChangedContext = new TriggerContext(
                Manager,
                new CardTrigger(Card),
                TimingAction) with
            {
                Action = new CardFormChangedAction(source)
            };
            var cardInfo = CardInfo.Create(Card, formChangedContext);

            if (Card.TriggeredEffects.TryGetValue(
                    CardTriggeredTiming.FormChanged,
                    out var triggeredEffects))
            {
                queue.EnqueueImmediate(triggeredEffects
                    .Where(conditionalEffect => conditionalEffect != null &&
                        (conditionalEffect.Conditions ?? new List<ICondition>()).All(
                            condition => condition != null && condition.Eval(formChangedContext)))
                    .Where(conditionalEffect => conditionalEffect.Effect != null)
                    .Select(conditionalEffect =>
                    new CardTriggeredEffectQueueItem(
                        formChangedContext,
                        conditionalEffect.Effect)));
            }

            var formChangedEvent = new CardFormChangedEvent(
                Card.Identity,
                operationResult.BeforeCardDataId,
                operationResult.AfterCardDataId,
                operationResult.TransformKey,
                cause,
                cardInfo);
            return new EffectResult(
                Array.Empty<BaseResultAction>(),
                new IGameEvent[] { formChangedEvent });
        }

    }

    internal sealed record RemoveCardFormOverrideQueueItem(
        GameplayManager Manager,
        ICardEntity Card,
        CardFormOverrideState State,
        UpdateTimingAction TimingAction) : EffectQueueItem((TriggerContext)null)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var operationResult = Card.TryRemoveOverrideForm(State.Identity);
            if (!operationResult.IsSuccess ||
                operationResult.BeforeCardDataId == operationResult.AfterCardDataId)
            {
                return EffectResult.Empty;
            }

            var source = new CardFormChangedSource(
                Card,
                operationResult.BeforeCardDataId,
                operationResult.AfterCardDataId,
                operationResult.TransformKey,
                CardFormChangeCause.OverrideRemoved);
            var formChangedContext = new TriggerContext(
                Manager,
                new CardTrigger(Card),
                TimingAction) with
            {
                Action = new CardFormChangedAction(source)
            };
            var cardInfo = CardInfo.Create(Card, formChangedContext);

            if (Card.TriggeredEffects.TryGetValue(
                    CardTriggeredTiming.FormChanged,
                    out var triggeredEffects))
            {
                queue.EnqueueImmediate(triggeredEffects
                    .Where(conditionalEffect => conditionalEffect != null &&
                        (conditionalEffect.Conditions ?? new List<ICondition>()).All(
                            condition => condition != null && condition.Eval(formChangedContext)))
                    .Where(conditionalEffect => conditionalEffect.Effect != null)
                    .Select(conditionalEffect =>
                    new CardTriggeredEffectQueueItem(
                        formChangedContext,
                        conditionalEffect.Effect)));
            }

            return new EffectResult(
                Array.Empty<BaseResultAction>(),
                new IGameEvent[]
                {
                    new CardFormChangedEvent(
                        Card.Identity,
                        operationResult.BeforeCardDataId,
                        operationResult.AfterCardDataId,
                        operationResult.TransformKey,
                        CardFormChangeCause.OverrideRemoved,
                        cardInfo)
                });
        }
    }

    internal sealed record CardTriggeredEffectQueueItem(
        TriggerContext Context,
        ICardEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var commands = EffectDataResolver.ResolveCardEffect(Context, Effect);
            return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
        }
    }
}
