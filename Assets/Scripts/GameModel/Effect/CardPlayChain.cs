using System;
using System.Collections.Generic;
using System.Threading;

namespace MortalGame.GameModel
{
    public sealed record CardPlayRequest(
        Guid CardIdentity,
        Guid OwnerIdentity,
        IActionSource RequestedBy);

    internal sealed class CardPlayChain
    {
        private readonly Queue<CardPlayRequest> _pending = new();
        private readonly CancellationToken _cancellationToken;
        private readonly EffectQueueExecutionScope _scope;
        private readonly List<IGameEvent> _events = new();

        internal IReadOnlyList<IGameEvent> Events => _events;
        internal int PendingCount => _pending.Count;
        internal int ProcessedItemCount => _scope.ProcessedItemCount;

        internal CardPlayChain(int budget, CancellationToken cancellationToken)
        {
            _scope = new EffectQueueExecutionScope(budget);
            _cancellationToken = cancellationToken;
        }

        internal void Enqueue(CardPlayRequest request) => _pending.Enqueue(request);
        internal void Record(IGameEvent gameEvent) => _events.Add(gameEvent);
        internal void Record(IEnumerable<IGameEvent> events) => _events.AddRange(events);

        internal EffectResult RunEffects(IEnumerable<EffectQueueItem> items)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var runner = new EffectQueueRunner(_scope, Record, _cancellationToken);
            runner.EnqueueRange(items);
            var result = runner.RunToCompletion();
            if (_scope.IsHalted)
                throw new CardPlayChainHaltedException(result);
            return result;
        }

        internal void BeginStep(string description)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (!_scope.TryBeginItem(new[] { description }))
                throw new CardPlayChainHaltedException(EffectResult.Empty);
        }

        internal bool TryDequeue(out CardPlayRequest request)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return _pending.TryDequeue(out request);
        }

        internal void Clear() => _pending.Clear();
    }

    internal sealed class CardPlayChainHaltedException : Exception
    {
        internal EffectResult PartialResult { get; }
        internal CardPlayChainHaltedException(EffectResult partialResult)
            : base("出牌鏈執行預算已耗盡。") => PartialResult = partialResult;
    }
}
