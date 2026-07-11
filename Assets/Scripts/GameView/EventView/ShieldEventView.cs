using MortalGame.GameModel;
using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class ShieldEventView : BaseAnimationEventView
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        public void SetEventInfo(GetShieldEvent getShieldEvent, Transform parent)
        {
            transform.SetParent(parent, false);
            _text.text = getShieldEvent.GetShieldResult.ShieldPoint.ToString();
        }

    }
}
