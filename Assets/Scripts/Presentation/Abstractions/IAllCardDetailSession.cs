using System;
using MortalGame.GameData;
using UniRx;

namespace MortalGame.Presentation.Abstractions
{
    public interface IAllCardDetailSession : IDisposable
    {
        IObservable<CardCollectionType> CollectionRequested { get; }
        IObservable<Guid> CardClicked { get; }
        IObservable<Unit> CloseRequested { get; }
    }
}
