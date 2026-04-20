using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class BaseCharacterView : MonoBehaviour
{
    [BoxGroup("EventView")]
    [SerializeField]
    protected DamageEventViewFactory _damageEventViewFactory;
    [BoxGroup("EventView")]
    [SerializeField]
    protected HealEventViewFactory _healEventViewFactory;
    [BoxGroup("EventView")]
    [SerializeField]
    protected ShieldEventViewFactory _shieldEventViewFactory;
    [BoxGroup("EventView")]
    [SerializeField]
    protected GainEnergyEventViewFactory _gainEnergyEventViewFactory;
    [BoxGroup("EventView")]
    [SerializeField]
    protected LoseEnergyEventViewFactory _loseEnergyEventViewFactory;
    [BoxGroup("EventView")]
    [SerializeField]
    protected IncreaseDispositionEventViewFactory _increaseDispositionEventViewFactory;
    [BoxGroup("EventView")]
    [SerializeField]
    protected DecreaseDispositionEventViewFactory _decreaseDispositionEventViewFactory;
    [BoxGroup("EventView")]
    [SerializeField]
    protected Transform _eventViewParent;
    [BoxGroup("EventView")]
    [SerializeField]
    protected float _minTimeInterval;

    protected float _timmer = 0;
    protected Queue<IAnimationNumberEvent> _animationEventBuffer;
    protected IGameplayModel _statusWatcher;

    public void UpdateHealth(IAnimationNumberEvent healthEvent)
    {
        _animationEventBuffer.Enqueue(healthEvent);
    }
    public void UpdateEnergy(IAnimationNumberEvent energyEvent)
    {
        _animationEventBuffer.Enqueue(energyEvent);
    }
    public void UpdateDisposition(IAnimationNumberEvent dispositionEvent)
    {
        _animationEventBuffer.Enqueue(dispositionEvent);
    }

    protected async UniTaskVoid _Run(CancellationToken cancellationToken = default)
    {
        _animationEventBuffer = new Queue<IAnimationNumberEvent>();
        while (!cancellationToken.IsCancellationRequested)
        {
            _timmer += Time.deltaTime;
            if (_timmer >= _minTimeInterval)
            {
                _timmer -= _minTimeInterval;
                if (_animationEventBuffer.TryDequeue(out var animationEvent))
                {
                    _PlayHealthEventAnimation(animationEvent).Forget();
                }
            }

            await UniTask.NextFrame();
        }
        _animationEventBuffer.Clear();
    }

    protected async UniTaskVoid _PlayHealthEventAnimation(IAnimationNumberEvent animationEvent)
    {
        switch (animationEvent)
        {
            case DamageEvent takeDamageEvent:
                var damageEventView = _damageEventViewFactory.CreatePrefab();
                damageEventView.SetEventInfo(takeDamageEvent, _eventViewParent);

                await damageEventView.PlayAnimation();
                _damageEventViewFactory.RecyclePrefab(damageEventView);
                break;

            case GetHealEvent getHealEvent:
                var healEventView = _healEventViewFactory.CreatePrefab();
                healEventView.SetEventInfo(getHealEvent, _eventViewParent);
                
                await healEventView.PlayAnimation();
                _healEventViewFactory.RecyclePrefab(healEventView);
                break;

            case GetShieldEvent getShieldEvent:
                var shieldEventView = _shieldEventViewFactory.CreatePrefab();
                shieldEventView.SetEventInfo(getShieldEvent, _eventViewParent);
                
                await shieldEventView.PlayAnimation();
                _shieldEventViewFactory.RecyclePrefab(shieldEventView);
                break;

            case GainEnergyEvent gainEnergyEvent:
                var gainEnergyEventView = _gainEnergyEventViewFactory.CreatePrefab();
                gainEnergyEventView.SetEventInfo(gainEnergyEvent, _eventViewParent);

                await gainEnergyEventView.PlayAnimation();
                _gainEnergyEventViewFactory.RecyclePrefab(gainEnergyEventView);
                break;

            case LoseEnergyEvent loseEnergyEvent:
                var loseEnergyEventView = _loseEnergyEventViewFactory.CreatePrefab();
                loseEnergyEventView.SetEventInfo(loseEnergyEvent, _eventViewParent);

                await loseEnergyEventView.PlayAnimation();
                _loseEnergyEventViewFactory.RecyclePrefab(loseEnergyEventView);
                break;
            
            case IncreaseDispositionEvent increaseDispositionEvent:
                var increaseDispositionEventView = _increaseDispositionEventViewFactory.CreatePrefab();
                increaseDispositionEventView.SetEventInfo(increaseDispositionEvent, _eventViewParent);

                await increaseDispositionEventView.PlayAnimation();
                _increaseDispositionEventViewFactory.RecyclePrefab(increaseDispositionEventView);
                break;
                
            case DecreaseDispositionEvent decreaseDispositionEvent:
                var decreaseDispositionEventView = _decreaseDispositionEventViewFactory.CreatePrefab();
                decreaseDispositionEventView.SetEventInfo(decreaseDispositionEvent, _eventViewParent);

                await decreaseDispositionEventView.PlayAnimation();
                _decreaseDispositionEventViewFactory.RecyclePrefab(decreaseDispositionEventView);
                break;
        }        
    }
}
