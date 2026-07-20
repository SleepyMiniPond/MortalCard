using System;
using System.Collections;
using MortalGame.GameModel;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;
using Rayark.Mast;
using UnityEngine;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;

namespace MortalGame.Presenter
{

    public class GameplayPresenter : IGameplayActionReciever
    {
        private IGameplayView _gameplayView;
        private GameViewModel _gameInfoModel;
        private GameplayManager _gameplayManager;
        private IUIPresenter _uiPresenter;
        private ISubSelectionPresenter _subSelectionPresenter;
        private IGameResultLosePresenter _gameResultLosePresenter;
        private IGameResultWinPresenter _gameResultWinPresenter;

        private readonly Queue<IGameCommand> _pendingGameCommands = new Queue<IGameCommand>();

        public IEnumerable<ISelectableView> SelectableViews => _gameplayView.SelectableViews;
        public ISelectableView BasicSelectableView => _gameplayView.BasicSelectableView;

        public GameplayPresenter(
            GameplayView gameplayView,
            IGameResultWinPanel gameResultWinPanel,
            IGameResultLosePanel gameResultLosePanel,
            GameStageSetting gameStageSetting,
            GameContextManager gameContextManager
        )
        {
            _gameplayView = gameplayView;
            _gameplayManager = new GameplayManager(gameStageSetting, gameContextManager);

            _gameInfoModel = new GameViewModel();
            _gameplayView.Init(_gameInfoModel, this, _gameplayManager, gameContextManager.LocalizeLibrary, gameContextManager.DispositionLibrary);
            _uiPresenter = new UIPresenter(_gameplayView, _gameplayView, _gameInfoModel, gameContextManager.LocalizeLibrary);
            _gameResultWinPresenter = new GameResultWinPresenter(gameResultWinPanel);
            _gameResultLosePresenter = new GameResultLosePresenter(gameResultLosePanel);
            _subSelectionPresenter = new SubSelectionPresenter(_gameInfoModel, gameContextManager.LocalizeLibrary, _gameplayView.SinglePopupPanel, _gameplayView.CardSelectionPanel);
        }

        public async UniTask<GameplayResultCommand> Run(CancellationToken cancellationToken)
        {
            try
            {
                return await _Run(cancellationToken);
            }
            finally
            {
                await _gameplayView.DisposeCharacterViews();
            }
        }

        private async UniTask<GameplayResultCommand> _Run(CancellationToken cancellationToken)
        {
            using var battleCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var battleResult = Option.None<BattleResult>();
            var (battleCompleted, gameplayEventCompleted, uiCompleted) = await UniTask.WhenAll(
                RunAndCancelOthers(RunBattle()),
                RunAndCancelOthers(_GameplayBattleActions(battleCancellation.Token)),
                RunAndCancelOthers(_uiPresenter.Run(battleCancellation.Token)));

            cancellationToken.ThrowIfCancellationRequested();
            if (!battleCompleted)
            {
                throw new InvalidOperationException(
                    $"gameplayEvent Completed?[{gameplayEventCompleted}] UI Completed?[{uiCompleted}] before the battle completed.");
            }

            _gameplayView.DisableAllInteraction();

            if (battleResult.Map(result => result.IsAllyWin).ValueOr(false))
            {
                var winResult = await _gameResultWinPresenter.Run(cancellationToken);
                return new GameplayResultCommand(winResult);
            }
            else
            {
                var loseResult = await _gameResultLosePresenter.Run(cancellationToken);
                return new GameplayResultCommand(loseResult);
            }

            async UniTask RunBattle()
            {
                battleResult = await _gameplayManager.StartBattle(battleCancellation.Token);
            }

            async UniTask<bool> RunAndCancelOthers(UniTask task)
            {
                try
                {
                    await task;
                    return true;
                }
                catch (OperationCanceledException) when (battleCancellation.IsCancellationRequested)
                {
                    return false;
                }
                finally
                {
                    battleCancellation.Cancel();
                }
            }
        }

        public void RecieveEvent(IGameCommand gameCommand)
        {
            Debug.Log($"-- GameplayPresenter.RecieveEvent:[{gameCommand}] --");
            _pendingGameCommands.Enqueue(gameCommand);
        }

        private async UniTask _GameplayBattleActions(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (_pendingGameCommands.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var gameCommand = _pendingGameCommands.Dequeue();
                    await _ProcessGameAction(gameCommand, cancellationToken);
                }

                await UniTask.NextFrame(cancellationToken);

                var events = _gameplayManager.PopAllEvents();
                _gameplayView.Render(events, this);
            }
        }

        private async UniTask _ProcessGameAction(
            IGameCommand gameCommand,
            CancellationToken cancellationToken)
        {
            _gameplayView.DisableAllHandCards();
            var postProcessAction = await _PostProcessAction(gameCommand, cancellationToken);

            postProcessAction.MatchSome(action => _gameplayManager.EnqueueAction(action));
        }

        private async UniTask<Option<IGameAction>> _PostProcessAction(
            IGameCommand gameCommand,
            CancellationToken cancellationToken)
        {
            switch (gameCommand)
            {
                case TurnSubmitCommand turnSubmitCommand:
                    return Option.Some<IGameAction>(new TurnSubmitAction(turnSubmitCommand.Faction));

                case UseCardCommand useCardCommand:
                    var subSelectionOpt = _gameplayManager.QueryCardSubSelectionInfos(useCardCommand.CardIndentity);
                    if (subSelectionOpt.TryGetValue(out var subSelectionInfo))
                    {
                        var subSelectionActions = await _subSelectionPresenter.RunSubSelection(
                            subSelectionInfo,
                            cancellationToken);

                        var action = new UseCardAction(
                            useCardCommand.CardIndentity,
                            useCardCommand.SelectionTarget.Match(
                                some: target => MainSelectionAction.Create(target),
                                none: () => MainSelectionAction.Empty),
                            subSelectionActions);
                        return Option.Some<IGameAction>(action);
                    }
                    else
                    {
                        return Option.None<IGameAction>();
                    }

                default:
                    throw new System.NotImplementedException($"Unhandled game command type: {gameCommand.GetType()}");
            }
        }
    }

}
