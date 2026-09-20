using System;
using MortalGame.GameData;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;
namespace MortalGame.GameModel
{

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

        public async UniTask<T> Dequeue(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (_queue.TryDequeue(out var result))
                {
                    return result;
                }

                await UniTask.NextFrame(cancellationToken);
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
        private List<IGameEvent> _gameEvents = new();
        private UniTaskAwaitableQueue<IGameAction> _gameActions;
        private IGameContextManager _contextMgr;
        private GameHistory _gameHistory; // TODO
        private Option<CardPlayChain> _activeCardPlayChain = Option.None<CardPlayChain>();
        private CancellationToken _battleCancellationToken;
        internal int CardPlayChainBudget { get; set; } = EffectQueueRunner.BUDGET_COUNT;

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

        public async UniTask<Option<BattleResult>> StartBattle(
            CancellationToken cancellationToken)
        {
            _gameEvents = new List<IGameEvent>();
            _gameActions = new UniTaskAwaitableQueue<IGameAction>();
            _battleResult = Option.None<BattleResult>();

            _battleCancellationToken = cancellationToken;
            try
            {
                await _Run(cancellationToken);
            }
            finally
            {
                _battleCancellationToken = CancellationToken.None;
            }

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

        public Option<SubSelectionInfo> QueryCardSubSelectionInfos(
            Guid cardIdentity,
            MainSelectionAction mainSelectionAction)
        {
            if (!this.GetCard(cardIdentity).TryGetValue(out var cardEntity) ||
                !SelectionInfoUtility.TryCreateMainSelectionContext(
                        this,
                        cardEntity,
                        mainSelectionAction)
                    .TryGetValue(out var mainSelectionContext))
            {
                return Option.None<SubSelectionInfo>();
            }

            using (_contextMgr.SetContext(mainSelectionContext))
            {
                return cardEntity.SubSelects.ToInfo(this, cardEntity);
            }
        }

        private async UniTask _Run(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _GameStart();
                while (true)
                {
                    _TurnStart();

                    _TurnDrawCard();

                    _EnemyPrepare();

                    await _PlayerExecute(cancellationToken);

                    _EnemyExecute();

                    _TurnEnd();
                }
            }
            catch (GameEndException gameEndEx)
            {
                var cardInstanceChanges = CardInstanceChangeSetCollector.CollectForBattleResult(
                    gameEndEx.IsAllyWin,
                    _gameStageSetting.Ally.Deck,
                    _gameStatus.Ally.CardManager);
                _battleResult = new BattleResult(
                    gameEndEx.IsAllyWin,
                    cardInstanceChanges).Some();
            }
        }

        private void _GameStart()
        {
            _gameStatus.SummonAlly(ParseAlly(_gameStageSetting.Ally, _contextMgr));
            _gameEvents.Add(new AllySummonEvent(_gameStatus.Ally));

            _gameStatus.SummonEnemy(ParseEnemy(_gameStageSetting.Enemy, _contextMgr));
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

            var initialDeckCards = CreateInitialDeckCardSnapshot();

            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeGameStart, SystemSource.Instance));
            _gameEvents.AddRange(RunInitialCardInitialize(initialDeckCards));
            _gameEvents.AddRange(_RunTiming(GameTiming.AfterGameStart, SystemSource.Instance));

            IReadOnlyList<InitialDeckCardCandidate> CreateInitialDeckCardSnapshot()
            {
                var players = new IPlayerEntity[]
                {
                    _gameStatus.Ally,
                    _gameStatus.Enemy
                };
                var candidates = new List<InitialDeckCardCandidate>();
                var identities = new HashSet<Guid>();

                foreach (var player in players)
                {
                    foreach (var card in player.CardManager.Deck.Cards)
                    {
                        if (identities.Add(card.Identity))
                        {
                            candidates.Add(new InitialDeckCardCandidate(card));
                        }
                    }
                }

                return candidates;
            }

