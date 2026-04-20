using System.Collections;
using System.Collections.Generic;
using Optional;
using UnityEngine;

public interface IActionUnit
{
    GameTiming Timing { get; }
    IActionSource Source { get; }
}

public interface IActionTargetUnit : IActionUnit
{
    IActionTarget Target { get; }
}

public record UpdateTimingAction(GameTiming Timing, IActionSource Source) : IActionUnit;

// EffectAction
public interface IEffectAction : IActionUnit
{
    EffectType EffectType { get; }
}

public interface IEffectTargetAction : IEffectAction, IActionTargetUnit
{
}

public interface IEffectResultAction : IEffectAction, IActionTargetUnit
{
}

// LookAction
public record CardLookIntentAction(ICardEntity Card) : IActionUnit
{
    public GameTiming Timing => GameTiming.None;
    public IActionSource Source => SystemSource.Instance;
};
    
public record CardBuffPropertyLookAction(ICardBuffPropertyEntity Property) : IActionUnit
{
    public GameTiming Timing => GameTiming.None;
    public IActionSource Source => SystemSource.Instance;
};

public record PlayerBuffPropertyLookAction(IPlayerBuffPropertyEntity Property) : IActionUnit
{
    public GameTiming Timing => GameTiming.None;
    public IActionSource Source => SystemSource.Instance;
};

//===
public record CardCreateSystemAction(
    IPlayerEntity Player,
    CardCollectionType Destination) : IActionUnit
{
    public GameTiming Timing => GameTiming.GameStart;
    public IActionSource Source => SystemSource.Instance;
};

public record CardPlayIntentAction(CardPlaySource CardPlaySource) : IActionUnit
{
    public GameTiming Timing => GameTiming.PlayCardStart;
    public IActionSource Source => CardPlaySource;
};

public record CardPlayResultAction(CardPlayResultSource CardPlayResultSource) : IActionUnit
{
    public GameTiming Timing => GameTiming.PlayCardEnd;
    public IActionSource Source => CardPlayResultSource;
};