using System;
using System.Collections.Generic;
using Optional;

public interface IGameContextManager : IDisposable
{
    CardLibrary CardLibrary { get; }
    CardBuffLibrary CardBuffLibrary { get; }
    PlayerBuffLibrary PlayerBuffLibrary { get; }
    CharacterBuffLibrary CharacterBuffLibrary { get; }
    DispositionLibrary DispositionLibrary { get; }
    LocalizeLibrary LocalizeLibrary { get; }

    GameContext Context { get; }

    IGameContextManager SetClone();
    IGameContextManager SetSelectedPlayer(Option<IPlayerEntity> selectedPlayer);
    IGameContextManager SetSelectedCharacter(Option<ICharacterEntity> selectedCharacter);
    IGameContextManager SetSelectedCard(Option<ICardEntity> selectedCard);
}

public class GameContextManager : IGameContextManager
{
    private readonly CardLibrary _cardLibrary;
    private readonly CardBuffLibrary _cardBuffLibrary;
    private readonly PlayerBuffLibrary _playerBuffLibrary;
    private readonly CharacterBuffLibrary _characterBuffLibrary;
    private readonly DispositionLibrary _dispositionLibrary;
    private readonly LocalizeLibrary _localizeLibrary;

    public CardLibrary CardLibrary => _cardLibrary;
    public CardBuffLibrary CardBuffLibrary => _cardBuffLibrary;
    public PlayerBuffLibrary PlayerBuffLibrary => _playerBuffLibrary;
    public CharacterBuffLibrary CharacterBuffLibrary => _characterBuffLibrary;
    public DispositionLibrary DispositionLibrary => _dispositionLibrary;
    public LocalizeLibrary LocalizeLibrary => _localizeLibrary;

    private Stack<GameContext> _contextStack = new Stack<GameContext>();
    public GameContext Context => _contextStack.Peek();

    public GameContextManager(
        CardLibrary cardLibrary,
        CardBuffLibrary cardBuffLibrary,
        PlayerBuffLibrary playerBuffLibrary,
        CharacterBuffLibrary characterBuffLibrary,
        DispositionLibrary dispositionLibrary,
        LocalizeLibrary localizeLibrary)
    {
        _cardLibrary = cardLibrary;
        _cardBuffLibrary = cardBuffLibrary;
        _playerBuffLibrary = playerBuffLibrary;
        _characterBuffLibrary = characterBuffLibrary;
        _dispositionLibrary = dispositionLibrary;
        _localizeLibrary = localizeLibrary;
        _contextStack.Push(GameContext.EMPTY);
    }

    public void Dispose()
    {
        if (_contextStack.Count > 1)
        {
            _contextStack.Pop();
        }
    }

    public IGameContextManager SetClone()
    {
        _contextStack.Push(Context with { });
        return this;
    }
    public IGameContextManager SetSelectedPlayer(Option<IPlayerEntity> selectedPlayer)
    {
        return selectedPlayer.Match(
            some: player => {
                _contextStack.Push(Context with { SelectedPlayer = player.Identity });
                return this;
            },
            none: () => SetClone()
        );
    }
    public IGameContextManager SetSelectedCharacter(Option<ICharacterEntity> selectedCharacter)
    {
        return selectedCharacter.Match(
            some: character => {
                _contextStack.Push(Context with { SelectedCharacter = character.Identity });
                return this;
            },
            none: () => SetClone()
        );
    }
    public IGameContextManager SetSelectedCard(Option<ICardEntity> selectedCard)
    {
        return selectedCard.Match(
            some: card => {
                _contextStack.Push(Context with { SelectedCard = card.Identity });
                return this;
            },
            none: () => SetClone()
        );
    }
}

public record GameContext(
    Guid SelectedPlayer,
    Guid SelectedCharacter,
    Guid SelectedCard)
{ 
    public static GameContext EMPTY => new(Guid.Empty, Guid.Empty, Guid.Empty);
}