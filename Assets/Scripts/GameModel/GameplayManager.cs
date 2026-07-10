using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cysharp.Threading.Tasks;
using Optional;
using MortalGame.GameModel;

public interface IGameEventWatcher : IGameplayModel
{
    event Action OnUseCard;
    event Action OneTurnStart;
    event Action OnTurnEnd;
}

public class UniTaskAwaitableQueue<T>
{ 
    private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

    public void Enqueue(T task)
    {
        _queue.Enqueue(task);
    }

    public async UniTask<T> Dequeue()
    {
        while (true)
        {
            if (_queue.TryDequeue(out var result))
            {
                return result;
            }
            
            await UniTask.Yield();
        }
    }
    
    public void Clear()
    {
        _queue.Clear();
    }
}

public class GameplayManager : IGameplayModel, IGameEventWatcher
{
    public event Action OnUseCard;
    public event Action OneTurnStart;
    public event Action OnTurnEnd;

    public class GameEndException : Exception
    {
        public readonly bool IsAllyWin;
        public GameEndException(bool isAllyWin) : base() 
        {
            IsAllyWin = isAllyWin;
        }
    }

    private GameStageSetting _gameStageSetting;
    private GameStatus _gameStatus;
    private Option<BattleResult> _battleResult;
    private List<IGameEvent> _gameEvents;
    private UniTaskAwaitableQueue<IGameAction> _gameActions;
    private IGameContextManager _contextMgr;
    private GameHistory _gameHistory;

    public Option<BattleResult> BattleResult { get { return _battleResult; } }
    GameStatus IGameplayModel.GameStatus { get { return _gameStatus; } }
    IGameContextManager IGameplayModel.ContextManager { get { return _contextMgr; } }

    public GameplayManager(GameStageSetting gameStageSetting, GameContextManager contextManager)
    {
        // TODO split gamestatus and gamesnapshot and gameparams
        _gameStageSetting = gameStageSetting;
        _gameStatus = new GameStatus();
        _contextMgr = contextManager;
        _gameHistory = new GameHistory(this);
    }

    internal GameplayManager(
        GameStageSetting gameStageSetting,
        GameContextManager contextManager,
        GameStatus initialStatus)
        : this(gameStageSetting, contextManager)
    {
        _gameStatus = initialStatus;
    }

    public async UniTask<Option<BattleResult>> StartBattle()
    {
        _gameEvents = new List<IGameEvent>();
        _gameActions = new UniTaskAwaitableQueue<IGameAction>();
        _battleResult = Option.None<BattleResult>();

        await _Run();

        return _battleResult;
    }

    public void EnqueueAction(IGameAction action)
    {
        _gameActions.Enqueue(action);
    }

    public IReadOnlyCollection<IGameEvent> PopAllEvents()
    {
        var events = _gameEvents.ToArray();
        _gameEvents.Clear();
        return events;
    }

    public Option<SubSelectionInfo> QueryCardSubSelectionInfos(Guid cardIdentity)
    {
        return CardEntityExtensions
            .GetCard(this, cardIdentity)
            .Map(cardEntity => { 
                var cardData = _contextMgr.CardLibrary.GetCardData(cardEntity.CardDataId);
                return cardData.SubSelects.ToInfo(this, cardEntity);
            });
    }

    private async UniTask _Run()
    {
        _GameStart();

        try
        {
            while (true)
            {
                _TurnStart();
                
                _TurnDrawCard();
                
                _EnemyPrepare();    

                await _PlayerExecute();
                
                _EnemyExecute();
                
                _TurnEnd();
            }
        }
        catch (GameEndException gameEndEx)
        {
            _battleResult = new BattleResult(gameEndEx.IsAllyWin).Some();
        }
    }

