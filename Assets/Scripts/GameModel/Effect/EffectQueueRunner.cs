using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Optional;

namespace MortalGame.GameModel
{
    public enum EffectQueuePosition
    {
        Tail,
        Immediate
    }

    public interface IEffectQueueContext
    {
        int ProcessedItemCount { get; }
        void Enqueue(
            IEnumerable<EffectQueueItem> items, 
            EffectQueuePosition position = EffectQueuePosition.Tail,
            Option<GameContext> selection = default);
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
            IReadOnlyList<string> TriggerPath,
            Option<GameContext> Selection);

        private readonly LinkedList<PendingEffectQueueItem> _items = new();
        private readonly IGameContextManager _contextManager;
        private readonly EffectQueueExecutionScope _executionScope;
        private readonly Action<IEnumerable<IGameEvent>> _recordEvents;
        private readonly CancellationToken _cancellationToken;
        private IReadOnlyList<string> _currentTriggerPath = Array.Empty<string>();
        private Option<GameContext> _currentSelection = Option.None<GameContext>();

        public bool IsHalted => _executionScope.IsHalted;
        public int ProcessedItemCount => _executionScope.ProcessedItemCount;
        public int PendingItemCount => _items.Count;
        public EffectQueueHaltDiagnostic HaltDiagnostic => _executionScope.HaltDiagnostic;

        public EffectQueueRunner(IGameContextManager contextManager)
            : this(contextManager, new EffectQueueExecutionScope(BUDGET_COUNT), _ => { }, CancellationToken.None)
        {
        }

        internal EffectQueueRunner(
            IGameContextManager contextManager,
            EffectQueueExecutionScope executionScope,
            Action<IEnumerable<IGameEvent>> recordEvents,
            CancellationToken cancellationToken)
        {
            _contextManager = contextManager;
            _executionScope = executionScope;
            _recordEvents = recordEvents;
            _cancellationToken = cancellationToken;
        }

        public static EffectResult RunToCompletion(IGameContextManager contextManager, IEnumerable<EffectQueueItem> items)
        {
            var runner = new EffectQueueRunner(contextManager);
            runner.Enqueue(items);
            return runner.RunToCompletion();
        }

        public void Enqueue(IEnumerable<EffectQueueItem> items,
            EffectQueuePosition position = EffectQueuePosition.Tail, Option<GameContext> selection = default)
        {
            var effectiveSelection = selection.HasValue ? selection : _currentSelection;
            var bufferedItems = items
                .Select(CreatePendingItem)
                .ToArray();

            if (position == EffectQueuePosition.Tail)
            {
                foreach (var item in bufferedItems)
                    _items.AddLast(item);
            }
            else
            {
                for (var i = bufferedItems.Length - 1; i >= 0; i--)
                    _items.AddFirst(bufferedItems[i]);
            }

            PendingEffectQueueItem CreatePendingItem(EffectQueueItem item)
            {
                var triggerPath = _currentTriggerPath
                    .Append(item.GetType().Name)
                    .ToArray();
                return new PendingEffectQueueItem(item, triggerPath, effectiveSelection);
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
                var previousSelection = _currentSelection;
                _currentTriggerPath = pendingItem.TriggerPath;
                _currentSelection = pendingItem.Selection;
                try
                {
                    // 每個工作各自開關作用域；中止時不依賴尚未執行的清理工作。
                    using var selectionScope = _currentSelection
                        .Map(selection => _contextManager.SetContext(selection))
                        .ValueOr(() => null);
                    var result = pendingItem.Item.Execute(this);
                    actions.AddRange(result.Actions);
                    events.AddRange(result.Events);
                    _recordEvents(result.Events);
                }
                finally
                {
                    _currentTriggerPath = previousTriggerPath;
                    _currentSelection = previousSelection;
                }
            }

            return new EffectResult(actions, events);
        }

    }

    public static class EffectQueueContextExtensions
    {        
        public static void Enqueue(
            this IEffectQueueContext runner,
            EffectQueueItem item,
            EffectQueuePosition position = EffectQueuePosition.Tail,
            Option<GameContext> selection = default)
            => runner.Enqueue(new[] { item }, position, selection);

        public static void Enqueue(
            this IEffectQueueContext runner,
            TriggerContext context, 
            EffectCommandSet commands,
            EffectQueuePosition position = EffectQueuePosition.Tail, 
            Option<GameContext> selection = default)
        {
            var commandQueueItems = commands.Commands
                .Select(command => (EffectQueueItem)new EffectCommandQueueItem(context, command))
                .ToArray();
            runner.Enqueue(commandQueueItems, position, selection);
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
            queue.Enqueue(Context, commands, EffectQueuePosition.Immediate);
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
            queue.Enqueue(Context, commands, EffectQueuePosition.Immediate);
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
            queue.Enqueue(Context, commands, EffectQueuePosition.Immediate);
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
            queue.Enqueue(Context, commands, EffectQueuePosition.Immediate);
            return EffectResult.Empty;
        }
    }

}
