using System;
using MortalGame.GameModel;

namespace MortalGame.Exceptions
{
    public sealed class MissingBattleResultException : InvalidOperationException
    {
        public MissingBattleResultException()
            : base("戰鬥已結束，但 GameplayManager 未提供 BattleResult。")
        {
        }
    }

    public sealed class MissingLoseReactionException : InvalidOperationException
    {
        public MissingLoseReactionException()
            : base("失敗面板結束時缺少玩家操作結果。")
        {
        }
    }

    public sealed class UnsupportedGameplayResultException : InvalidOperationException
    {
        public GameplayResult Result { get; }

        public UnsupportedGameplayResultException(GameplayResult result)
            : base($"未支援的戰鬥結果：{result}。")
        {
            Result = result;
        }
    }

    public sealed class UnsupportedLevelMapReactionException : InvalidOperationException
    {
        public LevelMapReactionType ReactionType { get; }

        public UnsupportedLevelMapReactionException(LevelMapReactionType reactionType)
            : base($"未支援的地圖操作：{reactionType}。")
        {
            ReactionType = reactionType;
        }
    }

    public enum BattleFlowKind
    {
        Battle,
        PlayerCommands,
        EventRendering,
        UI
    }

    public sealed class UnexpectedBattleFlowCompletionException : InvalidOperationException
    {
        public BattleFlowKind FlowKind { get; }

        public UnexpectedBattleFlowCompletionException(BattleFlowKind flowKind)
            : base($"{_GetFlowName(flowKind)}流程在戰鬥完成前提早結束。")
        {
            FlowKind = flowKind;
        }

        private static string _GetFlowName(BattleFlowKind flowKind) => flowKind switch
        {
            BattleFlowKind.Battle => "戰鬥",
            BattleFlowKind.PlayerCommands => "玩家操作",
            BattleFlowKind.EventRendering => "事件呈現",
            BattleFlowKind.UI => "UI",
            _ => flowKind.ToString()
        };
    }

    public sealed class UnhandledGameCommandException : NotImplementedException
    {
        public Type ActualType { get; }

        public UnhandledGameCommandException(Type actualType)
            : base($"尚未實作的遊戲命令型別：{actualType.FullName}。")
        {
            ActualType = actualType;
        }
    }
}
