using System;

namespace MortalGame.Exceptions
{
    public sealed class UnregisteredDataTypeException : ArgumentException
    {
        public Type OwnerType { get; }
        public Type ActualType { get; }

        public UnregisteredDataTypeException(Type ownerType, Type actualType, string paramName)
            : base($"{ownerType.FullName} 未註冊的資料型別：{actualType.FullName}。", paramName)
        {
            OwnerType = ownerType;
            ActualType = actualType;
        }
    }

    public sealed class UnsupportedInputTypeException : InvalidOperationException
    {
        public Type OwnerType { get; }
        public Type ActualType { get; }

        public UnsupportedInputTypeException(Type ownerType, Type actualType)
            : base($"{ownerType.FullName} 不支援的輸入型別：{actualType.FullName}。")
        {
            OwnerType = ownerType;
            ActualType = actualType;
        }
    }
}
