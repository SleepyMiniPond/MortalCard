using MortalGame.GameModel;
using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class IncreaseDispositionEventView : BaseAnimationEventView
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        public void SetEventInfo(IncreaseDispositionEvent increaseDispositionEvent, Transform parent)
        {
            transform.SetParent(parent, false);
            _text.text = increaseDispositionEvent.DeltaDisposition.ToString();
        }

    }
}
