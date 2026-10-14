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
        private ICardSelectionFlow _cardSelectionFlow;
        private IWarningPopupPresenter _warningPopupPresenter;
        private IGameResultLosePresenter _gameResultLosePresenter;
        private IGameResultWinPresenter _gameResultWinPresenter;

        private sealed record PendingGameCommand(IGameCommand Command);
        private readonly Queue<PendingGameCommand> _pendingGameCommands = new();

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
            _cardSelectionFlow = new CardSelectionFlow(new SubSelectionPresenter(
                _gameInfoModel, gameContextManager.LocalizeLibrary,
                _gameplayView.SinglePopupPanel, _gameplayView.CardSelectionPanel));
            _warningPopupPresenter = new WarningPopupPresenter(_gameplayView.WarningPopupPanel);
        }

        public async UniTask<GameplayResultCommand> Run(CancellationToken cancellationToken)
        {
            try
            {
                return await _Run(cancellationToken);
            }
            finally
            {
                _gameplayView.DisableAllInteraction();
                await _gameplayView.DisposeCharacterViews();
            }
        }

        private async UniTask<GameplayResultCommand> _Run(CancellationToken cancellationToken)
        {
            using var battleCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var token = battleCancellation.Token;
            var battleResult = Option.None<BattleResult>();
            try
            {
                _gameplayView.DisableAllInteraction();
                // 所有戰鬥工作共用同一個生命週期，取消後等待各自清理完成。
                var (battleError, commandError, renderError, uiError) = await UniTask.WhenAll(
                    _RunBattleFlow(RunBattle, battleCancellation, "戰鬥", completesBattle: true),
                    _RunBattleFlow(() => _ProcessCommands(token), battleCancellation, "玩家操作"),
                    _RunBattleFlow(() => _RenderEvents(token), battleCancellation, "事件呈現"),
                    _RunBattleFlow(() => _uiPresenter.Run(token), battleCancellation, "UI"));

                var error = battleError ?? commandError ?? renderError ?? uiError;
                if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                _pendingGameCommands.Clear();
                _gameplayView.DisableAllInteraction();
            }

            if (!battleResult.TryGetValue(out var completedBattleResult))
                throw new InvalidOperationException("戰鬥已結束，但 GameplayManager 未提供 BattleResult。");

            if (completedBattleResult.IsAllyWin)
            {
                var winResult = await _gameResultWinPresenter.Run(cancellationToken);
                return new GameplayResultCommand(
                    winResult,
                    completedBattleResult.CardInstanceChanges);
            }
            else
            {
                var loseResult = await _gameResultLosePresenter.Run(cancellationToken);
                return new GameplayResultCommand(
                    loseResult,
                    completedBattleResult.CardInstanceChanges);
            }

            async UniTask RunBattle()
            {
                battleResult = await _gameplayManager.StartBattle(battleCancellation.Token);
            }

        }

        private static async UniTask<Exception> _RunBattleFlow(
            Func<UniTask> run,
            CancellationTokenSource lifetime,
            string name,
            bool completesBattle = false)
        {
            try
            {
                await run();
                if (!completesBattle && !lifetime.IsCancellationRequested)
                    return new InvalidOperationException($"{name}流程在戰鬥完成前提早結束。");
                return null;
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { return null; }
            catch (Exception error) { return error; }
            finally { lifetime.Cancel(); }
        }

        public void RecieveEvent(IGameCommand gameCommand)
        {
            if (_pendingGameCommands.Count > 0 || _gameplayManager.BattleResult.HasValue) return;
            Debug.Log($"-- GameplayPresenter.RecieveEvent:[{gameCommand}] --");
            _pendingGameCommands.Enqueue(new PendingGameCommand(gameCommand));
        }

        private async UniTask _ProcessCommands(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (_pendingGameCommands.Count == 0)
                {
                    await UniTask.NextFrame(token);
                    continue;
                }
                var pending = _pendingGameCommands.Peek();
                await _ProcessGameAction(pending.Command, token);
                // 正式提交後由對應的 GameEvent 移除；每次排入均有獨立項目，避免重用指令時誤等下一次操作。
                await UniTask.WaitUntil(() => _pendingGameCommands.Count == 0 ||
                    !ReferenceEquals(_pendingGameCommands.Peek(), pending), cancellationToken: token);
            }
        }

        private async UniTask _RenderEvents(CancellationToken token)
        {
            while (true)
            {
                await UniTask.NextFrame(token);
                foreach (var gameEvent in _gameplayManager.PopAllEvents())
                {
                    token.ThrowIfCancellationRequested();
                    _gameplayView.Render(new[] { gameEvent }, this);
                    if (gameEvent is CardPlayRejectedEvent { Faction: Faction.Ally } &&
                        !_gameplayManager.BattleResult.HasValue)
                    {
                        _gameplayView.DisableAllInteraction();
                        await _ShowSelectionFailure(token);
                    }
                    _CompletePendingCommand(gameEvent);

                    if (_pendingGameCommands.Count > 0 ||
                        _gameplayManager.BattleResult.HasValue)
                        _gameplayView.DisableAllInteraction();
                }
            }
        }

        private void _CompletePendingCommand(IGameEvent gameEvent)
        {
            if (!_pendingGameCommands.TryPeek(out var pending)) return;
            switch (pending.Command, gameEvent)
            {
                case (UseCardCommand command, PlayerExecuteStartEvent { Faction: Faction.Ally } started)
                    when started.CompletedCardIdentity.TryGetValue(out var cardIdentity) &&
                         command.CardIndentity == cardIdentity:
                case (TurnSubmitCommand, PlayerExecuteEndEvent { Faction: Faction.Ally }):
                    _pendingGameCommands.Dequeue();
                    break;
            }
        }

        private async UniTask _ProcessGameAction(IGameCommand gameCommand, CancellationToken token)
        {
            _gameplayView.DisableAllInteraction();
            var postProcessAction = await _PostProcessAction(gameCommand, token);
            token.ThrowIfCancellationRequested();
            if (!postProcessAction.TryGetValue(out var action))
            {
                _pendingGameCommands.Dequeue();
                _RestorePlayerInteraction(token);
                return;
            }

            _gameplayManager.EnqueueAction(action);
        }

        private void _RestorePlayerInteraction(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_gameplayManager.BattleResult.HasValue)
                return;

            var model = (IGameplayModel)_gameplayManager;
            _gameplayView.RestorePlayerInteraction(
                model.GameStatus.Ally.CardManager.ToInfo(),
                model.GameStatus.Ally.CardManager.HandCard.ToCardCollectionInfo(model));
        }

        private UniTask _ShowSelectionFailure(
            CancellationToken cancellationToken, CardSelectionFailure failure = null)
        {
            var key = failure?.Reason == CardSelectionFailureReason.InsufficientCandidates
                ? "CardSelection_InsufficientCandidates" 
                : "CardSelection_Rejected";
            var library = ((IGameplayModel)_gameplayManager).ContextManager.LocalizeLibrary;
            var message = library.Get(LocalizeInfoType.UI, key).Info;
            return _warningPopupPresenter.Run(message, cancellationToken);
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
                    var mainSelectionAction = useCardCommand.SelectionTarget.Match(
                        some: target => MainSelectionAction.Create(target),
                        none: () => MainSelectionAction.Empty);
                    var model = (IGameplayModel)_gameplayManager;
                    if (model.GetCard(useCardCommand.CardIndentity).TryGetValue(out var card))
                    {
                        var selectionDraft = CardSelectionDraft.Create(
                            card, model.GameStatus.Ally, mainSelectionAction);
                        var result = await _cardSelectionFlow.RunSubSelection(
                            model,
                            selectionDraft,
                            cancellationToken);
                        if (result is CardSelectionPresentationResult.Completed completed)
                            return Option.Some<IGameAction>(new UseCardAction(useCardCommand.CardIndentity,
                                mainSelectionAction, completed.Draft.SelectedGroups));
                        if (result is CardSelectionPresentationResult.Cancelled)
                            return Option.None<IGameAction>();
                        if (result is CardSelectionPresentationResult.Rejected rejected)
                        {
                            await _ShowSelectionFailure(cancellationToken, rejected.Failure);
                            return Option.None<IGameAction>();
                        }
                    }
                    await _ShowSelectionFailure(cancellationToken);
                    return Option.None<IGameAction>();

                default:
                    throw new NotImplementedException($"Unhandled game command type: {gameCommand.GetType()}");
            }
        }
    }

}
