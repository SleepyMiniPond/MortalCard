using System;
using MortalGame.GameModel;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{
    /// <summary>
    /// Standard 與 Override 卡牌資料共用的基底型別。
    /// </summary>
    public abstract class CardData
    {
        [BoxGroup("Identification")]
        public string ID;

        [Space]
        [TitleGroup("BasicData")]
        public CardRarity Rarity;
        [TitleGroup("BasicData")]
        public CardType Type;
        [TitleGroup("BasicData")]
        [ShowInInspector]
        public CardTheme[] Themes = new CardTheme[0];
        [TitleGroup("BasicData")]
        [Range(0, 10)]
        public int Cost;
        [TitleGroup("BasicData")]
        [Range(0, 20)]
        public int Power;

        [ShowInInspector]
        [BoxGroup("Target")]
        public MainTargetSelectLogic MainSelect = new();
        [ShowInInspector]
        [BoxGroup("Target")]
        public List<ISubSelectionGroup> SubSelects = new();

        [BoxGroup("Effects")]
        [ShowInInspector]
        public List<ICardEffect> Effects = new();
        [BoxGroup("Effects")]
        [ShowInInspector]
        [TableList]
        public List<TriggeredCardEffect> TriggeredEffects = new();

        [ShowInInspector]
        [BoxGroup("Properties")]
        public List<ICardPropertyData> PropertyDatas = new();
    }

    /// <summary>
    /// 可獨立存在的標準卡片資料，也是唯一能定義 Self Transform 規則的卡片資料。
    /// </summary>
    public sealed class StandardCardData : CardData
    {
        [BoxGroup("Transform")]
        [ShowInInspector]
        [TableList]
        public List<CardTransformRule> TransformRules = new();
    }

}
