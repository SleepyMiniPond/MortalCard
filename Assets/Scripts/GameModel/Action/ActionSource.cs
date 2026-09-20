using System.Collections.Generic;
using Optional;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface IActionSource
    {
    }

    public class SystemSource : IActionSource
    {
        public static readonly SystemSource Instance = new();
    }

    public record SystemExectueStartSource(IPlayerEntity Player) : IActionSource;

    public record SystemExectueEndSource(IPlayerEntity Player) : IActionSource;

    public enum CardPlayReason
    {
        Active = 0,
        Effect = 1
    }

    public record CardPlayPayment(int EnergySpent);

    public record CardPlaySource(
        ICardEntity Card,
        int HandCardIndex,
        int HandCardsCount,
        CardPlayReason Reason,
        Option<CardPlayPayment> Payment,
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
    public record CharacterBuffSource(ICharacterBuffEntity Buff) : IActionSource;

    public record CardFormChangedSource(
        ICardEntity Card,
        string BeforeCardDataId,
        string AfterCardDataId,
        string TransformKey,
        CardFormChangeCause Cause) : IActionSource;

}
