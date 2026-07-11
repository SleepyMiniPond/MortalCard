using MortalGame.GameModel;
using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class LoseEnergyEventView : BaseAnimationEventView
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        public void SetEventInfo(LoseEnergyEvent loseEnergyEvent, Transform parent)
        {
            transform.SetParent(parent, false);
            _text.text = loseEnergyEvent.LoseEnergyResult.DeltaEp.ToString();
        }

    }
}
