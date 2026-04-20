using UnityEngine;

public enum ArithmeticType
{
    None = 0,
    Add,
    Multiply,
    Overwrite
}
public enum ArithmeticConditionType
{
    None = 0,
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
}
public enum SetConditionType
{
    None = 0,
    AnyInside,
    AllInside,
    AnyOutside,
    AllOutside,
}
public enum OrderType
{
    None = 0,
    Ascending,
    Descending,
    Random
}

public enum Faction
{
    None = 0,
    Ally,
    Enemy
}

public enum DamageType
{
    Normal,
    Penetrate,
    Additional,
    Effective
}
public enum DamageStyle
{
    None = 1 >> 0,
    FullAttack = 1 >> 1,
    QuickAttack = 1 >> 2,
    ComboAttack = 1 >> 3,
    FollowAttack = 1 >> 4,
    CounterAttack = 1 >> 5,
}

public enum EnergyGainType
{
    None = 0,
    RoundStartRecover,
    GainEffect,
}
public enum EnergyLoseType
{
    None = 0,
    PlayCardConsume,
    LoseEffect,
}
public enum MoveCardType
{
    None = 0,
    Draw,
    Discard,
    Recycle,
    Consume,
    Dispose,
}

public enum PlayerBuffPropertyDuration
{
    None = 0,
    ThisTurn,
    ThisBattle,
    ThisGame
}
public enum PlayerBuffProperty
{
    None = 0,
    AllCardPower,
    AllCardCost,
    NormalDamageAddition,
    PenetrateDamageAddition,
    EffectiveDamageAddition,
    AdditionalDamageAddition,
    HealAddition,
    ShieldAddition,
    NormalDamageRatio,
    PenetrateDamageRatio,
    EffectiveDamageRatio,
    AdditionalDamageRatio,
    HealRatio,
    ShieldRatio,
    MaxHealth,
    MaxEnergy,
}
public enum CharacterBuffProperty
{
    None = 0,
    EffectAttribute,
    MaxHealth,
    MaxEnergy,
}

public enum SelectType
{
    None = 0,
    Character,
    AllyCharacter,
    EnemyCharacter,
    Card,
    AllyCard,
    EnemyCard,
}

public enum TargetType
{
    None = 0,
    AllyCard,
    EnemyCard,
    AllyCharacter,
    EnemyCharacter,
}

public enum EffectType
{
    None = 0,
    Damage,
    Heal,
    Shield,
    GainEnergy,
    LoseEnergy,
    AdjustDisposition,
    RecycleDeck,
    DrawCard,
    MoveCard,
    CreateCard,
    AddPlayerBuff,
    RemovePlayerBuff,
    ModifyPlayerBuffLevel,
    AddCharacterBuff,
    RemoveCharacterBuff,
    ModifyCharacterBuffLevel,
    AddCardBuff,
    RemoveCardBuff,
    ModifyCardBuffLevel,
    CardPlayEffectAttribute,
}
public enum GameTiming
{
    None = 0,
    GameStart,
    TurnStart,
    TurnEnd,
    ExecuteStart,
    ExecuteEnd,
    CharacterSummon,
    CharacterDeath,
    DrawCard,
    PlayCardStart,
    PlayCardEnd,
    EffectIntent,
    EffectTargetIntent,
    EffectTargetResult,
    TriggerBuffStart,
    TriggerBuffEnd,
}

public enum SessionLifeTime
{
    WholeGame,
    WholeTurn,
    PlayCard
}

public enum EffectAttributeAdditionType
{
    None = 0,
    CostAddition,
    PowerAddition,
    NormalDamageAddition,
    PenetrateDamageAddition,
    EffectiveDamageAddition,
    AdditionalDamageAddition,
    HealAddition,
    ShieldAddition,
}
public enum EffectAttributeRatioType
{
    None = 0,
    NormalDamageRatio,
    PenetrateDamageRatio,
    EffectiveDamageRatio,
    AdditionalDamageRatio,
    HealRatio,
    ShieldRatio,
}