using MortalGame.GameModel;
using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class HealEventView : BaseAnimationEventView
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        public void SetEventInfo(GetHealEvent getHealEvent, Transform parent)
        {
            transform.SetParent(parent, false);
            _text.text = getHealEvent.GetHealResult.HealPoint.ToString();
        }

    }
}
