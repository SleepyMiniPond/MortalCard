using System.Collections.Generic;

public sealed class EffectQueueRunner
{
    private readonly Queue<EffectQueueItem> _items = new();

    public void EnqueueCardEffect(TriggerContext context, ICardEffect effect)
    {
        _items.Enqueue(new CardEffectQueueItem(context, effect));
    }

    public void EnqueuePlayerBuffEffect(TriggerContext context, IPlayerBuffEffect effect)
    {
        _items.Enqueue(new PlayerBuffEffectQueueItem(context, effect));
    }

    public void EnqueueCharacterBuffEffect(TriggerContext context, ICharacterBuffEffect effect)
    {
        _items.Enqueue(new CharacterBuffEffectQueueItem(context, effect));
    }

    public void EnqueueCardBuffEffect(TriggerContext context, ICardBuffEffect effect)
    {
        _items.Enqueue(new CardBuffEffectQueueItem(context, effect));
    }

    public EffectResult RunToCompletion()
    {
        var actions = new List<BaseResultAction>();
        var events = new List<IGameEvent>();

        while (_items.TryDequeue(out var item))
        {
            var result = item.Execute();
            actions.AddRange(result.Actions);
            events.AddRange(result.Events);
        }

        return new EffectResult(actions, events);
    }
}

public abstract record EffectQueueItem(TriggerContext Context)
{
    public abstract EffectResult Execute();
}

public sealed record CardEffectQueueItem(
    TriggerContext Context,
    ICardEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute()
    {
        var commands = EffectDataResolver.ResolveCardEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}

public sealed record PlayerBuffEffectQueueItem(
    TriggerContext Context,
    IPlayerBuffEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute()
    {
        var commands = EffectDataResolver.ResolvePlayerBuffEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}

public sealed record CharacterBuffEffectQueueItem(
    TriggerContext Context,
    ICharacterBuffEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute()
    {
        var commands = EffectDataResolver.ResolveCharacterBuffEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}

public sealed record CardBuffEffectQueueItem(
    TriggerContext Context,
    ICardBuffEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute()
    {
        var commands = EffectDataResolver.ResolveCardBuffEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}
