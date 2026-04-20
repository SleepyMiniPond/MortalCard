using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergyBarView : MonoBehaviour
{
    [SerializeField]
    private Image _image;
    [SerializeField]
    private TextMeshProUGUI _energyText;
    [SerializeField]
    private TextMeshProUGUI _maxEnergyText;

    public void SetEnergy(int energy, int maxEnergy)
    {
        _image.fillAmount = (float)energy / maxEnergy;
        _energyText.text = energy.ToString();
        _maxEnergyText.text = maxEnergy.ToString();
    }
}
