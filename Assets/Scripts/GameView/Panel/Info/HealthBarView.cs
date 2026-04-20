using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarView : MonoBehaviour
{
    [SerializeField]
    private Image _image;
    [SerializeField]
    private TextMeshProUGUI _hpText;
    [SerializeField]
    private TextMeshProUGUI _maxHpText;
    [SerializeField]
    private TextMeshProUGUI _dpText;
    [SerializeField]
    private GameObject[] _shieldObjects;

    public void SetHealth(int hp, int maxHp)
    {
        _image.fillAmount = (float)hp / maxHp;
        _hpText.text = hp.ToString();
        _maxHpText.text = maxHp.ToString();
    }
    public void SetShield(int dp)
    {
        _shieldObjects.ForEach(obj => obj.SetActive(dp > 0));
        _dpText.text = dp.ToString();
    }
}
