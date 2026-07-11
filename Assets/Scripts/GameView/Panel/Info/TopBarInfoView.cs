using TMPro;
using UnityEngine;

namespace MortalGame.GameView
{

    public class TopBarInfoView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _TurnText;

        public void UpdateTurnInfo(int currentTurn)
        {
            _TurnText.text = currentTurn.ToString();
        }
    }
}
