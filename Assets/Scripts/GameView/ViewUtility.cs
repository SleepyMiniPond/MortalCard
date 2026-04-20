using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public static class PlayableDirectorExtensions
{
    public static UniTask PlayAsync(this PlayableDirector self)
    {
        self.Play();
        return UniTask.WaitWhile(() => self.state == PlayState.Playing);
    }
}