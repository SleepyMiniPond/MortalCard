using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortalGame.GameView
{

    public interface IAnimationNumberEventView
    {
        UniTask PlayAnimation(CancellationToken cancellationToken);
    }
}
