using MortalGame.GameModel;
using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class GainEnergyEventView : BaseAnimationEventView
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        public void SetEventInfo(GainEnergyEvent gainEnergyEvent, Transform parent)
        {
            transform.SetParent(parent, false);
            _text.text = gainEnergyEvent.GainEnergyResult.DeltaEp.ToString();
        }

    }
}
