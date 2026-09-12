using System.Collections;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;
using UnityEngine;

namespace MortalGame.GameModel
{

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

    public record CardTriggeredTimingAction(
        ICardEntity Card,
        CardTriggeredTiming TriggeredTiming,
        IActionSource Source) : IActionUnit
    {
        // CardTriggeredTiming 與 GameTiming 是不同層級的時機，不在此互相轉換。
        public GameTiming Timing => GameTiming.None;
    };

    public record CardPlayIntentAction(CardPlaySource CardPlaySource) : IActionUnit
    {
        public GameTiming Timing => GameTiming.CardPlayIntent;
        public IActionSource Source => CardPlaySource;
    };

    public record CardPlayResultAction(CardPlayResultSource CardPlayResultSource) : IActionUnit
    {
        public GameTiming Timing => GameTiming.CardPlayResult;
        public IActionSource Source => CardPlayResultSource;
    };

    public record CardFormChangedAction(CardFormChangedSource CardFormChangedSource) : IActionUnit
    {
        public GameTiming Timing => GameTiming.None;
        public IActionSource Source => CardFormChangedSource;
    };

}
