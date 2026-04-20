using System;
using UnityEngine;

public interface ISelectionTarget
{
    TargetType TargetType { get; }
    Guid TargetIdentity { get; }
}

public interface ISelectableView : ISelectionTarget
{
    RectTransform RectTransform { get; }

    void OnSelect();
    void OnDeselect();
}
