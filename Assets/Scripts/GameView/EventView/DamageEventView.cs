using MortalGame.GameModel;
using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class DamageEventView : BaseAnimationEventView
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        public void SetEventInfo(DamageEvent damageEvent, Transform parent)
        {
            transform.SetParent(parent, false);
            _text.text = damageEvent.TakeDamageResult.DamagePoint.ToString();
        }

    }
}
