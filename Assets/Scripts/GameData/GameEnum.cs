using UnityEngine;

namespace MortalGame.GameData
{

    public enum ArithmeticType
    {
        None = 0,
        Add = 1,
        Multiply = 2,
        Overwrite = 3,
        Subtract = 4,
        Divide = 5,
        Remainder = 6,
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
        ApplyCardFormOverride,
    }
    public enum GameTiming
    {
        None = 0,
        GameStart = 1,
        EffectIntent = 11,
        EffectTargetIntent = 12,
        EffectTargetResult = 13,

        BeforeTurnStart = 16,
        AfterTurnStart = 17,
        BeforeDrawCard = 18,
        AfterDrawCard = 19,
        BeforeExecuteStart = 20,
        AfterExecuteStart = 21,
        BeforeExecuteEnd = 22,
        AfterExecuteEnd = 23,
        BeforeTurnEnd = 24,
        AfterTurnEnd = 25,
        BeforePlayCardStart = 26,
        AfterPlayCardStart = 27,
        BeforePlayCardEnd = 28,
        AfterPlayCardEnd = 29,
        BeforeTriggerBuffEffect = 30,
        AfterTriggerBuffEffect = 31,
        BeforeCharacterSummon = 32,
        AfterCharacterSummon = 33,
        BeforeCharacterDeath = 34,
        AfterCharacterDeath = 35,
        CardPlayIntent = 36,
        CardPlayResult = 37,
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

}
