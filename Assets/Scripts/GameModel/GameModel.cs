using System;
using System.Collections.Generic;
using Optional;
using UnityEngine;

public interface IGameplayModel
{
    GameStatus GameStatus { get; }
    IGameContextManager ContextManager { get; }
    Option<SubSelectionInfo> QueryCardSubSelectionInfos(Guid cardIdentity);
    IEnumerable<IGameEvent> UpdateReactorSessionAction(IActionUnit actionUnit);
    IEnumerable<IGameEvent> TriggerTiming(GameTiming timing, IActionSource actionSource);
}

public class ClonedGameplayModel : IGameplayModel
{
    private IGameplayModel _baseModel;
    private GameStatus _clonedStatus;

    public GameStatus GameStatus => _clonedStatus;
    public IGameContextManager ContextManager => _baseModel.ContextManager;

    public ClonedGameplayModel(IGameplayModel baseModel, GameStatus clonedStatus)
    {
        _baseModel = baseModel;
        _clonedStatus = clonedStatus;
    }

    public Option<SubSelectionInfo> QueryCardSubSelectionInfos(Guid cardIdentity)
    {
        return _baseModel.QueryCardSubSelectionInfos(cardIdentity);
    }

    public IEnumerable<IGameEvent> UpdateReactorSessionAction(IActionUnit actionUnit)
    {
        return _baseModel.UpdateReactorSessionAction(actionUnit);
    }

    public IEnumerable<IGameEvent> TriggerTiming(GameTiming timing, IActionSource actionSource)
    {
        return _baseModel.TriggerTiming(timing, actionSource);
    }
}