            IEnumerable<IGameEvent> RunInitialCardInitialize(
                IReadOnlyCollection<InitialDeckCardCandidate> initialDeckCards)
            {
                var initialQueueItems = initialDeckCards
                    .SelectMany(candidate => 
                        CardTriggeredEffectDispatch.CreateItems(
                            this,
                            candidate.Card,
                            CardTriggeredTiming.Initialize,
                            SystemSource.Instance));
                return RunEffectBatch(initialQueueItems).Events;
            }

            AllyEntity ParseAlly(AllyInstance allyInstance, IGameContextManager gameContextManager)
            {
                var characterRecord = new CharacterParameter
                {
                    NameKey = allyInstance.NameKey,
                    CurrentHealth = allyInstance.CurrentHealth,
                    MaxHealth = allyInstance.MaxHealth
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

            EnemyEntity ParseEnemy(EnemyData enemyData, IGameContextManager gameContextManager)
            {
                var characterRecord = new CharacterParameter
                {
                    NameKey = enemyData.PlayerData.NameKey,
                    CurrentHealth = enemyData.PlayerData.InitialHealth,
                    MaxHealth = enemyData.PlayerData.MaxHealth
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
            _gameStatus.SetNewTurn();
            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeTurnStart, SystemSource.Instance));

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

            _gameEvents.AddRange(_RunTiming(GameTiming.AfterTurnStart, SystemSource.Instance));

            _CheckGameEnd();
        }

        private void _TurnDrawCard()
        {
            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeDrawCard, SystemSource.Instance));

            var allyDrawCount = _contextMgr.DispositionLibrary.GetDrawCardCount(_gameStatus.Ally.DispositionManager.CurrentDisposition);
            var enemyDrawCount = _gameStatus.Enemy.TurnStartDrawCardCount;

            var allyDrawEvents = EffectManager.DrawCards(this, SystemSource.Instance, _gameStatus.Ally, allyDrawCount);
            _gameEvents.AddRange(allyDrawEvents.Events);

            var enemyDrawEvents = EffectManager.DrawCards(this, SystemSource.Instance, _gameStatus.Enemy, enemyDrawCount);
            _gameEvents.AddRange(enemyDrawEvents.Events);

            _gameEvents.AddRange(_RunTiming(GameTiming.AfterDrawCard, SystemSource.Instance));

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

