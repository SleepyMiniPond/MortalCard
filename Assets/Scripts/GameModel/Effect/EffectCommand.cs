using System.Collections;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface IEffectCommand
    { }

    public record DamageEffectCommand(
        ICharacterEntity Target,
        int DamagePoint,
        DamageType DamageType) : IEffectCommand;

    public record HealEffectCommand(
        ICharacterEntity Target,
        int HealPoint) : IEffectCommand;

    public record ShieldEffectCommand(
        ICharacterEntity Target,
        int ShieldPoint) : IEffectCommand;

    public record GainEnergyEffectCommand(
        IPlayerEntity Target,
        int EnergyPoint) : IEffectCommand;
    public record LoseEnergyEffectCommand(
        IPlayerEntity Target,
        int EnergyPoint) : IEffectCommand;

    public record IncreaseDispositionEffectCommand(
        AllyEntity Target,
        int DispositionPoint) : IEffectCommand;
    public record DecreaseDispositionEffectCommand(
        AllyEntity Target,
        int DispositionPoint) : IEffectCommand;

    public record AddPlayerBuffEffectCommand(
        IPlayerEntity Target,
        IPlayerBuffEntity NewBuff) : IEffectCommand;
    public record RemovePlayerBuffEffectCommand(
        IPlayerEntity Target,
        IPlayerBuffEntity ExistBuff) : IEffectCommand;
    public record ModifyPlayerBuffLevelEffectCommand(
        IPlayerEntity Target,
        string BuffId,
        int DeltaLevel) : IEffectCommand;

    public record DrawCardEffectCommand(
        IPlayerEntity Target,
        int DrawCount) : IEffectCommand;
    public record MoveCardEffectCommand(
        IPlayerEntity Target,
        ICardEntity Card,
        CardCollectionType Start,
        CardCollectionType Destination,
        MoveCardType MoveType) : IEffectCommand;

    public record CreateCardEffectCommand(
        IPlayerEntity Target,
        ICardEntity NewCard,
        CardCollectionType Destination) : IEffectCommand;
    public record CloneCardEffectCommand(
        IPlayerEntity Target,
        ICardEntity OriginCard,
        ICardEntity ClonedCard,
        CardCollectionType Destination) : IEffectCommand;
    public record AddCardBuffEffectCommand(
        ICardEntity Target,
        CardBuffLayerHandle LayerHandle,
        ICardBuffEntity NewBuff) : IEffectCommand;
    public record RemoveCardBuffEffectCommand(
        ICardEntity Target,
        CardBuffLayerHandle LayerHandle,
        ICardBuffEntity ExistBuff) : IEffectCommand;
    public record ModifyCardBuffLevelEffectCommand(
        ICardEntity Target,
        CardBuffLayerHandle LayerHandle,
        string BuffId,
        int Level) : IEffectCommand;

    public record ApplyCardFormOverrideEffectCommand(
        ICardEntity Target,
        string OverrideKey,
        string TargetCardDataId,
        IReadOnlyList<CardFormOverrideReleaseRule> ReleaseRules,
        IReadOnlyDictionary<string, IReactionSessionData> ReactionSessionDatas) : IEffectCommand;

    public record ModifyCardAttributeEffectCommand(
        EffectAttributeAdditionType AdditionType,
        int AdditionValue) : IEffectCommand;

}
