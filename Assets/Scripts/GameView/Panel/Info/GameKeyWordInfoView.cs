using TMPro;
using UnityEngine;

public class GameKeyWordInfoView : MonoBehaviour, IRecyclable
{
    [SerializeField]
    private TextMeshProUGUI _gameKeyWordTitleText;
    [SerializeField]
    private TextMeshProUGUI _gameKeyWordInfoText;

    public void SetInfo(GameKeyWord gameKeyWord, LocalizeLibrary localizeLibrary)
    {
        var localizeData = localizeLibrary.Get(LocalizeTitleInfoType.KeyWord, gameKeyWord.ToString());
        _gameKeyWordTitleText.text = localizeData.Title;
        _gameKeyWordInfoText.text = localizeData.Info;
    }
    
    public void Reset()
    {
    }
}
