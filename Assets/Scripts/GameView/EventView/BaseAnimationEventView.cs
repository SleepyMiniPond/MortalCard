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
            using var animationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, this.GetCancellationTokenOnDestroy());
            var token = animationCancellation.Token;
            token.ThrowIfCancellationRequested();
            gameObject.SetActive(true);
            try
            {
                await _playableDirector.PlayAsync(token);
            }
            finally
            {
                // 場景卸載可能先銷毀 Director 或特效，再恢復非同步清理。
                if (_playableDirector != null)
                    _playableDirector.Stop();
                if (this != null)
                    gameObject.SetActive(false);
            }
        }

        public virtual void Reset()
        {
        }
    }
}
