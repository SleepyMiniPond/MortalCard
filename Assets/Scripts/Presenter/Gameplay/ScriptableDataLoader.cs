using MortalGame.Presentation.Abstractions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MortalGame.GameData;

namespace MortalGame.Presenter
{

    public class ScriptableDataLoader : MonoBehaviour
    {
        [SerializeField]
        private GameContentCatalog _gameContentCatalog;

        [SerializeField]
        private AllPlayerScriptable _allPlayerScriptable;

        [SerializeField]
        private ExcelDatas _excelDatasScriptable;

        public CardData[] AllCards => _gameContentCatalog.CardAssets
            .Select(asset => asset.CardData)
            .ToArray();
        public CardBuffData[] AllCardBuffs => _gameContentCatalog.CardBuffAssets
            .Select(asset => asset.Data)
            .ToArray();
        public PlayerBuffData[] AllPlayerBuffs => _gameContentCatalog.PlayerBuffAssets
            .Select(asset => asset.Data)
            .ToArray();
        public CharacterBuffData[] AllCharacterBuffs => _gameContentCatalog.CharacterBuffAssets
            .Select(asset => asset.Data)
            .ToArray();
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

}
