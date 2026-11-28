using System;
using MortalGame.GameData;

namespace MortalGame.Exceptions
{
    public sealed class MissingCardSelectionSessionException : InvalidOperationException
    {
        public Guid CardIdentity { get; }

        public MissingCardSelectionSessionException(Guid cardIdentity)
            : base($"準備卡片 [{cardIdentity}] 的合成候選前，必須建立 CardSelectionSession。")
        {
            CardIdentity = cardIdentity;
        }
    }

    public sealed class UnsupportedSelectionTypeException : NotSupportedException
    {
        public Type ActualType { get; }

        public UnsupportedSelectionTypeException(Type actualType)
            : base($"尚未支援的選卡輸入型別：{actualType.FullName}。")
        {
            ActualType = actualType;
        }
    }

    public sealed class UnsupportedCardSetOperationException : ArgumentOutOfRangeException
    {
        public SetOperationType Operation { get; }

        public UnsupportedCardSetOperationException(SetOperationType operation, string paramName)
            : base(paramName, operation, "不支援的卡片集合運算。")
        {
            Operation = operation;
        }
    }
}
