using System;
using MortalGame.GameModel;

namespace MortalGame.Exceptions
{
    public sealed class InvalidSuccessfulCardFormStatusException : InvalidOperationException
    {
        public CardFormOperationStatus Status { get; }

        public InvalidSuccessfulCardFormStatusException(CardFormOperationStatus status)
            : base($"成功的形態操作具有無效狀態：{status}。")
        {
            Status = status;
        }
    }
}
