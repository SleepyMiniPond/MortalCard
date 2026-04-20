using Unity.VisualScripting;
using UnityEngine;

public record TriggerContext(
    IGameplayModel Model,
    ITriggeredSource Triggered,
    IActionUnit Action);

public interface ITriggeredSource
{
}

public class CardPlayTrigger : ITriggeredSource
{
    public CardPlaySource CardPlay { get; private set; }

    public CardPlayTrigger(CardPlaySource cardPlay)
    {
        CardPlay = cardPlay;
    }
}

public class CardTrigger : ITriggeredSource
{
    public ICardEntity Card { get; private set; }

    public CardTrigger(ICardEntity card)
    {
        Card = card;
    }
}

public class CardBuffTrigger : ITriggeredSource
{
    public ICardBuffEntity Buff { get; private set; }

    public CardBuffTrigger(ICardBuffEntity buff)
    {
        Buff = buff;
    }
}

public class PlayerBuffTrigger : ITriggeredSource
{
    public IPlayerBuffEntity Buff { get; private set; }

    public PlayerBuffTrigger(IPlayerBuffEntity buff)
    {
        Buff = buff;
    }
}

public class CharacterBuffTrigger : ITriggeredSource
{
    public ICharacterBuffEntity Buff { get; private set; }

    public CharacterBuffTrigger(ICharacterBuffEntity buff)
    {
        Buff = buff;
    }
}

public class PlayerTrigger : ITriggeredSource
{
    public IPlayerEntity Player { get; private set; }

    public PlayerTrigger(IPlayerEntity player)
    {
        Player = player;
    }
}