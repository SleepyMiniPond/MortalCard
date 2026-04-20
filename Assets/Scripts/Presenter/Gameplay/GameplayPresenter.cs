using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;
using Rayark.Mast;
using UnityEngine;

public interface IGameplayActionReciever
{
    void RecieveEvent(IGameCommand gameCommand);

    IEnumerable<ISelectableView> SelectableViews { get; }
    ISelectableView BasicSelectableView { get; }
}

public class GameplayPresenter : IGameplayActionReciever
{
    private IGameplayView _gameplayView;
    private GameViewModel _gameInfoModel;
    private GameplayManager _gameplayManager;
    private IUIPresenter _uiPresenter;
    private ISubSelectionPresenter _subSelectionPresenter;
    private IGameResultLosePresenter _gameResultLosePresenter;
    private IGameResultWinPresenter _gameResultWinPresenter;

    private readonly Queue<UniTask> _pendingActionTasks = new Queue<UniTask>();

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

    public async UniTask<GameplayResultCommand> Run()
    {
        var cts = new CancellationTokenSource();
        
        _GameplayBattleActions(cts).Forget();
        _uiPresenter.Run(cts.Token).Forget();

        var battleResult = await _gameplayManager.StartBattle();

        cts.Cancel();
        _gameplayView.DisableAllInteraction();

        if (battleResult.Map(result => result.IsAllyWin).ValueOr(false))
        {
            var winResult = await _gameResultWinPresenter.Run();
            return new GameplayResultCommand(winResult);
        }
        else
        {
            var loseResult = await _gameResultLosePresenter.Run();
            return new GameplayResultCommand(loseResult);
        }
    }

    public void RecieveEvent(IGameCommand gameCommand)
    {
        Debug.Log($"-- GameplayPresenter.RecieveEvent:[{gameCommand}] --");
        _pendingActionTasks.Enqueue(_ProcessGameAction(gameCommand));
    }

    private async UniTask<BattleResult> _GameplayBattleActions(CancellationTokenSource cts)
    {
        while (true)
        {
            while (_pendingActionTasks.Count > 0)
            {
                var actionTask = _pendingActionTasks.Dequeue();
                await actionTask;
            }
            
            await UniTask.Yield();

            var events = _gameplayManager.PopAllEvents();
            _gameplayView.Render(events, this);
        }
    }

    private async UniTask _ProcessGameAction(IGameCommand gameCommand)
    {        
        _gameplayView.DisableAllHandCards();
        var postProcessAction = await _PostProcessAction(gameCommand);

        postProcessAction.MatchSome(action => _gameplayManager.EnqueueAction(action));
    }

    private async UniTask<Option<IGameAction>> _PostProcessAction(IGameCommand gameCommand)
    {
        switch (gameCommand)
        {
            case TurnSubmitCommand turnSubmitCommand:
                return Option.Some<IGameAction>(new TurnSubmitAction(turnSubmitCommand.Faction));

            case UseCardCommand useCardCommand:
                var subSelectionOpt = _gameplayManager.QueryCardSubSelectionInfos(useCardCommand.CardIndentity);
                if (subSelectionOpt.TryGetValue(out var subSelectionInfo))
                {
                    var subSelectionActions = await _subSelectionPresenter.RunSubSelection(subSelectionInfo);

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
