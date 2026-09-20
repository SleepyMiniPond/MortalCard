using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MortalGame.GameModel
{

    public interface IEffectQueueContext
    {
        int ProcessedItemCount { get; }
        void Enqueue(EffectQueueItem item);
        void EnqueueImmediateCommands(TriggerContext context, EffectCommandSet commands);
        void EnqueueImmediate(EffectQueueItem item);
        void EnqueueImmediate(IEnumerable<EffectQueueItem> items);
    }

    internal sealed class EffectQueueExecutionScope
    {
        public Guid CorrelationId { get; }
        public int Budget { get; }
        public int ProcessedItemCount { get; private set; }
        public bool IsHalted { get; private set; }
        public EffectQueueHaltDiagnostic HaltDiagnostic { get; private set; }

        public EffectQueueExecutionScope(int budget)
        {
            if (budget <= 0)
                throw new ArgumentOutOfRangeException(nameof(budget));

            Budget = budget;
            CorrelationId = Guid.NewGuid();
        }

        internal bool TryBeginItem(IReadOnlyList<string> triggerPath)
        {
            if (IsHalted)
                return false;

            if (ProcessedItemCount >= Budget)
            {
                IsHalted = true;
                HaltDiagnostic = new EffectQueueHaltDiagnostic(
                    CorrelationId,
                    Budget,
                    ProcessedItemCount,
                    triggerPath.ToArray());
                EffectQueueDiagnosticLogger.LogBudgetExceeded(HaltDiagnostic);
                return false;
            }

            ProcessedItemCount++;
            return true;
        }
    }

    public sealed class EffectQueueRunner : IEffectQueueContext
    {
        public const int BUDGET_COUNT = 1000;

        private sealed record PendingEffectQueueItem(
            EffectQueueItem Item,
            IReadOnlyList<string> TriggerPath);

        private readonly LinkedList<PendingEffectQueueItem> _items = new();
        private readonly EffectQueueExecutionScope _executionScope;
        private readonly Action<IEnumerable<IGameEvent>> _recordEvents;
        private readonly CancellationToken _cancellationToken;
        private IReadOnlyList<string> _currentTriggerPath = Array.Empty<string>();

        public bool IsHalted => _executionScope.IsHalted;
        public int ProcessedItemCount => _executionScope.ProcessedItemCount;
        public int PendingItemCount => _items.Count;
        public EffectQueueHaltDiagnostic HaltDiagnostic => _executionScope.HaltDiagnostic;

        public EffectQueueRunner()
            : this(new EffectQueueExecutionScope(BUDGET_COUNT), _ => { }, CancellationToken.None)
        {
        }

        internal EffectQueueRunner(
            EffectQueueExecutionScope executionScope,
            Action<IEnumerable<IGameEvent>> recordEvents,
            CancellationToken cancellationToken)
        {
            _executionScope = executionScope;
            _recordEvents = recordEvents;
            _cancellationToken = cancellationToken;
        }

        public static EffectResult RunToCompletion(IEnumerable<EffectQueueItem> items)
        {
            var runner = new EffectQueueRunner();
            runner.EnqueueRange(items);
            return runner.RunToCompletion();
        }

        public void Enqueue(EffectQueueItem item)
        {
            _items.AddLast(CreatePendingItem(item));
        }

        public void EnqueueRange(IEnumerable<EffectQueueItem> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
                Enqueue(item);
        }

        public void EnqueueCommands(TriggerContext context, EffectCommandSet commands)
        {
            EnqueueRange(CreateCommandQueueItems(context, commands));
        }

        public void EnqueueImmediateCommands(TriggerContext context, EffectCommandSet commands)
        {
            EnqueueImmediate(CreateCommandQueueItems(context, commands));
        }

        public void EnqueueImmediate(EffectQueueItem item)
        {
            _items.AddFirst(CreatePendingItem(item));
        }

        public void EnqueueImmediate(IEnumerable<EffectQueueItem> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var bufferedItems = new List<EffectQueueItem>(items);
            for (var i = bufferedItems.Count - 1; i >= 0; i--)
            {
                _items.AddFirst(CreatePendingItem(bufferedItems[i]));
            }
        }

        public EffectResult RunToCompletion()
        {
            var actions = new List<BaseResultAction>();
            var events = new List<IGameEvent>();

            while (_items.Count > 0)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var pendingItem = _items.First.Value;
                if (!_executionScope.TryBeginItem(pendingItem.TriggerPath))
                    break;

                _items.RemoveFirst();
                var previousTriggerPath = _currentTriggerPath;
                _currentTriggerPath = pendingItem.TriggerPath;
                try
                {
                    var result = pendingItem.Item.Execute(this);
                    actions.AddRange(result.Actions);
                    events.AddRange(result.Events);
                    _recordEvents(result.Events);
                }
                finally
                {
                    _currentTriggerPath = previousTriggerPath;
                }
            }

            return new EffectResult(actions, events);
        }

        private PendingEffectQueueItem CreatePendingItem(EffectQueueItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var triggerPath = _currentTriggerPath
                .Concat(new[] { item.GetType().Name })
                .ToArray();
            return new PendingEffectQueueItem(item, triggerPath);
        }

        private static IReadOnlyList<EffectQueueItem> CreateCommandQueueItems(
            TriggerContext context,
            EffectCommandSet commands)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            return commands.Commands
                .Select(command => (EffectQueueItem)new EffectCommandQueueItem(context, command))
                .ToArray();
        }
    }

    public abstract record EffectQueueItem(TriggerContext Context)
    {
        public abstract EffectResult Execute(IEffectQueueContext queue);
    }

    internal sealed record EffectCommandQueueItem(
        TriggerContext Context,
        IEffectCommand Command) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            return EffectCommandExecutor.ApplyEffectCommand(Context, Command, queue);
        }
    }

    public sealed record CardEffectQueueItem(
        TriggerContext Context,
        ICardEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var commands = EffectDataResolver.ResolveCardEffect(Context, Effect);
            queue.EnqueueImmediateCommands(Context, commands);
            return EffectResult.Empty;
        }
    }

    public sealed record PlayerBuffEffectQueueItem(
        TriggerContext Context,
        IPlayerBuffEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var commands = EffectDataResolver.ResolvePlayerBuffEffect(Context, Effect);
            queue.EnqueueImmediateCommands(Context, commands);
            return EffectResult.Empty;
        }
    }

    public sealed record CharacterBuffEffectQueueItem(
        TriggerContext Context,
        ICharacterBuffEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var commands = EffectDataResolver.ResolveCharacterBuffEffect(Context, Effect);
            queue.EnqueueImmediateCommands(Context, commands);
            return EffectResult.Empty;
        }
    }

    public sealed record CardBuffEffectQueueItem(
        TriggerContext Context,
        ICardBuffEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var commands = EffectDataResolver.ResolveCardBuffEffect(Context, Effect);
            queue.EnqueueImmediateCommands(Context, commands);
            return EffectResult.Empty;
        }
    }

}
