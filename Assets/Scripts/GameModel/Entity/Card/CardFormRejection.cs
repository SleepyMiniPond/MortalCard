using System;

namespace MortalGame.GameModel
{
    /// <summary>保存形態操作的拒絕原因與必要資料，並集中提供開發診斷。</summary>
    public abstract record CardFormRejection
    {
        public abstract string Message { get; }

        public sealed record InvalidSelfFormInput : CardFormRejection
        {
            public override string Message => "TransformKey 與目標 CardDataId 不可為空白。";
        }

        public sealed record MissingSelfFormCardData(string TargetCardDataId) : CardFormRejection
        {
            public override string Message => $"找不到目標 CardData：{TargetCardDataId}。";
        }

        public sealed record InvalidOverrideInput : CardFormRejection
        {
            public override string Message => "OverrideKey、目標 CardDataId、來源、解除規則與 ReactionSessions 不可為空。";
        }

        public sealed record MissingOverrideCardData(string TargetCardDataId) : CardFormRejection
        {
            public override string Message => $"找不到目標 Override CardData：{TargetCardDataId}。";
        }

        public sealed record OverrideLayerMismatch(Guid StateIdentity) : CardFormRejection
        {
            public override string Message => $"目前 Override State [{StateIdentity}] 與 Buff Layer 不一致。";
        }
    }
}
