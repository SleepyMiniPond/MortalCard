using System;
using System.Collections.Generic;
using Optional;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.GameModel
{

public interface IGameContextManager : IDisposable
{
    CardLibrary CardLibrary { get; }
    CardBuffLibrary CardBuffLibrary { get; }
    PlayerBuffLibrary PlayerBuffLibrary { get; }
    CharacterBuffLibrary CharacterBuffLibrary { get; }
    DispositionLibrary DispositionLibrary { get; }
    LocalizeLibrary LocalizeLibrary { get; }
    IGameRandom GameRandom { get; }
    ICardPropertyEntityFactory CardPropertyEntityFactory { get; }
    ICardBuffPropertyEntityFactory CardBuffPropertyEntityFactory { get; }
    ICardBuffLifeTimeEntityFactory CardBuffLifeTimeEntityFactory { get; }
    IReactionSessionEntityFactory ReactionSessionEntityFactory { get; }
    IPlayerBuffPropertyEntityFactory PlayerBuffPropertyEntityFactory { get; }
    IPlayerBuffLifeTimeEntityFactory PlayerBuffLifeTimeEntityFactory { get; }
    ICharacterBuffPropertyEntityFactory CharacterBuffPropertyEntityFactory { get; }
    ICharacterBuffLifeTimeEntityFactory CharacterBuffLifeTimeEntityFactory { get; }

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
    private IGameRandom _gameRandom;
    private readonly ICardPropertyEntityFactory _cardPropertyEntityFactory;
    private readonly ICardBuffPropertyEntityFactory _cardBuffPropertyEntityFactory;
    private readonly ICardBuffLifeTimeEntityFactory _cardBuffLifeTimeEntityFactory;
    private readonly IReactionSessionEntityFactory _reactionSessionEntityFactory;
    private readonly IPlayerBuffPropertyEntityFactory _playerBuffPropertyEntityFactory;
    private readonly IPlayerBuffLifeTimeEntityFactory _playerBuffLifeTimeEntityFactory;
    private readonly ICharacterBuffPropertyEntityFactory _characterBuffPropertyEntityFactory;
    private readonly ICharacterBuffLifeTimeEntityFactory _characterBuffLifeTimeEntityFactory;

    public CardLibrary CardLibrary => _cardLibrary;
    public CardBuffLibrary CardBuffLibrary => _cardBuffLibrary;
    public PlayerBuffLibrary PlayerBuffLibrary => _playerBuffLibrary;
    public CharacterBuffLibrary CharacterBuffLibrary => _characterBuffLibrary;
    public DispositionLibrary DispositionLibrary => _dispositionLibrary;
    public LocalizeLibrary LocalizeLibrary => _localizeLibrary;
    public IGameRandom GameRandom => _gameRandom;
    public ICardPropertyEntityFactory CardPropertyEntityFactory => _cardPropertyEntityFactory;
    public ICardBuffPropertyEntityFactory CardBuffPropertyEntityFactory => _cardBuffPropertyEntityFactory;
    public ICardBuffLifeTimeEntityFactory CardBuffLifeTimeEntityFactory => _cardBuffLifeTimeEntityFactory;
    public IReactionSessionEntityFactory ReactionSessionEntityFactory => _reactionSessionEntityFactory;
    public IPlayerBuffPropertyEntityFactory PlayerBuffPropertyEntityFactory => _playerBuffPropertyEntityFactory;
    public IPlayerBuffLifeTimeEntityFactory PlayerBuffLifeTimeEntityFactory => _playerBuffLifeTimeEntityFactory;
    public ICharacterBuffPropertyEntityFactory CharacterBuffPropertyEntityFactory => _characterBuffPropertyEntityFactory;
    public ICharacterBuffLifeTimeEntityFactory CharacterBuffLifeTimeEntityFactory => _characterBuffLifeTimeEntityFactory;

    private Stack<GameContext> _contextStack = new Stack<GameContext>();
    public GameContext Context => _contextStack.Peek();

    public GameContextManager(
        CardLibrary cardLibrary,
        CardBuffLibrary cardBuffLibrary,
        PlayerBuffLibrary playerBuffLibrary,
        CharacterBuffLibrary characterBuffLibrary,
        DispositionLibrary dispositionLibrary,
        LocalizeLibrary localizeLibrary,
        IGameRandom gameRandom,
        ICardPropertyEntityFactory cardPropertyEntityFactory,
        ICardBuffPropertyEntityFactory cardBuffPropertyEntityFactory,
        ICardBuffLifeTimeEntityFactory cardBuffLifeTimeEntityFactory,
        IReactionSessionEntityFactory reactionSessionEntityFactory,
        IPlayerBuffPropertyEntityFactory playerBuffPropertyEntityFactory,
        IPlayerBuffLifeTimeEntityFactory playerBuffLifeTimeEntityFactory,
        ICharacterBuffPropertyEntityFactory characterBuffPropertyEntityFactory,
        ICharacterBuffLifeTimeEntityFactory characterBuffLifeTimeEntityFactory)
    {
        _cardLibrary = cardLibrary;
        _cardBuffLibrary = cardBuffLibrary;
        _playerBuffLibrary = playerBuffLibrary;
        _characterBuffLibrary = characterBuffLibrary;
        _dispositionLibrary = dispositionLibrary;
        _localizeLibrary = localizeLibrary;
        _gameRandom = gameRandom;
        _cardPropertyEntityFactory = cardPropertyEntityFactory;
        _cardBuffPropertyEntityFactory = cardBuffPropertyEntityFactory;
        _cardBuffLifeTimeEntityFactory = cardBuffLifeTimeEntityFactory;
        _reactionSessionEntityFactory = reactionSessionEntityFactory;
        _playerBuffPropertyEntityFactory = playerBuffPropertyEntityFactory;
        _playerBuffLifeTimeEntityFactory = playerBuffLifeTimeEntityFactory;
        _characterBuffPropertyEntityFactory = characterBuffPropertyEntityFactory;
        _characterBuffLifeTimeEntityFactory = characterBuffLifeTimeEntityFactory;
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

}
