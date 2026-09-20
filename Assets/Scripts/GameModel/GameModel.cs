using System;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;
namespace MortalGame.GameModel
{

    public interface IGameplayModel
    {
        GameStatus GameStatus { get; }
        IGameContextManager ContextManager { get; }
        EffectResult RunEffectBatch(IEnumerable<EffectQueueItem> items);
        void EnqueueCardPlay(CardPlayRequest request);
        Option<SubSelectionInfo> QueryCardSubSelectionInfos(
            Guid cardIdentity,
            MainSelectionAction mainSelectionAction);
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

        public EffectResult RunEffectBatch(IEnumerable<EffectQueueItem> items)
            => _baseModel.RunEffectBatch(items);

        public void EnqueueCardPlay(CardPlayRequest request)
            => _baseModel.EnqueueCardPlay(request);

        public ClonedGameplayModel(IGameplayModel baseModel, GameStatus clonedStatus)
        {
            _baseModel = baseModel;
            _clonedStatus = clonedStatus;
        }

        public Option<SubSelectionInfo> QueryCardSubSelectionInfos(
            Guid cardIdentity,
            MainSelectionAction mainSelectionAction)
        {
            return _baseModel.QueryCardSubSelectionInfos(cardIdentity, mainSelectionAction);
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
