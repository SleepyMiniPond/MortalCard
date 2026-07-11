using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace MortalGame.GameView
{
    public abstract class BaseAnimationEventView :
        MonoBehaviour,
        IRecyclable,
        IAnimationNumberEventView
    {
        [SerializeField]
        private PlayableDirector _playableDirector;

        public async UniTask PlayAnimation(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            try
            {
                await _playableDirector.PlayAsync(cancellationToken);
            }
            finally
            {
                _playableDirector.Stop();
                gameObject.SetActive(false);
            }
        }

        public virtual void Reset()
        {
        }
    }
}
