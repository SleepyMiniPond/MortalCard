using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;

namespace MortalGame.Presenter
{
    /// <summary>
    /// 協調一輪遊戲與同關重試；場景負責等待自己的互動結果。
    /// </summary>
    public sealed class GameFlow
    {
        private readonly Context _context;
        private readonly ISceneLoader _sceneLoader;

        public GameFlow(Context context, ISceneLoader sceneLoader)
        {
            _context = context;
            _sceneLoader = sceneLoader;
        }

        public async UniTask Run(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var menu = await _sceneLoader.LoadMenuScene(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await menu.Run(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _context.ResetRun();
                await _RunLevelMap(cancellationToken);
            }
        }

        private async UniTask _RunLevelMap(CancellationToken cancellationToken)
        {
            var route = new TemporaryBattleRoute();
            while (!route.IsFinished)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var levelMap = await _sceneLoader.LoadLevelMapScene(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var command = await levelMap.Run(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                switch (command.ReactionType)
                {
                    case LevelMapReactionType.Finish:
                    case LevelMapReactionType.Fail:
                        return;
                    case LevelMapReactionType.Restart:
                        _context.ResetRun();
                        route.Reset();
                        continue;
                    case LevelMapReactionType.StartGamePlay:
                        var result = await _RunBattle(route.CreateBattle(_context), cancellationToken);
                        switch (result.Result)
                        {
                            case GameplayWinResult:
                                _context.CommitBattleCompletion(result.AllyCompletion.ValueOr(() =>
                                    throw new InvalidOperationException("正常勝利結果缺少玩家完成快照。")));
                                route.CompleteStage();
                                continue;
                            case GameplayLoseResult { ReactionType: LoseReactionType.Restart }:
                                _context.ResetRun();
                                route.Reset();
                                continue;
                            case GameplayLoseResult { ReactionType: LoseReactionType.Quit }:
                                return;
                            default:
                                throw new InvalidOperationException($"未支援的戰鬥結果：{result.Result}");
                        }
                    default:
                        throw new InvalidOperationException($"未支援的地圖操作：{command.ReactionType}");
                }
            }
        }

        private async UniTask<GameplayResultCommand> _RunBattle(
            GameStageSetting setting, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // 牌組集合由每次嘗試各自持有，不讓重試或戰鬥內修改污染進關快照。
                var attempt = setting with
                {
                    Ally = setting.Ally with
                    {
                        Deck = setting.Ally.Deck.Select(card => card with
                        {
                            AdditionPropertyDatas = card.AdditionPropertyDatas.ToArray()
                        }).ToList()
                    }
                };
                var gameplay = await _sceneLoader.LoadGameplayScene(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var result = await gameplay.Run(_context, attempt, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (result.Result is GameplayLoseResult { ReactionType: LoseReactionType.Retry })
                    continue;
                return result;
            }
        }
    }
}
