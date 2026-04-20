using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScriptableDataLoader : MonoBehaviour
{ 
    [SerializeField]
    private AllCardScriptable _allCardScriptable;
    
    [SerializeField]
    private AllCardBuffScriptable _allCardBuffScriptable;

    [SerializeField]
    private AllPlayerBuffScriptable _allPlayerBuffScriptable;

    [SerializeField]
    private AllCharacterBuffScriptable _allCharacterBuffScriptable;

    [SerializeField]
    private AllPlayerScriptable _allPlayerScriptable;

    [SerializeField]
    private ExcelDatas _excelDatasScriptable;

    public CardData[] AllCards => _allCardScriptable.AllCardData.Select(c => c.Data).ToArray();
    public CardBuffData[] AllCardBuffs => _allCardBuffScriptable.AllCardBuffData.Select(c => c.Data).ToArray();
    public PlayerBuffData[] AllPlayerBuffs => _allPlayerBuffScriptable.AllBuffData.Select(b => b.Data).ToArray();
    public CharacterBuffData[] AllCharacterBuffs => _allCharacterBuffScriptable.AllBuffData.Select(b => b.Data).ToArray();
    public AllyData Ally => _allPlayerScriptable.AllyObject.Ally;
    public EnemyData[] AllEnemies => _allPlayerScriptable.EnemyObjects.Select(p => p.Enemy).ToArray();

    public DispositionData[] DispositionSettings()
    {
        return _excelDatasScriptable.Disposition
            .Select(row => new DispositionData(
                row.Id,
                row.Range,
                row.RecoverEnergyPoint,
                row.DrawCardCount
            ))
            .ToArray();
    }

    public IReadOnlyDictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>> LocalizeTitleInfoSetting()
    {
        return new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>
        {
            {
                LocalizeTitleInfoType.Player,
                ParseTable(_excelDatasScriptable.LocalizePlayer)
            },
            {
                LocalizeTitleInfoType.Card,
                ParseTable(_excelDatasScriptable.LocalizeCard)
            },
            {
                LocalizeTitleInfoType.CardBuff,
                ParseTable(_excelDatasScriptable.LocalizeCardBuff)
            },
            {
                LocalizeTitleInfoType.PlayerBuff,
                ParseTable(_excelDatasScriptable.LocalizePlayerBuff)
            },
            {
                LocalizeTitleInfoType.KeyWord,
                ParseTable(_excelDatasScriptable.LocalizeKeyWord)
            },
        };

        Dictionary<string, LocalizeTitleInfoData> ParseTable(List<LocalizeExcelTitleData> datas)
            => datas.ToDictionary(d => d.Id, d => new LocalizeTitleInfoData(d.Title, d.Info));
    }

    public IReadOnlyDictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>> LocalizeInfoSetting()
    {
        return new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>
        {
            {
                LocalizeInfoType.UI,
                ParseTable(_excelDatasScriptable.LocalizeUI)
            },
        };

        Dictionary<string, LocalizeInfoData> ParseTable(List<LocalizeExcelData> datas)
            => datas.ToDictionary(d => d.Id, d => new LocalizeInfoData(d.Info));
    }
}
