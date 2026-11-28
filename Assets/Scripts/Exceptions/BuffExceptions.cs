using System;

namespace MortalGame.Exceptions
{
    public sealed class BuffNotFoundException : InvalidOperationException
    {
        public Type OwnerType { get; }
        public string BuffId { get; }

        public BuffNotFoundException(Type ownerType, string buffId)
            : base($"{ownerType.FullName} 找不到要修改層數的 Buff：{buffId}。")
        {
            OwnerType = ownerType;
            BuffId = buffId;
        }
    }

    public sealed class BuffAlreadyExistsException : InvalidOperationException
    {
        public Type OwnerType { get; }
        public string BuffId { get; }

        public BuffAlreadyExistsException(Type ownerType, string buffId)
            : base($"{ownerType.FullName} 已存在相同 ID 的 Buff，無法重複加入：{buffId}。")
        {
            OwnerType = ownerType;
            BuffId = buffId;
        }
    }
}
