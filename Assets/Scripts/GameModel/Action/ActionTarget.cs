using UnityEngine;

public interface IActionTarget
{

}

public class SystemTarget : IActionTarget
{
    public SystemTarget()
    {
    }
}

public class PlayerTarget : IActionTarget
{
    public IPlayerEntity Player { get; private set; }

    public PlayerTarget(IPlayerEntity player)
    {
        Player = player;
    }
}

public class CharacterTarget : IActionTarget
{
    public ICharacterEntity Character { get; private set; }

    public CharacterTarget(ICharacterEntity character)
    {
        Character = character;
    }
}

public class CardTarget : IActionTarget
{
    public ICardEntity Card { get; private set; }

    public CardTarget(ICardEntity card)
    {
        Card = card;
    }
}

public class PlayerAndCardTarget : IActionTarget
{
    public IPlayerEntity Player { get; private set; }
    public ICardEntity Card { get; private set; }

    public PlayerAndCardTarget(IPlayerEntity player, ICardEntity card)
    {
        Player = player;
        Card = card;
    }
}