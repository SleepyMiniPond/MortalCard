using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;
using UniRx;
using UnityEngine;

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

    public UniTaskPresenter()
    {
    }

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
            _currentTask = firstTask.SomeNotNull();

            while (conditionFunc() && !cancellationToken.IsCancellationRequested)
            {
                if (_TryPopOutNextTask(out var task))
                {
                    var evt = await task;
                    if (evt is IUniTaskPresenter.Halt)
                    {
                        break;
                    }
                }
                else
                {
                    await UniTask.NextFrame();
                }
            }
        }
    }

    private bool _TryPopOutNextTask(out UniTask<IUniTaskPresenter.Event> task)
    {
        if (_currentTask.HasValue)
        {
            task = _currentTask.ValueOr(UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.None()));
            _currentTask = Option.None<UniTask<IUniTaskPresenter.Event>>();
            return true;
        }

        task = UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.None());
        return false;
    }

    public void TryEnqueueNextEvent(IUniTaskPresenter.Event nextEvent)
    {
        if (!_currentTask.HasValue)
        {
            var task = _eventTaskHandler(nextEvent);
            _currentTask = Option.Some(task);
        }
    }
}
