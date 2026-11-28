using MortalGame.Presentation.Abstractions;
using System;
using MortalGame.GameData;
using MortalGame.GameModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MortalGame.Presenter
{

    public class Context
    {
        private readonly AllyData _initialAllyData;
        public Dictionary<string, CardData> CardTable { get; private set; }
        public Dictionary<string, CardBuffData> CardBuffTable { get; private set; }
        public Dictionary<string, PlayerBuffData> PlayerBuffTable { get; private set; }
        public Dictionary<string, CharacterBuffData> CharacterBuffTable { get; private set; }
        public DispositionData[] DispositionSettings { get; private set; }
        public EnemyData[] AllEnemies { get; private set; }
        public AllyInstance Ally { get; private set; }
        public IReadOnlyDictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>> LocalizeTitleInfoSetting { get; private set; }
        public IReadOnlyDictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>> LocalizeInfoSetting { get; private set; }

        public Context(ScriptableDataLoader scriptableDataLoader)
        {
            CardTable = scriptableDataLoader.AllCards.ToDictionary(c => c.ID, c => c);
            CardBuffTable = scriptableDataLoader.AllCardBuffs.ToDictionary(c => c.ID, c => c);
            PlayerBuffTable = scriptableDataLoader.AllPlayerBuffs.ToDictionary(b => b.ID, b => b);
            CharacterBuffTable = scriptableDataLoader.AllCharacterBuffs.ToDictionary(b => b.ID, b => b);
            DispositionSettings = scriptableDataLoader.DispositionSettings();
            AllEnemies = scriptableDataLoader.AllEnemies;

            _initialAllyData = scriptableDataLoader.Ally;
            ResetRun();

            LocalizeTitleInfoSetting = scriptableDataLoader.LocalizeTitleInfoSetting();
            LocalizeInfoSetting = scriptableDataLoader.LocalizeInfoSetting();
        }

        public void CommitBattleCompletion(AllyBattleCompletion completion)
        {
            Ally = Ally.ApplyBattleCompletion(completion);
        }

        public void ResetRun()
        {
            Ally = new AllyInstance(
                Identity: Guid.NewGuid(),
                NameKey: _initialAllyData.PlayerData.NameKey,
                CurrentDisposition: _initialAllyData.InitialDisposition,
                CurrentHealth: _initialAllyData.PlayerData.InitialHealth,
                MaxHealth: _initialAllyData.PlayerData.MaxHealth,
                CurrentEnergy: _initialAllyData.PlayerData.MaxEnergy,
                MaxEnergy: _initialAllyData.PlayerData.MaxEnergy,
                Deck: _initialAllyData.PlayerData.Deck.Cards.Select(card => CardInstance.Create(card.Data)).ToList(),
                HandCardMaxCount: _initialAllyData.PlayerData.HandCardMaxCount);
        }
    }

}
