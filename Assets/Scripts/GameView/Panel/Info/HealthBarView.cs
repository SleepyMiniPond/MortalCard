using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.GameView
{

    public class HealthBarView : MonoBehaviour
    {
        [SerializeField]
        private Image _image;
        [SerializeField]
        private TextMeshProUGUI _hpText;
        [SerializeField]
        private TextMeshProUGUI _maxHpText;
        [SerializeField]
        private TextMeshProUGUI _shieldText;
        [SerializeField]
        private GameObject[] _shieldObjects;

        public void SetHealth(int hp, int maxHp)
        {
            _image.fillAmount = (float)hp / maxHp;
            _hpText.text = hp.ToString();
            _maxHpText.text = maxHp.ToString();
        }
        public void SetShield(int shield)
        {
            _shieldObjects.ForEach(obj => obj.SetActive(shield > 0));
            _shieldText.text = shield.ToString();
        }
    }
}
