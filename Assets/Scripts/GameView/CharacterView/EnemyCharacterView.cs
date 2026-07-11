using System;
using MortalGame.GameModel;
using MortalGame.GameData;
using System.Collections.Generic;
using UnityEngine;
using MortalGame.Presentation.Abstractions;

namespace MortalGame.GameView
{

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
        }

        public ICharacterAnimationLifetime SummonEnemy(EnemySummonEvent enemySummonEvent)
        {
            Debug.Log($"Summon Enemy: {enemySummonEvent.Enemy.MainCharacter.NameKey}");
            _playerIdentity = enemySummonEvent.Enemy.MainCharacter.Identity;

            return _StartAnimationWorker();
        }

        public void OnSelect()
        {
        }
        public void OnDeselect()
        {
        }
    }
}
