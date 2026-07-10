using System;
using UnityEngine;
using MortalGame.GameView;

namespace MortalGame.GameView
{

public class PlaygroundView : MonoBehaviour, ISelectableView
{
    public RectTransform RectTransform => transform.GetComponent<RectTransform>();
    public TargetType TargetType => TargetType.None;
    public Guid TargetIdentity => Guid.Empty;

    public void OnSelect()
    {
    }
    public void OnDeselect()
    {
    }
}
}
