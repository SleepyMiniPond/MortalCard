using UnityEngine;
using MortalGame.GameModel;

namespace MortalGame.Presentation.Abstractions
{

public interface ISelectableView : ISelectionTarget
{
    RectTransform RectTransform { get; }

    void OnSelect();
    void OnDeselect();
}
}
