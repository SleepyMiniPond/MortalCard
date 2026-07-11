using MortalGame.GameModel;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MortalGame.GameView
{

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

        protected Option<ICharacterAnimationLifetime> _animationLifetime =
            Option.None<ICharacterAnimationLifetime>();
        protected IGameplayModel _statusWatcher;

        public void UpdateHealth(IAnimationNumberEvent healthEvent)
        {
            _animationLifetime.MatchSome(lifetime => lifetime.Enqueue(healthEvent));
        }
        public void UpdateEnergy(IAnimationNumberEvent energyEvent)
        {
            _animationLifetime.MatchSome(lifetime => lifetime.Enqueue(energyEvent));
        }
        public void UpdateDisposition(IAnimationNumberEvent dispositionEvent)
        {
            _animationLifetime.MatchSome(lifetime => lifetime.Enqueue(dispositionEvent));
        }

        protected ICharacterAnimationLifetime _StartAnimationWorker()
        {
            ICharacterEventAnimationPlayer animationPlayer =
                new CharacterEventAnimationPlayer(
                    _damageEventViewFactory,
                    _healEventViewFactory,
                    _shieldEventViewFactory,
                    _gainEnergyEventViewFactory,
                    _loseEnergyEventViewFactory,
                    _increaseDispositionEventViewFactory,
                    _decreaseDispositionEventViewFactory,
                    _eventViewParent);
            var worker = new CharacterAnimationWorker(
                animationPlayer.Play,
                _minTimeInterval);
            _animationLifetime = Option.Some<ICharacterAnimationLifetime>(worker);
            return worker;
        }
    }
}
