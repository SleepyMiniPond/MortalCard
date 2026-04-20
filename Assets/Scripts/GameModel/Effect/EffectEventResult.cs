using System.Collections.Generic;
using Optional;
using UnityEngine;

public interface IEffectResult
{
}

public record TakeDamageResult(
    DamageType Type,
    int DamagePoint,
    int DeltaHp,
    int DeltaDp,
    int OverHp) : IEffectResult;
public record GetHealResult(
    int HealPoint,
    int DeltaHp,
    int OverHp) : IEffectResult;
public record GetShieldResult(
    int ShieldPoint,
    int DeltaDp,
    int OverDp) : IEffectResult;

public record GainEnergyResult(
    EnergyGainType Type,
    int EnergyPoint,
    int DeltaEp,
    int OverEp) : IEffectResult;
public record LoseEnergyResult(
    EnergyLoseType Type,
    int EnergyPoint,
    int DeltaEp,
    int OverEp) : IEffectResult;

public record IncreaseDispositionResult(
    int DispositionPoint,
    int DeltaDisposition,
    int OverDisposition) : IEffectResult;
public record DecreaseDispositionResult(
    int DispositionPoint,
    int DeltaDisposition,
    int OverDisposition) : IEffectResult;

public record AddPlayerBuffResult(
    IPlayerBuffEntity PlayerBuff) : IEffectResult;
public record ModifyPlayerBuffResult(    
    IPlayerBuffEntity PlayerBuff,
    int DeltaLevel,
    int NewLevel) : IEffectResult;
public record RemovePlayerBuffResult(
    IPlayerBuffEntity PlayerBuff) : IEffectResult;
        
public record AddCharacterBuffResult(
    ICharacterBuffEntity CharacterBuff) : IEffectResult;
public record ModifyCharacterBuffResult(    
    ICharacterBuffEntity CharacterBuff,
    int DeltaLevel,
    int NewLevel) : IEffectResult;
public record RemoveCharacterBuffResult(
    ICharacterBuffEntity CharacterBuff) : IEffectResult;

public record AddCardBuffResult(
    ICardBuffEntity CardBuff) : IEffectResult;
public record ModifyCardBuffLevelResult(    
    ICardBuffEntity CardBuff,
    int DeltaLevel,
    int NewLevel) : IEffectResult;
public record RemoveCardBuffResult(
    ICardBuffEntity CardBuff) : IEffectResult;

public record MoveCardResult(
    ICardEntity Card,
    CardCollectionType Start,
    CardCollectionType Destination) : IEffectResult;
public record CreateCardResult(
    ICardEntity Card,
    CardCollectionType Zone) : IEffectResult;
public record CloneCardResult(
    ICardEntity OriginCard,
    ICardEntity Card,
    CardCollectionType Zone) : IEffectResult;