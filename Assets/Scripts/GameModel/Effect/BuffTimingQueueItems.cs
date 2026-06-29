using System;
using System.Collections.Generic;
using Optional;

internal sealed record TriggerTimingQueueItem(
    GameplayManager Manager,
    GameTiming Timing,
    IActionSource Source) : EffectQueueItem((TriggerContext)null)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        var items = Manager.CreateTriggerTimingQueueItems(Timing, Source);
        for (var i = items.Count - 1; i >= 0; i--)
        {
            queue.EnqueueImmediate(items[i]);
        }

        return new EffectResult(Array.Empty<BaseResultAction>(), Array.Empty<IGameEvent>());
    }
}

internal sealed record TriggeredPlayerBuffEffectQueueItem(
    GameplayManager Manager,
    TriggerContext Context,
    IPlayerBuffEffect Effect,
    IActionSource TriggerBuffSource) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        var events = new List<IGameEvent>();
        events.AddRange(Manager.UpdateReactorSessionAction(
            new UpdateTimingAction(GameTiming.TriggerBuffStart, Context.Action.Source)));

        var commands = EffectDataResolver.ResolvePlayerBuffEffect(Context, Effect);
        var effectResult = EffectCommandExecutor.ApplyEffectCommands(Context, commands);
        events.AddRange(effectResult.Events);

        queue.EnqueueImmediate(new TriggerTimingQueueItem(Manager, GameTiming.TriggerBuffEnd, TriggerBuffSource));
        return new EffectResult(effectResult.Actions, events);
    }
}

internal sealed record TriggeredCharacterBuffEffectQueueItem(
    GameplayManager Manager,
    ICharacterEntity SelectedCharacter,
    TriggerContext Context,
    ICharacterBuffEffect Effect,
    IActionSource TriggerBuffSource) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        using var characterContext = Manager.EffectQueueContextManager.SetSelectedCharacter(SelectedCharacter.Some());
        var events = new List<IGameEvent>();
        events.AddRange(Manager.UpdateReactorSessionAction(
            new UpdateTimingAction(GameTiming.TriggerBuffStart, Context.Action.Source)));

        var commands = EffectDataResolver.ResolveCharacterBuffEffect(Context, Effect);
        var effectResult = EffectCommandExecutor.ApplyEffectCommands(Context, commands);
        events.AddRange(effectResult.Events);

        queue.EnqueueImmediate(new TriggerTimingQueueItem(Manager, GameTiming.TriggerBuffEnd, TriggerBuffSource));
        return new EffectResult(effectResult.Actions, events);
    }
}

internal sealed record TriggeredCardBuffEffectQueueItem(
    GameplayManager Manager,
    ICardEntity SelectedCard,
    TriggerContext Context,
    ICardBuffEffect Effect,
    IActionSource TriggerBuffSource) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        using var cardContext = Manager.EffectQueueContextManager.SetSelectedCard(SelectedCard.Some());
        var events = new List<IGameEvent>();
        events.AddRange(Manager.UpdateReactorSessionAction(
            new UpdateTimingAction(GameTiming.TriggerBuffStart, Context.Action.Source)));

        var commands = EffectDataResolver.ResolveCardBuffEffect(Context, Effect);
        var effectResult = EffectCommandExecutor.ApplyEffectCommands(Context, commands);
        events.AddRange(effectResult.Events);

        queue.EnqueueImmediate(new TriggerTimingQueueItem(Manager, GameTiming.TriggerBuffEnd, TriggerBuffSource));
        return new EffectResult(effectResult.Actions, events);
    }
}
