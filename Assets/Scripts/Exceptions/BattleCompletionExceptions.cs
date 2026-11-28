using System;

namespace MortalGame.Exceptions
{
    public sealed class BattleCompletionSourceMismatchException : InvalidOperationException
    {
        public Guid ExpectedIdentity { get; }
        public Guid ActualIdentity { get; }

        public BattleCompletionSourceMismatchException(Guid expectedIdentity, Guid actualIdentity)
            : base($"戰後快照的來源玩家與目前 AllyInstance 不一致。預期：{expectedIdentity}，實際：{actualIdentity}。")
        {
            ExpectedIdentity = expectedIdentity;
            ActualIdentity = actualIdentity;
        }
    }

    public sealed class DuplicateCardInstanceIdentityException : InvalidOperationException
    {
        public Type OwnerType { get; }
        public Guid InstanceGuid { get; }

        public DuplicateCardInstanceIdentityException(Type ownerType, Guid instanceGuid)
            : base($"{ownerType.FullName} 包含重複的卡片身份：{instanceGuid}。")
        {
            OwnerType = ownerType;
            InstanceGuid = instanceGuid;
        }
    }

    public sealed class BattleCompletionCardCountMismatchException : InvalidOperationException
    {
        public int ExpectedCount { get; }
        public int ActualCount { get; }

        public BattleCompletionCardCountMismatchException(int expectedCount, int actualCount)
            : base($"戰後快照必須涵蓋完整原牌組，不得新增或遺漏卡片。預期張數：{expectedCount}，實際張數：{actualCount}。")
        {
            ExpectedCount = expectedCount;
            ActualCount = actualCount;
        }
    }

    public sealed class MissingBattleCompletionCardException : InvalidOperationException
    {
        public Guid InstanceGuid { get; }

        public MissingBattleCompletionCardException(Guid instanceGuid)
            : base($"戰後快照缺少原牌組的卡片身份：{instanceGuid}。")
        {
            InstanceGuid = instanceGuid;
        }
    }

    public sealed class MissingBattleCompletionException : InvalidOperationException
    {
        public MissingBattleCompletionException()
            : base("正常勝利結果缺少玩家完成快照。")
        {
        }
    }

    public sealed class BattleCompletionOutcomeMismatchException : InvalidOperationException
    {
        public bool IsAllyWin { get; }
        public bool HasCompletion { get; }

        public BattleCompletionOutcomeMismatchException(bool isAllyWin, bool hasCompletion)
            : base($"戰鬥勝負與玩家完成快照不一致。玩家勝利：{isAllyWin}，具有快照：{hasCompletion}。")
        {
            IsAllyWin = isAllyWin;
            HasCompletion = hasCompletion;
        }
    }

    public sealed class MissingAllySourceIdentityException : InvalidOperationException
    {
        public Guid PlayerIdentity { get; }

        public MissingAllySourceIdentityException(Guid playerIdentity)
            : base($"勝利玩家 [{playerIdentity}] 缺少來源 AllyInstance 身份。")
        {
            PlayerIdentity = playerIdentity;
        }
    }
}
