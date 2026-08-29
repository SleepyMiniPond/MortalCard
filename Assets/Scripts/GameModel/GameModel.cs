using System;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;
using UnityEngine;
namespace MortalGame.GameModel
{

    public interface IGameplayModel
    {
        GameStatus GameStatus { get; }
        IGameContextManager ContextManager { get; }
        Option<SubSelectionInfo> QueryCardSubSelectionInfos(Guid cardIdentity);
        IEnumerable<IGameEvent> ObserveRootAction(IActionUnit actionUnit);
        IEnumerable<IGameEvent> ObserveDerivedAction(
            TriggerContext parentContext,
            IActionUnit actionUnit);
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

        public IEnumerable<IGameEvent> ObserveRootAction(IActionUnit actionUnit)
        {
            return _baseModel.ObserveRootAction(actionUnit);
        }

        public IEnumerable<IGameEvent> ObserveDerivedAction(
            TriggerContext parentContext,
            IActionUnit actionUnit)
        {
            return _baseModel.ObserveDerivedAction(parentContext, actionUnit);
        }

        public IEnumerable<IGameEvent> TriggerTiming(GameTiming timing, IActionSource actionSource)
        {
            return _baseModel.TriggerTiming(timing, actionSource);
        }
    }

}
