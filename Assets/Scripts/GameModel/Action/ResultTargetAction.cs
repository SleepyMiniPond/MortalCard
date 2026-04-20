using Optional;
using UnityEngine;

public interface A{ int x => 2; }

public abstract record BaseResultAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType,
    GameTiming Timing = GameTiming.EffectTargetResult) : IEffectResultAction;

public record DamageResultAction(
    IActionSource Source,
    IActionTarget Target,
    TakeDamageResult DamageResult,
    EffectType EffectType = EffectType.Damage) : BaseResultAction(Source, Target, EffectType);

public record HealResultAction(
    IActionSource Source,
    IActionTarget Target,
    GetHealResult HealResult,
    EffectType EffectType = EffectType.Heal) : BaseResultAction(Source, Target, EffectType);

public record ShieldResultAction(
    IActionSource Source,
    IActionTarget Target,
    GetShieldResult ShieldResult,
    EffectType EffectType = EffectType.Shield) : BaseResultAction(Source, Target, EffectType);

public record GainEnergyResultAction(
    IActionSource Source,
    IActionTarget Target,
    GainEnergyResult EnergyResult,
    EffectType EffectType = EffectType.GainEnergy) : BaseResultAction(Source, Target, EffectType);

public record LoseEnergyResultAction(
    IActionSource Source,
    IActionTarget Target,
    LoseEnergyResult EnergyResult,
    EffectType EffectType = EffectType.LoseEnergy) : BaseResultAction(Source, Target, EffectType);

public record IncreaseDispositionResultAction(
    IActionSource Source,
    IActionTarget Target,
    IncreaseDispositionResult DispositionResult,
    EffectType EffectType = EffectType.AdjustDisposition) : BaseResultAction(Source, Target, EffectType);

public record DecreaseDispositionResultAction(
    IActionSource Source,
    IActionTarget Target,
    DecreaseDispositionResult DispositionResult,
    EffectType EffectType = EffectType.AdjustDisposition) : BaseResultAction(Source, Target, EffectType);

public record RecycleDeckResultAction(
    IActionTarget Target,
    EffectType EffectType = EffectType.RecycleDeck) : BaseResultAction(SystemSource.Instance, Target, EffectType);

public record DrawCardResultAction(
    IActionSource Source,
    IActionTarget Target,
    ICardEntity Card,
    EffectType EffectType = EffectType.DrawCard) : BaseResultAction(Source, Target, EffectType);

public record MoveCardResultAction(
    IActionSource Source,
    IActionTarget Target,
    MoveCardResult MoveCardResult,
    MoveCardType MoveType,
    EffectType EffectType = EffectType.MoveCard) : BaseResultAction(Source, Target, EffectType);

public record CreateCardResultAction(
    IActionSource Source,
    IActionTarget Target,
    CreateCardResult CreateResult,
    EffectType EffectType = EffectType.CreateCard) : BaseResultAction(Source, Target, EffectType);

public record CloneCardResultAction(
    IActionSource Source,
    IActionTarget Target,
    ICardEntity OriginCard,
    CreateCardResult CreateResult,
    EffectType EffectType = EffectType.CreateCard) : BaseResultAction(Source, Target, EffectType);

public record AddPlayerBuffResultAction(
    IActionSource Source,
    IActionTarget Target,
    AddPlayerBuffResult AddResult,
    EffectType EffectType = EffectType.AddPlayerBuff) : BaseResultAction(Source, Target, EffectType);

public record RemovePlayerBuffResultAction(
    IActionSource Source,
    IActionTarget Target,
    RemovePlayerBuffResult RemoveResult,
    EffectType EffectType = EffectType.RemovePlayerBuff) : BaseResultAction(Source, Target, EffectType);

public record ModifyPlayerBuffLevelResultAction(
    IActionSource Source,
    IActionTarget Target,
    ModifyPlayerBuffResult ModifyResult,
    EffectType EffectType = EffectType.ModifyPlayerBuffLevel) : BaseResultAction(Source, Target, EffectType);

public record AddCardBuffResultAction(
    IActionSource Source,
    IActionTarget Target,
    AddCardBuffResult AddResult,
    EffectType EffectType = EffectType.AddCardBuff) : BaseResultAction(Source, Target, EffectType);

public record RemoveCardBuffResultAction(
    IActionSource Source,
    IActionTarget Target,
    RemoveCardBuffResult RemoveResult,
    EffectType EffectType = EffectType.RemoveCardBuff) : BaseResultAction(Source, Target, EffectType);

public record ModifyCardBuffLevelResultAction(
    IActionSource Source,
    IActionTarget Target,
    ModifyCardBuffLevelResult ModifyResult,
    EffectType EffectType = EffectType.ModifyCardBuffLevel) : BaseResultAction(Source, Target, EffectType);