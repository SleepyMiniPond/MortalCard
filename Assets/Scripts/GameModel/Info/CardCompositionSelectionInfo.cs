using System.Collections.Immutable;

namespace MortalGame.GameModel
{
    public sealed record CardTemplateSubSelectionAction(string TemplateCardDataId, System.Guid OfferIdentity = default) : ISubSelectionAction;
    public sealed record CardEffectSubSelectionAction(string OptionId, System.Guid OfferIdentity = default) : ISubSelectionAction;

    public sealed record SelectedCardEffect(string TemplateSelectionId, string OptionId);
    public sealed record SelectedCardComposition(System.Guid CardIdentity, ComposedCardDefinition Definition);

    /// <summary>Query 回傳完整合法池，Prepare 回傳本輪已保存的提供清單。</summary>
    public sealed record CardTemplateSelectionInfo(
        ImmutableArray<string> TemplateCardDataIds,
        int OfferCount,
        string Id,
        int StepIndex,
        int StepCount,
        string CardDataId) : SubSelectionStepInfo(Id, StepIndex, StepCount, CardDataId);

    public sealed record CardEffectOptionInfo(string Id, string DescriptionKey)
    {
        public CardDescriptionFragment Description { get; init; }
    }

    public sealed record CardEffectSelectionInfo(
        ImmutableArray<CardEffectOptionInfo> Options,
        string TemplateSelectionId,
        string TemplateCardDataId,
        int OfferCount,
        string Id,
        int StepIndex,
        int StepCount,
        string CardDataId) : SubSelectionStepInfo(Id, StepIndex, StepCount, CardDataId);
}
