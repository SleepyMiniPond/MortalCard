using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

public class AllyCharacterView : BaseCharacterView, ISelectableView
{
    [SerializeField]
    private RectTransform _rectTransform;

    public RectTransform RectTransform => _rectTransform;
    public TargetType TargetType => TargetType.AllyCharacter;
    public Guid TargetIdentity => _playerIdentity;

    private Guid _playerIdentity;

    public void Init(IGameplayModel statusWatcher) 
    {
        _statusWatcher = statusWatcher;
        _timmer = 0;
    }

    public void SummonAlly(AllySummonEvent allySummonEvent)
    {
        Debug.Log($"Summon Ally: {allySummonEvent.Player.MainCharacter.NameKey}");
        _playerIdentity = allySummonEvent.Player.MainCharacter.Identity;

        _Run().Forget();
    }
    
    public void OnSelect()
    {
    }

    public void OnDeselect()
    {
    }
}
