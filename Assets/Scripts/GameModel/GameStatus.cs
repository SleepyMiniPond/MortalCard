using System;
using System.Collections.Generic;
using System.Linq;
using OneOf;
using Optional;
using Rayark.Mast;
using UniRx;

public enum GameState
{
    None = 0,
    GameStart,
    TurnStart,
    DrawCard,
    EnemyPrepare,
    PlayerPrepare,
    PlayerExecute,
    EnemyExecute,
    TurnEnd,
    GameEnd,
}

public class GameStatus
{
    private int _turnCount;
    private AllyEntity _ally;
    private EnemyEntity _enemy;
    private IReactiveProperty<Option<Guid>> _currentPlayer = new ReactiveProperty<Option<Guid>>(Option.None<Guid>());

    public int TurnCount => _turnCount;
    public AllyEntity Ally => _ally;
    public EnemyEntity Enemy => _enemy;
    public IReadOnlyReactiveProperty<Option<IPlayerEntity>> CurrentPlayer 
        => _currentPlayer
            .Select(value => value
                .FlatMap(id => this.GetPlayer(id)))
            .ToReactiveProperty();
    public IReadOnlyReactiveProperty<Option<IPlayerEntity>> OppositePlayer 
        => CurrentPlayer
            .Select(current => current
                .Map(player => player.Faction == Faction.Ally 
                    ? (IPlayerEntity)Enemy 
                    : (IPlayerEntity)Ally))
            .ToReactiveProperty();
    public UnityEngine.Random.State RandomState => UnityEngine.Random.state;

    public GameStatus()
    {
        _turnCount = 0;
    }
    
    public void SummonAlly(AllyEntity ally)
    {
        _ally = ally;
    }
    public void SummonEnemy(EnemyEntity enemy)
    {
        _enemy = enemy;
    }

    public IDisposable SetCurrentPlayer(IPlayerEntity player)
    {
        _currentPlayer.Value = player.Identity.Some();

        return Disposable.Create(() => _currentPlayer.Value = Option.None<Guid>());
    }

    public void SetNewTurn()
    {
        _turnCount++;
    }
}

public enum LevelMapStatus
{
    None,
    Walk,
    Battle,
    Shop,
    Leave
}
public enum LevelMapReactionType
{
    StartGamePlay,
    Finish,
    Fail,
    Restart
}

public enum LoseReactionType
{
    Quit,
    Restart,
    Retry
}


public record BattleResult(bool IsAllyWin);

public record GameplayResultCommand(
    GameplayResult Result);

public abstract record GameplayResult;
public record GameplayWinResult() : GameplayResult;
public record GameplayLoseResult(
    LoseReactionType ReactionType) : GameplayResult;

public record LevelMapCommand(
    LevelMapReactionType ReactionType);