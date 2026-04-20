using System.Collections.Generic;
using UnityEngine;

public interface IActionSource
{ 
}

public class SystemSource : IActionSource
{
    public static readonly SystemSource Instance = new();
}

public record SystemExectueStartSource(IPlayerEntity Player) : IActionSource;

public record SystemExectueEndSource(IPlayerEntity Player) : IActionSource;

public record CardPlaySource(
    ICardEntity Card,
    int HandCardIndex,
    int HandCardsCount,
    LoseEnergyEffectCommand ConsumeEnergy,
    IEffectAttribute Attribute) : IActionSource
{ 
    public CardPlayResultSource CreateResultSource(IReadOnlyList<IEffectResultAction> effectResults)
    {
        return new CardPlayResultSource(this, effectResults);
    }
}

public record CardPlayResultSource(
    CardPlaySource CardPlaySource,
    IReadOnlyList<IEffectResultAction> EffectResults) : IActionSource;

public record PlayerBuffSource(IPlayerBuffEntity Buff) : IActionSource;

public record CardBuffSource(ICardBuffEntity Buff) : IActionSource;