using Optional;
using UnityEngine;

public abstract record BaseIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType,
    GameTiming Timing = GameTiming.EffectTargetIntent) : IEffectTargetAction;

public record DamageIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    DamageType Type,
    EffectType EffectType = EffectType.Damage) : BaseIntentTargetAction(Source, Target, EffectType);

public record HealIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.Heal) : BaseIntentTargetAction(Source, Target, EffectType);

public record ShieldIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.Shield) : BaseIntentTargetAction(Source, Target, EffectType);

public record GainEnergyIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.GainEnergy) : BaseIntentTargetAction(Source, Target, EffectType);

public record LoseEnergyIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.LoseEnergy) : BaseIntentTargetAction(Source, Target, EffectType);

public record IncreaseDispositionIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.AdjustDisposition) : BaseIntentTargetAction(Source, Target, EffectType);

public record DecreaseDispositionIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.AdjustDisposition) : BaseIntentTargetAction(Source, Target, EffectType);

public record RecycleDeckIntentTargetAction(
    IActionTarget Target,
    EffectType EffectType = EffectType.RecycleDeck) : BaseIntentTargetAction(SystemSource.Instance, Target, EffectType);

public record DrawCardIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.DrawCard) : BaseIntentTargetAction(Source, Target, EffectType);

public record DiscardCardIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.MoveCard) : BaseIntentTargetAction(Source, Target, EffectType);

public record ConsumeCardIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.MoveCard) : BaseIntentTargetAction(Source, Target, EffectType);

public record DisposeCardIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.MoveCard) : BaseIntentTargetAction(Source, Target, EffectType);

public record CreateCardIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.CreateCard) : BaseIntentTargetAction(Source, Target, EffectType);

public record CloneCardIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.CreateCard) : BaseIntentTargetAction(Source, Target, EffectType);

public record AddPlayerBuffIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.AddPlayerBuff) : BaseIntentTargetAction(Source, Target, EffectType);

public record RemovePlayerBuffIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.RemovePlayerBuff) : BaseIntentTargetAction(Source, Target, EffectType);

public record AddCardBuffIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.AddCardBuff) : BaseIntentTargetAction(Source, Target, EffectType);

public record RemoveCardBuffIntentTargetAction(
    IActionSource Source,
    IActionTarget Target,
    EffectType EffectType = EffectType.RemoveCardBuff) : BaseIntentTargetAction(Source, Target, EffectType);