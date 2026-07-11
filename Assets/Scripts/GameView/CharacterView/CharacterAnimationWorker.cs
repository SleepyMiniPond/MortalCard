using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using UnityEngine;

namespace MortalGame.GameView
{
    public interface ICharacterAnimationLifetime : IDisposable
    {
        UniTask Completion { get; }
        void Enqueue(IAnimationNumberEvent animationEvent);
    }

    public sealed class CharacterAnimationWorker : ICharacterAnimationLifetime
    {
        private readonly Queue<IAnimationNumberEvent> _pendingEvents = new();
        private readonly List<UniTask> _activeAnimations = new();
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Func<IAnimationNumberEvent, CancellationToken, UniTask> _playAnimation;
        private readonly float _minTimeInterval;

        private float _timer;
        private bool _isDisposed;

        public UniTask Completion { get; }

        public CharacterAnimationWorker(
            Func<IAnimationNumberEvent, CancellationToken, UniTask> playAnimation,
            float minTimeInterval)
        {
            _playAnimation = playAnimation;
            _minTimeInterval = minTimeInterval;
            Completion = _Run(_cancellation.Token).Preserve();
        }

        public void Enqueue(IAnimationNumberEvent animationEvent)
        {
            if (!_isDisposed)
            {
                _pendingEvents.Enqueue(animationEvent);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _cancellation.Cancel();
            _cancellation.Dispose();
        }

        private async UniTask _Run(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _ObserveCompletedAnimations();

                    _timer += Time.deltaTime;
                    if (_timer >= _minTimeInterval)
                    {
                        _timer -= _minTimeInterval;
                        if (_pendingEvents.TryDequeue(out var animationEvent))
                        {
                            _activeAnimations.Add(
                                _playAnimation(animationEvent, cancellationToken).Preserve());
                        }
                    }

                    await UniTask.NextFrame(cancellationToken);
                }
            }
            finally
            {
                if (!_isDisposed)
                {
                    _cancellation.Cancel();
                }

                _pendingEvents.Clear();
                await UniTask.WhenAll(
                    _activeAnimations.Select(task => task.SuppressCancellationThrow()));
                _activeAnimations.Clear();
            }
        }

        private async UniTask _ObserveCompletedAnimations()
        {
            for (var index = _activeAnimations.Count - 1; index >= 0; index--)
            {
                var animationTask = _activeAnimations[index];
                if (animationTask.Status == UniTaskStatus.Pending)
                {
                    continue;
                }

                _activeAnimations.RemoveAt(index);
                await animationTask;
            }
        }
    }
}