    private void _GameStart()
    {
        _gameStatus.SummonAlly(_ParseAlly(_gameStageSetting.Ally, _contextMgr));
        _gameEvents.Add(new AllySummonEvent(_gameStatus.Ally));

        _gameStatus.SummonEnemy(_ParseEnemy(_gameStageSetting.Enemy,  _contextMgr));
        _gameEvents.Add(new EnemySummonEvent(_gameStatus.Enemy));  

        var createAllyDeckResult = EffectManager.CreateNewDeckCard(
            this,
            SystemSource.Instance,
            _gameStatus.Ally,
            _gameStageSetting.Ally.Deck);
        _gameEvents.AddRange(createAllyDeckResult.Events);

        var createEnemyDeckResult = EffectManager.CreateNewDeckCard(
            this,
            SystemSource.Instance,
            _gameStatus.Enemy,
            _gameStageSetting.Enemy.PlayerData.Deck.Cards
                .Select(c => CardInstance.Create(c.Data))
                .ToList());
        _gameEvents.AddRange(createEnemyDeckResult.Events);

        _gameEvents.AddRange(
            UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.GameStart, new SystemSource())));

        AllyEntity _ParseAlly(AllyInstance allyInstance, IGameContextManager gameContextManager)
        {
            var characterRecord = new CharacterParameter
            {
                NameKey         = allyInstance.NameKey,
                CurrentHealth   = allyInstance.CurrentHealth,
                MaxHealth       = allyInstance.MaxHealth
            };

            return new AllyEntity(
                originPlayerInstanceGuid: allyInstance.Identity,
                characterParams: characterRecord.WrapAsEnumerable().ToArray(),
                currentEnergy: allyInstance.CurrentEnergy,
                maxEnergy: allyInstance.MaxEnergy,
                handCardMaxCount: allyInstance.HandCardMaxCount,
                currentDisposition: allyInstance.CurrentDisposition,
                maxDisposition: gameContextManager.DispositionLibrary.MaxDisposition,
                gameContext: gameContextManager
            );
        }

        EnemyEntity _ParseEnemy(EnemyData enemyData, IGameContextManager gameContextManager)
        {
            var characterRecord = new CharacterParameter
            {
                NameKey         = enemyData.PlayerData.NameKey,
                CurrentHealth   = enemyData.PlayerData.InitialHealth,
                MaxHealth       = enemyData.PlayerData.MaxHealth
            };

            return new EnemyEntity(
                characterParams: new[] { characterRecord },
                currentEnergy: enemyData.PlayerData.InitialEnergy,
                maxEnergy: enemyData.PlayerData.MaxEnergy,
                handCardMaxCount: enemyData.PlayerData.HandCardMaxCount,
                selectedCardMaxCount: enemyData.SelectedCardMaxCount,
                turnStartDrawCardCount: enemyData.TurnStartDrawCardCount,
                energyRecoverPoint: enemyData.EnergyRecoverPoint,
                gameContext: gameContextManager
            );
        }
    }

    private void _TurnStart()
    {
        _gameEvents.AddRange(
            UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.TurnStart, new SystemSource())));
        
        _gameStatus.SetNewTurn();
        _gameEvents.Add(new RoundStartEvent(
            Round: _gameStatus.TurnCount,
            Player: _gameStatus.Ally,
            Enemy: _gameStatus.Enemy
        ));

        var recoverEnergyPoint = _contextMgr.DispositionLibrary.GetRecoverEnergyPoint(_gameStatus.Ally.DispositionManager.CurrentDisposition);
        var allyGainEnergyResult = _gameStatus.Ally.EnergyManager.RecoverEnergy(recoverEnergyPoint);
        _gameEvents.Add(new GainEnergyEvent(_gameStatus.Ally.Faction, _gameStatus.Ally.EnergyManager.ToInfo(), allyGainEnergyResult));

        var enemyGainEnergyResult = _gameStatus.Enemy.EnergyManager.RecoverEnergy(_gameStatus.Enemy.EnergyRecoverPoint);
        _gameEvents.Add(new GainEnergyEvent(_gameStatus.Enemy.Faction, _gameStatus.Enemy.EnergyManager.ToInfo(), enemyGainEnergyResult));

        _CheckGameEnd();
    }

    private void _TurnDrawCard()
    {
        _gameEvents.AddRange(
            UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.DrawCard, new SystemSource())));
        
        var allyDrawCount = _contextMgr.DispositionLibrary.GetDrawCardCount(_gameStatus.Ally.DispositionManager.CurrentDisposition);
        var enemyDrawCount = _gameStatus.Enemy.TurnStartDrawCardCount;

        var allyDrawEvents = EffectManager.DrawCards(this, SystemSource.Instance, _gameStatus.Ally, allyDrawCount);
        _gameEvents.AddRange(allyDrawEvents.Events);

        var enemyDrawEvents = EffectManager.DrawCards(this, SystemSource.Instance, _gameStatus.Enemy, enemyDrawCount);
        _gameEvents.AddRange(enemyDrawEvents.Events);

        var triggerEvts = _TriggerTiming(GameTiming.DrawCard, SystemSource.Instance);
        _gameEvents.AddRange(triggerEvts);
   
        _CheckGameEnd();
    }

    private void _EnemyPrepare()
    {
        while (_gameStatus.Enemy.TryGetRecommandSelectCard(this, out var recommendCard))
        {
            _gameEvents.Add(new EnemySelectCardEvent(
                SelectedCardInfo: recommendCard.ToInfo(this),
                SelectedCards: _gameStatus.Enemy.SelectedCards.Cards.Select(card => card.Identity).ToImmutableArray()
            ));
        }
        
        _CheckGameEnd();
    }

    public async UniTask _PlayerExecute()
    {
        using var allyStatus = _gameStatus.SetCurrentPlayer(_gameStatus.Ally);

        _gameEvents.Add(new PlayerExecuteStartEvent(
            Faction: _gameStatus.Ally.Faction,
            CardManagerInfo: _gameStatus.Ally.CardManager.ToInfo(this),
            HandCardInfo: _gameStatus.Ally.CardManager.HandCard.ToCardCollectionInfo(this)
        ));
        
        var isExecuting = true;
        while (isExecuting)
        {
            var action = await _gameActions.Dequeue();

            switch (action)
            {
                case UseCardAction useCardAction:
                    using (_SetUseCardSelectTarget(useCardAction))
                    {
                        _UseCard(_gameStatus.Ally, useCardAction.CardIndentity);
                        _gameEvents.Add(new PlayerExecuteStartEvent(
                            Faction: _gameStatus.Ally.Faction,
                            CardManagerInfo: _gameStatus.Ally.CardManager.ToInfo(this),
                            HandCardInfo: _gameStatus.Ally.CardManager.HandCard.ToCardCollectionInfo(this)
                        ));
                    }
                    break;

                case TurnSubmitAction turnSubmitAction:
                    isExecuting = false;
                    _FinishPlayerExecuteTurn();
                    break;
            }
            
            _CheckGameEnd();
        }
    }
    private void _EnemyExecute()
    {
        _gameEvents.AddRange(
            UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.ExecuteStart, new SystemSource())));

        using var enemyStatus = _gameStatus.SetCurrentPlayer(_gameStatus.Enemy);
        while (_gameStatus.Enemy.TryGetNextUseCardAction(this, out var useCardAction))
        {
            using (_SetUseCardSelectTarget(useCardAction))
            {
                _UseCard(_gameStatus.Enemy, useCardAction.CardIndentity);
                _gameEvents.Add(new PlayerExecuteStartEvent(
                    Faction: _gameStatus.Enemy.Faction,
                    CardManagerInfo: _gameStatus.Enemy.CardManager.ToInfo(this),
                    HandCardInfo: _gameStatus.Ally.CardManager.HandCard.ToCardCollectionInfo(this)
                ));
            }
            
            _CheckGameEnd();
        }

        var unselectedCards = _gameStatus.Enemy.SelectedCards.UnSelectAllCards();
        _gameEvents.Add(new EnemyUnselectedCardEvent(UnselectedCards: unselectedCards.Select(c => c.Identity).ToImmutableArray()));

        _FinishEnemyExecuteTurn();
    }

    private void _FinishPlayerExecuteTurn()
    {
        _gameEvents.Add(new PlayerExecuteEndEvent(
            Faction: _gameStatus.Ally.Faction,
            CardManagerInfo: _gameStatus.Ally.CardManager.ToInfo(this)
        ));

        var endTurnSource = new SystemExectueEndSource(_gameStatus.Ally);
        var triggerEvts = _TriggerTiming(GameTiming.ExecuteEnd, endTurnSource);
        _gameEvents.AddRange(triggerEvts);

        _gameEvents.AddRange(
            UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.ExecuteEnd, new SystemSource())));

        _gameActions.Clear();
    }
    private void _FinishEnemyExecuteTurn()
    {
        var endTurnSource = new SystemExectueEndSource(_gameStatus.Enemy);
        var triggerEvts = _TriggerTiming(GameTiming.ExecuteEnd, endTurnSource);
        _gameEvents.AddRange(triggerEvts);

        _gameEvents.AddRange(
            UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.ExecuteEnd, new SystemSource())));

        _gameActions.Clear();
    }

    private void _TurnEnd()
    {
        _gameEvents.AddRange(
            _gameStatus.Ally.CardManager.ClearHandOnTurnEnd(this));
        _gameEvents.AddRange(
            _gameStatus.Enemy.CardManager.ClearHandOnTurnEnd(this));

        _gameEvents.AddRange(
            UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.TurnEnd, new SystemSource())));

        var triggerEvts = _TriggerTiming(GameTiming.TurnEnd, SystemSource.Instance);
        _gameEvents.AddRange(triggerEvts);
        
        _CheckGameEnd();
    }

    private IGameContextManager _SetUseCardSelectTarget(UseCardAction useCardAction)
    {
        switch (useCardAction.MainSelectionAction.TargetType)
        {
            case TargetType.AllyCharacter:
            case TargetType.EnemyCharacter:
                var enemyCharacterOpt = useCardAction.MainSelectionAction.SelectedTarget
                    .FlatMap(enemyCharacterIdentity => this.GetCharacter(enemyCharacterIdentity));
                return _contextMgr.SetSelectedCharacter(enemyCharacterOpt);
            case TargetType.AllyCard:
            case TargetType.EnemyCard:
                var enemyCardOpt = useCardAction.MainSelectionAction.SelectedTarget
                    .FlatMap(enemyCardIndentity => this.GetCard(enemyCardIndentity));
                return _contextMgr.SetSelectedCard(enemyCardOpt);
            default:
            case TargetType.None:
                return _contextMgr.SetClone();
        }
    }
    private void _UseCard(IPlayerEntity player, Guid CardIndentity)
    {
        var usedCard = player.CardManager.HandCard.Cards.FirstOrDefault(c => c.Identity == CardIndentity);
        if (usedCard != null &&
            !usedCard.HasProperty(CardProperty.Sealed))
        {
            var useCardEvents = new List<IGameEvent>();

            var useCardContext = new TriggerContext(this, new CardTrigger(usedCard), new CardLookIntentAction(usedCard));
            var cardRuntimCost = GameFormula.CardCost(useCardContext, usedCard);
            if (cardRuntimCost <= player.CurrentEnergy)
            {
                var loseEnergyCommand = new LoseEnergyEffectCommand(player, cardRuntimCost);
                var loseEnergyResult = player.EnergyManager.ConsumeEnergy(cardRuntimCost);
                useCardEvents.Add(new LoseEnergyEvent(player.Faction, player.EnergyManager.ToInfo(), loseEnergyResult));

                var (isSuccess, playCardDisposable) = player.CardManager.TryPlayCard(usedCard, out int handCardIndex, out int handCardsCount);
                if (isSuccess)
                {
                    var cardPlaySource = new CardPlaySource(usedCard, handCardIndex, handCardsCount, loseEnergyCommand, new CardPlayAttributeEntity());
                    var cardPlayTrigger = new CardPlayTrigger(cardPlaySource);
                    var cardPlayIntent = new CardPlayIntentAction(cardPlaySource);
                    var cardPlayTriggerContext = new TriggerContext(this, cardPlayTrigger, cardPlayIntent);
                    var cardPlayResultSource = null as CardPlayResultSource;

                    using (playCardDisposable)
                    {
                        // Create PlayCardSession
                        useCardEvents.AddRange(UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.PlayCardStart, new SystemSource())));

                        useCardEvents.AddRange(UpdateReactorSessionAction(cardPlayIntent));

                        //TODO: check and remove expired buffs
                        //      trigger events while remove buffs

                        useCardEvents.AddRange(_TriggerTiming(GameTiming.PlayCardStart, cardPlaySource));

                        var effectActionResults = new List<BaseResultAction>();

                        var repeatTimes = usedCard.HasProperty(CardProperty.EffectRepeat) ?
                            1 : Math.Max(1, usedCard.GetCardProperty(cardPlayTriggerContext, CardProperty.EffectRepeat));
                        for (int i = 0; i < repeatTimes; i++)
                        {
                            var effectQueueRunner = new EffectQueueRunner();
                            foreach (var effect in usedCard.Effects)
                            {
                                effectQueueRunner.EnqueueCardEffect(cardPlayTriggerContext, effect);
                            }

                            var effectResult = effectQueueRunner.RunToCompletion();
                            useCardEvents.AddRange(effectResult.Events);
                            effectActionResults.AddRange(effectResult.Actions);
                        }

                        cardPlayResultSource = cardPlaySource.CreateResultSource(effectActionResults);

                        var usedCardInfo = usedCard.ToInfo(this);
                        var usedCardEvent = new UsedCardEvent(
                            Faction: player.Faction,
                            UsedCardInfo: usedCardInfo,
                            CardManagerInfo: player.CardManager.ToInfo(this));
                        useCardEvents.Add(usedCardEvent);
                    }

                    if(usedCard.HasProperty(CardProperty.Recycle))
                    {
                        var recycleResult = EffectManager.RecycleCardOnPlayEnd(this, player, usedCard);
                        useCardEvents.AddRange(recycleResult.Events);
                    }

                    useCardEvents.AddRange(_TriggerTiming(GameTiming.PlayCardEnd, cardPlayResultSource));

                    useCardEvents.AddRange(
                        UpdateReactorSessionAction(new CardPlayResultAction(cardPlayResultSource)));

                    // Close PlayCardSession
                    useCardEvents.AddRange(
                        UpdateReactorSessionAction(new UpdateTimingAction(GameTiming.PlayCardEnd, new SystemSource())));
                }
            }

            OnUseCard?.Invoke(); // pass record to History

            _gameEvents.AddRange(useCardEvents);
        }
    }

    public IEnumerable<IGameEvent> UpdateReactorSessionAction(IActionUnit actionUnit)
    {
        var allyEvt = _gameStatus.Ally.Update(new TriggerContext(this, new PlayerTrigger(_gameStatus.Ally), actionUnit));
        var enemyEvt = _gameStatus.Enemy.Update(new TriggerContext(this, new PlayerTrigger(_gameStatus.Enemy), actionUnit));

        return new List<IGameEvent> { allyEvt, enemyEvt };
    }

    public IEnumerable<IGameEvent> TriggerTiming(GameTiming timing, IActionSource actionSource)
    {
        return _TriggerTiming(timing, actionSource);
    }

    internal IGameContextManager EffectQueueContextManager => _contextMgr;

    // TODO: collect reactionEffects created from reactionSessions
    private IEnumerable<IGameEvent> _TriggerTiming(GameTiming timing, IActionSource actionSource)
    {
        var effectQueueRunner = new EffectQueueRunner();
        effectQueueRunner.Enqueue(new TriggerTimingQueueItem(this, timing, actionSource));
        return effectQueueRunner.RunToCompletion().Events;
    }

    internal IReadOnlyList<EffectQueueItem> CreateTriggerTimingQueueItems(GameTiming timing, IActionSource actionSource)
    {
        var timingAction = new UpdateTimingAction(timing, actionSource);

        return PlayerQueueItems(_gameStatus.Ally)
            .Concat(PlayerQueueItems(_gameStatus.Enemy))
            .ToArray();

        IEnumerable<EffectQueueItem> PlayerQueueItems(IPlayerEntity player)
        {
            return PlayerBuffQueueItems(player)
                .Concat(CharacterBuffQueueItems(player))
                .Concat(CardBuffQueueItems(player));
        }

        IEnumerable<EffectQueueItem> PlayerBuffQueueItems(IPlayerEntity player)
        {
            return player.BuffManager.Buffs
                .SelectMany(buff => AddPlayerBuffQueueItems(buff, timing, timingAction));
        }

        IEnumerable<EffectQueueItem> CharacterBuffQueueItems(IPlayerEntity player)
        {
            return player.Characters
                .SelectMany(character => character.BuffManager.Buffs
                    .SelectMany(buff => AddCharacterBuffQueueItems(character, buff, timing, timingAction)));
        }

        IEnumerable<EffectQueueItem> CardBuffQueueItems(IPlayerEntity player)
        {
            return player.CardManager.AllCards()
                .SelectMany(card => card.BuffManager.Buffs
                    .SelectMany(buff => AddCardBuffQueueItems(card, buff, timing, timingAction)));
        }

        IEnumerable<EffectQueueItem> AddPlayerBuffQueueItems(
            IPlayerBuffEntity buff,
            GameTiming timing,
            UpdateTimingAction timingAction)
        {
            var buffTrigger = new PlayerBuffTrigger(buff);
            var buffTriggerContext = new TriggerContext(this, buffTrigger, timingAction);
            var conditionalEffectsOpt = _contextMgr.PlayerBuffLibrary.GetBuffEffects(buff.PlayerBuffDataId, timing);
            return conditionalEffectsOpt
                .Map(conditionalEffects => conditionalEffects
                    .Where(conditionalEffect => conditionalEffect.Conditions.All(c => c.Eval(buffTriggerContext)))
                    .Select(conditionalEffect => 
                        new TriggeredPlayerBuffEffectQueueItem(
                            this,
                            buffTriggerContext,
                            conditionalEffect.Effect,
                            new PlayerBuffSource(buff)) as EffectQueueItem)
                    .ToArray())
                .ValueOr(Array.Empty<EffectQueueItem>());
        }

        IEnumerable<EffectQueueItem> AddCharacterBuffQueueItems(
            ICharacterEntity selectedCharacter,
            ICharacterBuffEntity buff,
            GameTiming timing,
            UpdateTimingAction timingAction)
        {
            using var characterContext = _contextMgr.SetSelectedCharacter(selectedCharacter.Some());
            var buffTrigger = new CharacterBuffTrigger(buff);
            var buffTriggerContext = new TriggerContext(this, buffTrigger, timingAction);
            var conditionalEffectsOpt = _contextMgr.CharacterBuffLibrary.GetBuffEffects(buff.CharacterBuffDataId, timing);
            return conditionalEffectsOpt
                .Map(conditionalEffects => conditionalEffects
                    .Where(conditionalEffect => conditionalEffect.Conditions.All(c => c.Eval(buffTriggerContext)))
                    .Select(conditionalEffect =>
                        new TriggeredCharacterBuffEffectQueueItem(
                            this,
                            selectedCharacter,
                            buffTriggerContext,
                            conditionalEffect.Effect,
                            new CharacterBuffSource(buff)) as EffectQueueItem)
                    .ToArray())
                .ValueOr(Array.Empty<EffectQueueItem>());
        }

        IEnumerable<EffectQueueItem> AddCardBuffQueueItems(
            ICardEntity selectedCard,
            ICardBuffEntity buff,
            GameTiming timing,
            UpdateTimingAction timingAction)
        {
            using var cardContext = _contextMgr.SetSelectedCard(selectedCard.Some());
            var cardBuffTrigger = new CardBuffTrigger(buff);
            var buffTriggerContext = new TriggerContext(this, cardBuffTrigger, timingAction);
            var conditionalEffectsOpt = _contextMgr.CardBuffLibrary.GetBuffEffects(buff.CardBuffDataID, timing);
            return conditionalEffectsOpt
                .Map(conditionalEffects => conditionalEffects
                    .Where(conditionalEffect => conditionalEffect.Conditions.All(c => c.Eval(buffTriggerContext)))
                    .Select(conditionalEffect =>
                        new TriggeredCardBuffEffectQueueItem(
                            this,
                            selectedCard,
                            buffTriggerContext,
                            conditionalEffect.Effect,
                            new CardBuffSource(buff)) as EffectQueueItem)
                    .ToArray())
                .ValueOr(Array.Empty<EffectQueueItem>());
        }
    }

    private void _CheckGameEnd()
    {
        if (_gameStatus.Ally.IsDead)
        {
            throw new GameEndException(false);
        }
        else if (_gameStatus.Enemy.IsDead)
        {
            throw new GameEndException(true);
        }
    }
}
