using System;
using MortalGame.GameModel;
using MortalGame.GameData;
using System.Collections.Generic;

namespace MortalGame.GameModel
{

public interface IEffectQueueContext
{
    int ProcessedItemCount { get; }
    void Enqueue(EffectQueueItem item);
    void EnqueueImmediate(EffectQueueItem item);
}

public sealed class EffectQueueRunner : IEffectQueueContext
{
    private readonly LinkedList<EffectQueueItem> _items = new();
    private readonly int _maxProcessedItemCount;

    public bool IsHalted { get; private set; }
    public int ProcessedItemCount { get; private set; }
    public int PendingItemCount => _items.Count;

    public EffectQueueRunner(int maxProcessedItemCount = 1000)
    {
        if (maxProcessedItemCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxProcessedItemCount));

        _maxProcessedItemCount = maxProcessedItemCount;
    }

    public void Enqueue(EffectQueueItem item)
    {
        _items.AddLast(item);
    }

    public void EnqueueImmediate(EffectQueueItem item)
    {
        _items.AddFirst(item);
    }

    public void EnqueueCardEffect(TriggerContext context, ICardEffect effect)
    {
        Enqueue(new CardEffectQueueItem(context, effect));
    }

    public void EnqueuePlayerBuffEffect(TriggerContext context, IPlayerBuffEffect effect)
    {
        Enqueue(new PlayerBuffEffectQueueItem(context, effect));
    }

    public void EnqueueCharacterBuffEffect(TriggerContext context, ICharacterBuffEffect effect)
    {
        Enqueue(new CharacterBuffEffectQueueItem(context, effect));
    }

    public void EnqueueCardBuffEffect(TriggerContext context, ICardBuffEffect effect)
    {
        Enqueue(new CardBuffEffectQueueItem(context, effect));
    }

    public EffectResult RunToCompletion()
    {
        var actions = new List<BaseResultAction>();
        var events = new List<IGameEvent>();

        IsHalted = false;
        ProcessedItemCount = 0;
        while (_items.Count > 0)
        {
            if (ProcessedItemCount >= _maxProcessedItemCount)
            {
                IsHalted = true;
                break;
            }

            var item = _items.First.Value;
            _items.RemoveFirst();
            ProcessedItemCount++;
            var result = item.Execute(this);
            actions.AddRange(result.Actions);
            events.AddRange(result.Events);
        }

        return new EffectResult(actions, events);
    }
}

public abstract record EffectQueueItem(TriggerContext Context)
{
    public abstract EffectResult Execute(IEffectQueueContext queue);
}

public sealed record CardEffectQueueItem(
    TriggerContext Context,
    ICardEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        var commands = EffectDataResolver.ResolveCardEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}

public sealed record PlayerBuffEffectQueueItem(
    TriggerContext Context,
    IPlayerBuffEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        var commands = EffectDataResolver.ResolvePlayerBuffEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}

public sealed record CharacterBuffEffectQueueItem(
    TriggerContext Context,
    ICharacterBuffEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        var commands = EffectDataResolver.ResolveCharacterBuffEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}

public sealed record CardBuffEffectQueueItem(
    TriggerContext Context,
    ICardBuffEffect Effect) : EffectQueueItem(Context)
{
    public override EffectResult Execute(IEffectQueueContext queue)
    {
        var commands = EffectDataResolver.ResolveCardBuffEffect(Context, Effect);
        return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
    }
}

}
