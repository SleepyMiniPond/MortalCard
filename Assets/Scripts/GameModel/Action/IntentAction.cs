using UnityEngine;

public abstract record BaseEffectIntentAction(
    IActionSource Source,
    EffectType EffectType,
    GameTiming Timing = GameTiming.EffectIntent) : IEffectAction;

public record DamageIntentAction(IActionSource Source, DamageType Type) : BaseEffectIntentAction(Source, EffectType.Damage);

public record HealIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.Heal);

public record ShieldIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.Shield);

public record GainEnergyIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.GainEnergy);
public record LoseEnergyIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.LoseEnergy);

public record IncreaseDispositionIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.AdjustDisposition);
public record DecreaseDispositionIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.AdjustDisposition);

public record RecycleDeckIntentAction() : BaseEffectIntentAction(SystemSource.Instance, EffectType.RecycleDeck);

public record DrawCardIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.DrawCard);

public record DiscardCardIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.MoveCard);
public record ConsumeCardIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.MoveCard);
public record DisposeCardIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.MoveCard);
public record CreateCardIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.CreateCard);
public record CloneCardIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.CreateCard);

public record AddPlayerBuffIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.AddPlayerBuff);
public record RemovePlayerBuffIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.RemovePlayerBuff);

public record AddCardBuffIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.AddCardBuff);
public record RemoveCardBuffIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.RemoveCardBuff);

public record CardPlayEffectAttributeIntentAction(IActionSource Source) : BaseEffectIntentAction(Source, EffectType.CardPlayEffectAttribute);