using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

namespace MortalGame.GameView
{

public class DamageEventView : MonoBehaviour, IRecyclable, IAnimationNumberEventView
{
    [SerializeField]
    private TextMeshProUGUI _text;
    [SerializeField]
    private PlayableDirector _playableDirector;

    public void SetEventInfo(DamageEvent damageEvent, Transform parent)
    {
        transform.SetParent(parent, false);
        _text.text = damageEvent.TakeDamageResult.DamagePoint.ToString();
    }

    public void Reset()
    {
    }

    public async UniTask PlayAnimation()
    {
        gameObject.SetActive(true);
        await _playableDirector.PlayAsync();
        gameObject.SetActive(false);
    }
}
}
