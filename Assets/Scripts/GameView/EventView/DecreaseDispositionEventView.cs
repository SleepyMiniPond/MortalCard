using MortalGame.GameModel;
using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class DecreaseDispositionEventView : BaseAnimationEventView
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        public void SetEventInfo(DecreaseDispositionEvent decreaseDispositionEvent, Transform parent)
        {
            transform.SetParent(parent, false);
            _text.text = decreaseDispositionEvent.DeltaDisposition.ToString();
        }

    }
}
