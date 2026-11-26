using System;
using UnityEngine;

namespace MortalGame.Presentation.Abstractions
{
    public interface IHandCardInputSession : IDisposable
    {
        IObservable<Guid> PointerEntered { get; }
        IObservable<Guid> PointerExited { get; }
        IObservable<(Guid Identity, Vector2 Position)> DragStarted { get; }
        IObservable<(Guid Identity, Vector2 Position)> Dragged { get; }
        IObservable<(Guid Identity, Vector2 Position)> DragEnded { get; }
    }
}
