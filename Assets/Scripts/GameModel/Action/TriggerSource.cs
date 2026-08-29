using MortalGame.GameData;
using Optional;
using Unity.VisualScripting;
using UnityEngine;

namespace MortalGame.GameModel
{

    public record TriggerContext(
        IGameplayModel Model,
        ITriggeredSource Triggered,
        IActionUnit Action)
    {
        public Option<GameTiming> ReactionOriginTiming { get; init; } =
            Action is UpdateTimingAction { Timing: not GameTiming.None } timingAction
                ? timingAction.Timing.Some()
                : Option.None<GameTiming>();
    }

    public interface ITriggeredSource
    {
    }

    public interface ICardTriggeredSource : ITriggeredSource
    {
        ICardEntity Card { get; }
    }

    public interface ICharacterTriggeredSource : ITriggeredSource
    {
        ICharacterEntity Character { get; }
    }

    public interface IPlayerTriggeredSource : ITriggeredSource
    {
        IPlayerEntity Player { get; }
    }

    public class CardPlayTrigger : ICardTriggeredSource
    {
        public CardPlaySource CardPlay { get; private set; }
        public ICardEntity Card => CardPlay.Card;

        public CardPlayTrigger(CardPlaySource cardPlay)
        {
            CardPlay = cardPlay;
        }
    }

    public class CardTrigger : ICardTriggeredSource
    {
        public ICardEntity Card { get; private set; }

        public CardTrigger(ICardEntity card)
        {
            Card = card;
        }
    }

    public class CardBuffTrigger : ICardTriggeredSource
    {
        public ICardEntity Card { get; private set; }
        public ICardBuffEntity Buff { get; private set; }

        public CardBuffTrigger(ICardEntity card, ICardBuffEntity buff)
        {
            Card = card;
            Buff = buff;
        }
    }

    public class PlayerBuffTrigger : IPlayerTriggeredSource
    {
        public IPlayerEntity Player { get; private set; }
        public IPlayerBuffEntity Buff { get; private set; }

        public PlayerBuffTrigger(IPlayerEntity player, IPlayerBuffEntity buff)
        {
            Player = player;
            Buff = buff;
        }
    }

    public class CharacterBuffTrigger : ICharacterTriggeredSource
    {
        public ICharacterEntity Character { get; private set; }
        public ICharacterBuffEntity Buff { get; private set; }

        public CharacterBuffTrigger(ICharacterEntity character, ICharacterBuffEntity buff)
        {
            Character = character;
            Buff = buff;
        }
    }

    public class PlayerTrigger : IPlayerTriggeredSource
    {
        public IPlayerEntity Player { get; private set; }

        public PlayerTrigger(IPlayerEntity player)
        {
            Player = player;
        }
    }

    /// <summary>
    /// 提供卡牌 External Override 的解除條件存取目前狀態與 Session。
    /// </summary>
    public sealed class CardFormOverrideTrigger : ICardTriggeredSource
    {
        public ICardEntity Card { get; }
        public CardFormOverrideState State { get; }

        public CardFormOverrideTrigger(ICardEntity card, CardFormOverrideState state)
        {
            Card = card;
            State = state;
        }
    }

}
