using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CardData
{
    [Serializable]
    public class TriggeredCardEffect
    {
        [TableColumnWidth(150, false)]
        public CardTriggeredTiming Timing;

        [ShowInInspector]
        // TODO: conditional cardeffect
        public ICardEffect[] Effects = new ICardEffect[0];
    }

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
    public MainTargetSelectLogic MainSelect = new ();
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

[Serializable]
public class MainTargetSelectLogic
{
    public IMainTargetSelectable MainSelectable = new NoneSelectable();
    public TargetLogicTag LogicTag = TargetLogicTag.None;
}