using System;
using MortalGame.GameModel;

namespace MortalGame.Exceptions
{
    public sealed class MissingBattleCardEntityException : InvalidOperationException
    {
        public Guid InstanceGuid { get; }

        public MissingBattleCardEntityException(Guid instanceGuid)
            : base($"戰鬥結束時找不到 CardInstance [{instanceGuid}] 對應的卡片實體。")
        {
            InstanceGuid = instanceGuid;
        }
    }

    public sealed class CardOverrideRemovalFailedException : InvalidOperationException
    {
        public Guid CardIdentity { get; }
        public CardFormRejection Rejection { get; }

        public CardOverrideRemovalFailedException(Guid cardIdentity, CardFormRejection rejection)
            : base($"無法清除卡片 [{cardIdentity}] 的 External Override：{rejection?.Message}")
        {
            CardIdentity = cardIdentity;
            Rejection = rejection;
        }
    }

    public sealed class CardInstancePersistenceExportFailedException : InvalidOperationException
    {
        public Guid InstanceGuid { get; }

        public CardInstancePersistenceExportFailedException(Guid instanceGuid)
            : base($"無法建立 CardInstance [{instanceGuid}] 的持久形態輸出。")
        {
            InstanceGuid = instanceGuid;
        }
    }
}
