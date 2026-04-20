using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class IncreaseDispositionEventView: MonoBehaviour, IRecyclable, IAnimationNumberEventView
{
    [SerializeField]
    private TextMeshProUGUI _text;
    [SerializeField]
    private PlayableDirector _playableDirector;

    public void SetEventInfo(IncreaseDispositionEvent increaseDispositionEvent, Transform parent)
    {
        transform.SetParent(parent, false);
        _text.text = increaseDispositionEvent.DeltaDisposition.ToString();
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