        public async UniTask _PlayerExecute(CancellationToken cancellationToken)
        {
            using var allyStatus = _gameStatus.SetCurrentPlayer(_gameStatus.Ally);
            var executeStartSource = new SystemExectueStartSource(_gameStatus.Ally);

            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeExecuteStart, executeStartSource));

            _gameEvents.Add(new PlayerExecuteStartEvent(
                Faction: _gameStatus.Ally.Faction,
                CardManagerInfo: _gameStatus.Ally.CardManager.ToInfo(),
                HandCardInfo: _gameStatus.Ally.CardManager.HandCard.ToCardCollectionInfo(this)
            ));

            _gameEvents.AddRange(_RunTiming(GameTiming.AfterExecuteStart, executeStartSource));

            var isExecuting = true;
            while (isExecuting)
            {
                var action = await _gameActions.Dequeue(cancellationToken);

                switch (action)
                {
                    case UseCardAction useCardAction:
                        if (_UseCard(_gameStatus.Ally, useCardAction))
                        {
                            _gameEvents.Add(new PlayerExecuteStartEvent(
                                Faction: _gameStatus.Ally.Faction,
                                CardManagerInfo: _gameStatus.Ally.CardManager.ToInfo(),
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
            using var enemyStatus = _gameStatus.SetCurrentPlayer(_gameStatus.Enemy);
            var executeStartSource = new SystemExectueStartSource(_gameStatus.Enemy);

            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeExecuteStart, executeStartSource));
            _gameEvents.AddRange(_RunTiming(GameTiming.AfterExecuteStart, executeStartSource));

            while (_gameStatus.Enemy.TryGetNextUseCardAction(this, out var useCardAction))
            {
                if (_UseCard(_gameStatus.Enemy, useCardAction))
                {
                    _gameEvents.Add(new PlayerExecuteStartEvent(
                        Faction: _gameStatus.Enemy.Faction,
                        CardManagerInfo: _gameStatus.Enemy.CardManager.ToInfo(),
                        HandCardInfo: _gameStatus.Ally.CardManager.HandCard.ToCardCollectionInfo(this)
                    ));
                }

                _CheckGameEnd();
            }

            _FinishEnemyExecuteTurn();
        }

        private void _FinishPlayerExecuteTurn()
        {
            var executeEndSource = new SystemExectueEndSource(_gameStatus.Ally);
            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeExecuteEnd, executeEndSource));

            _gameEvents.Add(new PlayerExecuteEndEvent(
                Faction: _gameStatus.Ally.Faction,
                CardManagerInfo: _gameStatus.Ally.CardManager.ToInfo()
            ));

            _gameEvents.AddRange(_RunTiming(GameTiming.AfterExecuteEnd, executeEndSource));

            _gameActions.Clear();
        }
        private void _FinishEnemyExecuteTurn()
        {
            var executeEndSource = new SystemExectueEndSource(_gameStatus.Enemy);
            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeExecuteEnd, executeEndSource));

            var unselectedCards = _gameStatus.Enemy.SelectedCards.UnSelectAllCards();
            _gameEvents.Add(new EnemyUnselectedCardEvent(
                UnselectedCards: unselectedCards.Select(c => c.Identity).ToImmutableArray()));

            _gameEvents.AddRange(_RunTiming(GameTiming.AfterExecuteEnd, executeEndSource));

            _gameActions.Clear();
        }

        private void _TurnEnd()
        {
            _gameEvents.AddRange(_RunTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance));

            ClearHandAndRunCardTimings(_gameStatus.Ally);
            ClearHandAndRunCardTimings(_gameStatus.Enemy);

            _gameEvents.AddRange(_RunTiming(GameTiming.AfterTurnEnd, SystemSource.Instance));

            _CheckGameEnd();

            void ClearHandAndRunCardTimings(IPlayerEntity player)
            {
                var handClearResult = player.CardManager.ClearHandOnTurnEnd(this);
                _gameEvents.AddRange(handClearResult.Events);

                var orderedResults = handClearResult.Cards
                    .Where(result =>
                        result.TriggeredTiming == CardTriggeredTiming.Preserved)
                    .Concat(handClearResult.Cards.Where(result =>
                        result.TriggeredTiming == CardTriggeredTiming.Discarded));
                var cardTimingItems = orderedResults
                    .SelectMany(result =>
                        CardTriggeredEffectDispatch.CreateItems(
                            this,
                            result.Card,
                            result.TriggeredTiming,
                            SystemSource.Instance));

                _gameEvents.AddRange(
                    RunEffectBatch(cardTimingItems).Events);
            }
        }

        private bool _TrySetUseCardSelection(
            UseCardAction useCardAction,
            out IGameContextManager selectionScope)
        {
            selectionScope = null;
            if (!this.GetCard(useCardAction.CardIndentity)
                    .TryGetValue(out var cardEntity) ||
                !SelectionInfoUtility.TryCreateUseCardContext(
                        this,
                        cardEntity,
                        useCardAction)
                    .TryGetValue(out var selectionContext))
            {
                return false;
            }

            selectionScope = _contextMgr.SetContext(selectionContext);
            return true;
        }
        private bool _UseCard(IPlayerEntity player, UseCardAction action)
        {
            if (_activeCardPlayChain.HasValue)
                throw new InvalidOperationException("完整出牌流程不可重入；請改為排入出牌請求。");

            var result = _RunCardPlayChain(new ActiveCardPlayRoot(player, action));
            _gameEvents.AddRange(result.Effects.Events);
            return result.CardPlayed;
        }

        private bool _ExecuteSelectedCard(CardPlayChain chain, IPlayerEntity player, UseCardAction action, CardPlayReason reason)
        {
            if (!_TrySetUseCardSelection(action, out var selectionScope))
                return false;

            bool succeeded;
            using (selectionScope)
            {
                succeeded = _ExecuteCardPlay(chain, player, action.CardIndentity, reason);
            }
            if (succeeded)
                _CheckGameEnd();
            return succeeded;
        }

        private void _ExecuteCardPlayRequest(CardPlayChain chain, CardPlayRequest request)
        {
            if (!_gameStatus.GetPlayer(request.OwnerIdentity).TryGetValue(out var owner))
                return;
            var card = owner.CardManager.HandCard.Cards.FirstOrDefault(c => c.Identity == request.CardIdentity);
            if (card == null || card.HasProperty(CardProperty.Sealed))
                return;

            using (_contextMgr.SetContext(GameContext.EMPTY))
            {
                if (!SelectTargetLogic.SelectTargets(this, card, owner).TryGetValue(out var selection))
                    return;
                _ExecuteSelectedCard(chain, owner,
                    new UseCardAction(card.Identity, selection.MainSelectionAction, selection.SubSelectionActions),
                    CardPlayReason.Effect);
            }
        }

        public void EnqueueCardPlay(CardPlayRequest request)
        {
            if (!_activeCardPlayChain.TryGetValue(out var chain))
                throw new InvalidOperationException("出牌請求必須由效果根批次提交。");
            chain.Enqueue(request);
        }

        public EffectResult RunEffectBatch(IEnumerable<EffectQueueItem> items)
        {
            if (_activeCardPlayChain.TryGetValue(out var activeChain))
                return activeChain.RunEffects(items);

            return _RunCardPlayChain(new EffectBatchRoot(items)).Effects;
        }

        // 根工作只攜帶資料；實際執行分支集中於下方，不傳入委派。
        private abstract record CardPlayChainRoot;
        private sealed record ActiveCardPlayRoot(IPlayerEntity Player, UseCardAction Action) : CardPlayChainRoot;
        private sealed record EffectBatchRoot(IEnumerable<EffectQueueItem> Items) : CardPlayChainRoot;
        private sealed record CardPlayChainResult(bool CardPlayed, EffectResult Effects);

        private CardPlayChainResult _RunCardPlayChain(CardPlayChainRoot root)
        {
            var chain = new CardPlayChain(CardPlayChainBudget, _battleCancellationToken);
            _activeCardPlayChain = chain.Some();
            var result = EffectResult.Empty;
            var rootCompleted = false;
            var cardPlayed = false;
            try
            {
                try
                {
                    switch (root)
                    {
                        case ActiveCardPlayRoot active:
                            chain.BeginStep($"ActiveCard:{active.Action.CardIndentity}");
                            cardPlayed = _ExecuteSelectedCard(chain, active.Player, active.Action, CardPlayReason.Active);
                            break;
                        case EffectBatchRoot batch:
                            result = chain.RunEffects(batch.Items);
                            break;
                        default:
                            throw new InvalidOperationException("不支援的出牌鏈根工作。");
                    }
                    rootCompleted = true;
                    
                    while (chain.TryDequeue(out var request))
                    {
                        chain.BeginStep($"CardPlayRequest:{request.CardIdentity};Source:{request.RequestedBy.GetType().Name}");
                        _ExecuteCardPlayRequest(chain, request);
                    }
                }
                catch (CardPlayChainHaltedException halted)
                {
                    // 根批次中止時保留其部分結果；後續牌的結果不可倒灌。
                    if (!rootCompleted && root is EffectBatchRoot)
                        result = halted.PartialResult;
                }
                return new CardPlayChainResult(cardPlayed,
                    new EffectResult(result.Actions, chain.Events.ToArray()));
            }
            catch
            {
                // 無法正常回傳時，仍保留已提交事件供畫面同步。
                _gameEvents.AddRange(chain.Events);
                throw;
            }
            finally
            {
                chain.Clear();
                _activeCardPlayChain = Option.None<CardPlayChain>();
            }
        }

        private bool _ExecuteCardPlay(CardPlayChain chain, IPlayerEntity player, Guid cardIdentity, CardPlayReason reason)
        {
            if (_gameStatus.Ally.CardManager.PlayingCard.HasValue ||
                _gameStatus.Enemy.CardManager.PlayingCard.HasValue)
                return false;

            var usedCard = player.CardManager.HandCard.Cards.FirstOrDefault(c => c.Identity == cardIdentity);
            if (usedCard == null || usedCard.HasProperty(CardProperty.Sealed))
            {
                return false;
            }

            var lookContext = new TriggerContext(this, new CardTrigger(usedCard), new CardLookIntentAction(usedCard));
            if (!usedCard.GetCardProperty(lookContext, CardProperty.EffectRepeat).TryGetValue(out var effectRepeat))
            {
                return false;
            }

            var payment = Option.None<CardPlayPayment>();
            if (reason == CardPlayReason.Active)
            {
                if (!GameFormula.CardCost(lookContext, usedCard).TryGetValue(out var cost) ||
                    cost > player.CurrentEnergy)
                {
                    return false;
                }
                payment = new CardPlayPayment(cost).Some();
            }

            var (isSuccess, playCardDisposable) = player.CardManager.TryPlayCard(
                usedCard, out var handCardIndex, out var handCardsCount);
            if (!isSuccess)
            {
                return false;
            }

            var cardPlaySource = new CardPlaySource(
                usedCard, handCardIndex, handCardsCount, reason, payment, new CardPlayAttributeEntity());
            var cardPlayTrigger = new CardPlayTrigger(cardPlaySource);
            var cardPlayIntent = new CardPlayIntentAction(cardPlaySource);
            var cardPlayTriggerContext = new TriggerContext(this, cardPlayTrigger, cardPlayIntent);
            CardPlayResultSource cardPlayResultSource;
            var completed = false;
            try
            {
                using (playCardDisposable)
                {
                    if (payment.TryGetValue(out var paid))
                    {
                        var result = player.EnergyManager.ConsumeEnergy(paid.EnergySpent);
                        chain.Record(new LoseEnergyEvent(player.Faction, player.EnergyManager.ToInfo(), result));
                    }
                    _RunTiming(
                        GameTiming.BeforePlayCardStart,
                        cardPlaySource);

                    chain.Record(ObserveRootAction(cardPlayIntent));

                    // TODO：檢查並移除過期 Buff，派送對應移除事件。

                    _RunTiming(
                        GameTiming.AfterPlayCardStart,
                        cardPlaySource);

                    var effectActionResults = new List<BaseResultAction>();

                    var repeatTimes = Math.Max(1, effectRepeat);
                    for (int i = 0; i < repeatTimes; i++)
                    {
                        chain.BeginStep($"CardEffectRepeat:{usedCard.Identity}:{i}");
                        var effectResult = RunEffectBatch(
                            usedCard.Effects.Select(effect =>
                                new CardEffectQueueItem(
                                    cardPlayTriggerContext,
                                    effect)));

                        effectActionResults.AddRange(effectResult.Actions);
                    }

                    var usedCardEvent = new UsedCardEvent(
                        Faction: player.Faction,
                        UsedCardIdentity: usedCard.Identity,
                        CardManagerInfo: player.CardManager.ToInfo(),
                        Reason: cardPlaySource.Reason);
                    chain.Record(usedCardEvent);

                    var playedResult = RunEffectBatch(
                        CardTriggeredEffectDispatch.CreateItems(
                            cardPlayTriggerContext,
                            usedCard,
                            cardPlaySource.Reason == CardPlayReason.Active
                            ? CardTriggeredTiming.Played
                            : CardTriggeredTiming.EffectPlayed));

                    effectActionResults.AddRange(playedResult.Actions);

                    cardPlayResultSource = cardPlaySource.CreateResultSource(effectActionResults);

                    chain.Record(
                        ObserveRootAction(new CardPlayResultAction(cardPlayResultSource)));
                    _RunTiming(
                        GameTiming.BeforePlayCardEnd,
                        cardPlayResultSource);
                }

                if (usedCard.HasProperty(CardProperty.Recycle))
                {
                    var recycleResult = EffectManager.RecycleCardOnPlayEnd(this, player, usedCard);
                    chain.Record(recycleResult.Events);
                }

                _RunTiming(
                    GameTiming.AfterPlayCardEnd,
                    cardPlayResultSource);
                completed = true;
            }
            finally
            {
                if (!completed)
                {
                    // PlayingCard 的 Dispose 已完成；只同步真實離場，不偽造 UsedCard 或 Result。
                    if (player.CardManager.HandCard.Cards.Any(c => c.Identity == usedCard.Identity))
                        chain.Record(new PlayerExecuteEndEvent(player.Faction, player.CardManager.ToInfo()));
                    else
                    {
                        var destination = player.CardManager.ExclusionZone.Cards.Any(c => c.Identity == usedCard.Identity)
                            ? CardCollectionType.ExclusionZone : CardCollectionType.Graveyard;
                        chain.Record(new MoveCardEvent(player.Faction, usedCard.Identity,
                            CardCollectionType.HandCard, destination, player.CardManager.ToInfo()));
                    }
                }
            }

            OnUseCard?.Invoke();
            return true;
        }

        public IEnumerable<IGameEvent> ObserveRootAction(IActionUnit actionUnit)
        {
            return _ObserveAction(
                new TriggerContext(
                    this,
                    new PlayerTrigger(_gameStatus.Ally),
                    actionUnit));
        }

        public IEnumerable<IGameEvent> ObserveDerivedAction(
            TriggerContext parentContext,
            IActionUnit actionUnit)
        {
            return _ObserveAction(parentContext with { Action = actionUnit });
        }

        private IEnumerable<IGameEvent> _ObserveAction(TriggerContext context)
        {
            var allyEvt = _gameStatus.Ally.Update(context with
            {
                Model = this,
                Triggered = new PlayerTrigger(_gameStatus.Ally)
            });
            var enemyEvt = _gameStatus.Enemy.Update(context with
            {
                Model = this,
                Triggered = new PlayerTrigger(_gameStatus.Enemy)
            });

            return new List<IGameEvent> { allyEvt, enemyEvt };
        }

        public IEnumerable<IGameEvent> TriggerTiming(GameTiming timing, IActionSource actionSource)
        {
            return _RunTiming(timing, actionSource);
        }

        internal IGameContextManager EffectQueueContextManager => _contextMgr;

        // TODO: collect reactionEffects created from reactionSessions
        private IEnumerable<IGameEvent> _RunTiming(
            GameTiming timing,
            IActionSource actionSource)
        {
            var result = RunEffectBatch(new[] { new TriggerTimingQueueItem(this, timing, actionSource) });
            return result.Events;
        }

        internal TimingReactionSnapshot CreateTimingReactionSnapshot(
            GameTiming timing,
            IActionSource actionSource)
        {
            var timingAction = new UpdateTimingAction(timing, actionSource);
            IPlayerEntity[] players = { _gameStatus.Ally, _gameStatus.Enemy };
            var cards = players
                .SelectMany(player => player.CardManager.ReactionCards())
                .ToArray();

            return new TimingReactionSnapshot(
                timingAction,
                players
                    .SelectMany(player => player.BuffManager.Buffs
                        .Select(buff => new PlayerBuffReactionCandidate(player, buff)))
                    .ToArray(),
                players
                    .SelectMany(player => player.Characters)
                    .SelectMany(character => character.BuffManager.Buffs
                        .Select(buff => new CharacterBuffReactionCandidate(character, buff)))
                    .ToArray(),
                cards
                    .SelectMany(card => card.BuffManager.Buffs
                        .Select(buff => new CardBuffReactionCandidate(card, buff)))
                    .ToArray(),
                cards
                    .SelectMany(card => card.OverrideFormState
                        .Map(state => new CardFormOverrideReactionCandidate(card, state)
                            .WrapAsEnumerable())
                        .ValueOr(Array.Empty<CardFormOverrideReactionCandidate>()))
                    .ToArray(),
                cards);
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

        private sealed record InitialDeckCardCandidate(
            ICardEntity Card);

    }

}
