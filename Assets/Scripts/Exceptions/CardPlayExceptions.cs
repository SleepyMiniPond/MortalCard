using System;

namespace MortalGame.Exceptions
{
    public sealed class ReentrantCardPlayException : InvalidOperationException
    {
        public ReentrantCardPlayException()
            : base("完整出牌流程不可重入；請改為排入出牌請求。")
        {
        }
    }

    public sealed class CardPlayChainRequiredException : InvalidOperationException
    {
        public Guid CardIdentity { get; }

        public CardPlayChainRequiredException(Guid cardIdentity)
            : base($"卡片 [{cardIdentity}] 的出牌請求必須由效果根批次提交。")
        {
            CardIdentity = cardIdentity;
        }
    }
}
