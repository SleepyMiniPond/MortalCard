using System;

namespace MortalGame.Exceptions
{
    /// <summary>擁有者尚有未釋放的 Session，無法再次開啟或綁定。</summary>
    public sealed class SessionAlreadyOpenException : InvalidOperationException
    {
        public Type OwnerType { get; }

        public SessionAlreadyOpenException(Type ownerType)
            : base($"{(ownerType ?? throw new ArgumentNullException(nameof(ownerType))).FullName} 尚有未釋放的 Session，不能重複開啟或綁定。")
        {
            OwnerType = ownerType;
        }
    }
}
