using UnityEngine;
using MortalGame.GameModel;

namespace MortalGame.GameView
{

public interface ISelectableView : ISelectionTarget
{
    RectTransform RectTransform { get; }

    void OnSelect();
    void OnDeselect();
}
}
