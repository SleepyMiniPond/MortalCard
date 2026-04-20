using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCharacterView : BaseCharacterView, ISelectableView
{
    [SerializeField]
    private RectTransform _rectTransform;

    public RectTransform RectTransform => _rectTransform;
    public TargetType TargetType => TargetType.EnemyCharacter;
    public Guid TargetIdentity => _playerIdentity;

    private Guid _playerIdentity;

    public void Init(IGameplayModel statusWatcher) 
    {
        _statusWatcher = statusWatcher;
        _timmer = 0;
    }

    public void SummonEnemy(EnemySummonEvent enemySummonEvent)
    {
        Debug.Log($"Summon Enemy: {enemySummonEvent.Enemy.MainCharacter.NameKey}");
        _playerIdentity = enemySummonEvent.Enemy.MainCharacter.Identity;

        _Run().Forget();
    }

    public void OnSelect()
    {
    }
    public void OnDeselect()
    {
    }
}
