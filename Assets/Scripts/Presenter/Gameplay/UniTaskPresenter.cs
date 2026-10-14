using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;

namespace MortalGame.Presenter
{
    public interface IUniTaskPresenter
    {
        public abstract record Event();
        public record None() : Event;
        public record Halt() : Event;

        UniTask Run(
            IDisposable disposables,
            Func<bool> conditionFunc,
            CancellationToken cancellationToken,
            Func<Event, UniTask<Event>> eventTaskHandler,
            UniTask<Event> firstTask = default);
        void TryEnqueueNextEvent(Event task);
    }

    public class UniTaskPresenter : IUniTaskPresenter
    {
        private Option<UniTask<IUniTaskPresenter.Event>> _currentTask = Option.None<UniTask<IUniTaskPresenter.Event>>();
        private Func<IUniTaskPresenter.Event, UniTask<IUniTaskPresenter.Event>> _eventTaskHandler;

        public async UniTask Run(
            IDisposable disposables,
            Func<bool> conditionFunc,
            CancellationToken cancellationToken,
            Func<IUniTaskPresenter.Event, UniTask<IUniTaskPresenter.Event>> eventTaskHandler,
            UniTask<IUniTaskPresenter.Event> firstTask = default)
        {
            _eventTaskHandler = eventTaskHandler;
            using (disposables)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _currentTask = Option.Some(firstTask);
                    if (await firstTask is IUniTaskPresenter.Halt) return;
                    cancellationToken.ThrowIfCancellationRequested();
                    _currentTask = Option.None<UniTask<IUniTaskPresenter.Event>>();

                    while (conditionFunc())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (_TryGetCurrentTask(out var task))
                        {
                            // 等待子面板期間保留目前工作，避免底層按鈕插入其他事件。
                            var evt = await task;
                            _currentTask = Option.None<UniTask<IUniTaskPresenter.Event>>();
                            if (evt is IUniTaskPresenter.Halt) break;
                        }
                        else
                            await UniTask.NextFrame(cancellationToken);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    _eventTaskHandler = null;
                    _currentTask = Option.None<UniTask<IUniTaskPresenter.Event>>();
                }
            }
        }

        private bool _TryGetCurrentTask(out UniTask<IUniTaskPresenter.Event> task)
        {
            if (_currentTask.HasValue)
            {
                task = _currentTask.ValueOr(UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.None()));
                return true;
            }

            task = UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.None());
            return false;
        }

        public void TryEnqueueNextEvent(IUniTaskPresenter.Event nextEvent)
        {
            if (_eventTaskHandler == null || _currentTask.HasValue) return;

            // 先佔用工作位置，事件處理同步開啟子面板時也不會重入。
            _currentTask = Option.Some(UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.None()));
            try
            {
                var task = _eventTaskHandler(nextEvent);
                if (task.Status == UniTaskStatus.Succeeded)
                {
                    var evt = task.GetAwaiter().GetResult();
                    _currentTask = evt is IUniTaskPresenter.Halt
                        ? Option.Some(UniTask.FromResult(evt))
                        : Option.None<UniTask<IUniTaskPresenter.Event>>();
                }
                else
                    _currentTask = Option.Some(task);
            }
            catch
            {
                _currentTask = Option.None<UniTask<IUniTaskPresenter.Event>>();
                throw;
            }
        }
    }
}
