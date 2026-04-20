using System;
using UnityEngine;


public enum CardType
{
    None = 0,
    Attack,     // 捅人
    Defense,    // 備揍     
    Speech,     // 嘴攻
    Sneak,      // 暗器
    Special,    // 絕招
    Item
}

public enum CardRarity
{
    None = 0,
    Common,
    Rare,
    Epic,
    Legendary
}

public enum CardTheme
{
    None = 0,
    TangSect,   // 唐門
    Emei,       // 峨嵋
    Songshan,   // 嵩山
    BeggarClan, // 丐幫
    DianCang    // 點蒼
}

public enum CardProperty : int
{
    None                = 0,
    EffectRepeat         = 1,
    Recycle             = 1 << 1,
    PowerAddition       = 1 << 2, 
    CostAddition        = 1 << 3,
    Initialize          = 1 << 4,
    Preserved           = 1 << 5,
    Sealed              = 1 << 6,
    // Consumable means the card will be removed from the battle but added again next battle.
    Consumable          = 1 << 7,
    // Dispose means the card will be removed from the game forever.
    Dispose             = 1 << 8,
    AutoDispose         = 1 << 9,
}
public enum CardTriggeredTiming
// TODO: 卡片關鍵字效果 
// ex:  保留/靈光/餘波
{
    None = 0,
    Drawed,
    EffectDrawed,
    Played,
    EffectPlayed,
    Preserved,
    Discarded,
    EffectDiscarded,
    Initialize,
}

public enum CardCollectionType
{
    None = 0,
    Deck,
    HandCard,
    Graveyard,
    ExclusionZone,
    DisposeZone,
}

public enum TargetLogicTag
{
    None = 0,
    ToEnemy,
    ToAlly,
    ToRandom,
}