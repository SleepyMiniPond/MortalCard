namespace MortalGame.GameModel
{
    public enum CardFormPersistence
    {
        BattleOnly,
        Persistent,
    }

    public enum CardFormChangeCause
    {
        SelfTransformApplied,
        SelfTransformReverted,
        OverrideApplied,
        OverrideRemoved,
    }

    public enum CardFormOperationStatus
    {
        Applied,
        Reverted,
        NoOp,
        Rejected,
    }

    public record CardFormState(
        string TransformKey,
        string CardDataId,
        CardFormPersistence Persistence);

    public record PersistentCardFormState(
        string TransformKey,
        string CardDataId);

    public record CardFormOperationResult(
        CardFormOperationStatus Status,
        string BeforeCardDataId,
        string AfterCardDataId,
        string TransformKey,
        string RejectedReason = null)
    {
        public bool IsSuccess =>
            Status == CardFormOperationStatus.Applied ||
            Status == CardFormOperationStatus.Reverted;
    }
}
