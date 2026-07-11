using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using UnityEngine;

namespace MortalGame.GameView
{
    public interface ICharacterEventAnimationPlayer
    {
        UniTask Play(
            IAnimationNumberEvent animationEvent,
            CancellationToken cancellationToken);
    }

    public sealed class CharacterEventAnimationPlayer : ICharacterEventAnimationPlayer
    {
        private readonly DamageEventViewFactory _damageEventViewFactory;
        private readonly HealEventViewFactory _healEventViewFactory;
        private readonly ShieldEventViewFactory _shieldEventViewFactory;
        private readonly GainEnergyEventViewFactory _gainEnergyEventViewFactory;
        private readonly LoseEnergyEventViewFactory _loseEnergyEventViewFactory;
        private readonly IncreaseDispositionEventViewFactory _increaseDispositionEventViewFactory;
        private readonly DecreaseDispositionEventViewFactory _decreaseDispositionEventViewFactory;
        private readonly Transform _eventViewParent;

        public CharacterEventAnimationPlayer(
            DamageEventViewFactory damageEventViewFactory,
            HealEventViewFactory healEventViewFactory,
            ShieldEventViewFactory shieldEventViewFactory,
            GainEnergyEventViewFactory gainEnergyEventViewFactory,
            LoseEnergyEventViewFactory loseEnergyEventViewFactory,
            IncreaseDispositionEventViewFactory increaseDispositionEventViewFactory,
            DecreaseDispositionEventViewFactory decreaseDispositionEventViewFactory,
            Transform eventViewParent)
        {
            _damageEventViewFactory = damageEventViewFactory;
            _healEventViewFactory = healEventViewFactory;
            _shieldEventViewFactory = shieldEventViewFactory;
            _gainEnergyEventViewFactory = gainEnergyEventViewFactory;
            _loseEnergyEventViewFactory = loseEnergyEventViewFactory;
            _increaseDispositionEventViewFactory = increaseDispositionEventViewFactory;
            _decreaseDispositionEventViewFactory = decreaseDispositionEventViewFactory;
            _eventViewParent = eventViewParent;
        }

        public async UniTask Play(
            IAnimationNumberEvent animationEvent,
            CancellationToken cancellationToken)
        {
            switch (animationEvent)
            {
                case DamageEvent damageEvent:
                    var damageEventView = _damageEventViewFactory.CreatePrefab();
                    damageEventView.SetEventInfo(damageEvent, _eventViewParent);
                    await _PlayAndRecycle(
                        damageEventView,
                        _damageEventViewFactory,
                        cancellationToken);
                    break;

                case GetHealEvent healEvent:
                    var healEventView = _healEventViewFactory.CreatePrefab();
                    healEventView.SetEventInfo(healEvent, _eventViewParent);
                    await _PlayAndRecycle(
                        healEventView,
                        _healEventViewFactory,
                        cancellationToken);
                    break;

                case GetShieldEvent shieldEvent:
                    var shieldEventView = _shieldEventViewFactory.CreatePrefab();
                    shieldEventView.SetEventInfo(shieldEvent, _eventViewParent);
                    await _PlayAndRecycle(
                        shieldEventView,
                        _shieldEventViewFactory,
                        cancellationToken);
                    break;

                case GainEnergyEvent gainEnergyEvent:
                    var gainEnergyEventView = _gainEnergyEventViewFactory.CreatePrefab();
                    gainEnergyEventView.SetEventInfo(gainEnergyEvent, _eventViewParent);
                    await _PlayAndRecycle(
                        gainEnergyEventView,
                        _gainEnergyEventViewFactory,
                        cancellationToken);
                    break;

                case LoseEnergyEvent loseEnergyEvent:
                    var loseEnergyEventView = _loseEnergyEventViewFactory.CreatePrefab();
                    loseEnergyEventView.SetEventInfo(loseEnergyEvent, _eventViewParent);
                    await _PlayAndRecycle(
                        loseEnergyEventView,
                        _loseEnergyEventViewFactory,
                        cancellationToken);
                    break;

                case IncreaseDispositionEvent increaseDispositionEvent:
                    var increaseDispositionEventView =
                        _increaseDispositionEventViewFactory.CreatePrefab();
                    increaseDispositionEventView.SetEventInfo(
                        increaseDispositionEvent,
                        _eventViewParent);
                    await _PlayAndRecycle(
                        increaseDispositionEventView,
                        _increaseDispositionEventViewFactory,
                        cancellationToken);
                    break;

                case DecreaseDispositionEvent decreaseDispositionEvent:
                    var decreaseDispositionEventView =
                        _decreaseDispositionEventViewFactory.CreatePrefab();
                    decreaseDispositionEventView.SetEventInfo(
                        decreaseDispositionEvent,
                        _eventViewParent);
                    await _PlayAndRecycle(
                        decreaseDispositionEventView,
                        _decreaseDispositionEventViewFactory,
                        cancellationToken);
                    break;
            }
        }

        private static async UniTask _PlayAndRecycle<TView>(
            TView eventView,
            PrefabFactory<TView> factory,
            CancellationToken cancellationToken)
            where TView : MonoBehaviour, IRecyclable, IAnimationNumberEventView
        {
            try
            {
                await eventView.PlayAnimation(cancellationToken);
            }
            finally
            {
                factory.RecyclePrefab(eventView);
            }
        }
    }
}
