using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardBuffInfoView : MonoBehaviour, IRecyclable
{

    [SerializeField]
    private TextMeshProUGUI _cardBuffTitleText;
    [SerializeField]
    private TextMeshProUGUI _cardBuffInfoText;

    public void SetInfo(CardPropertyHint.InfoCellViewData viewData, LocalizeLibrary localizeLibrary)
    {
        var localizeData = localizeLibrary.Get(viewData.Type, viewData.LocalizeId);
        var templateValue = viewData.TemplateValues;

        _cardBuffTitleText.text = localizeData.Title;
        _cardBuffInfoText.text = localizeData.Info.ReplaceTemplateKeys(templateValue);
    }

    public void Reset()
    {
    }
}
