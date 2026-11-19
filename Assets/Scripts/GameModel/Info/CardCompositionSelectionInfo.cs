using System.Collections.Immutable;

namespace MortalGame.GameModel
{
    public sealed record CardTemplateSubSelectionAction(string TemplateCardDataId) : ISubSelectionAction;
    public sealed record CardEffectSubSelectionAction(string OptionId) : ISubSelectionAction;

    public sealed record SelectedCardEffect(string TemplateSelectionId, string OptionId);

    /// <summary>提供完整合法池與配置的提供數量；實際抽樣及保存由選取流程負責。</summary>
    public sealed record CardTemplateSelectionInfo(
        ImmutableArray<string> TemplateCardDataIds,
        int OfferCount,
        string Id,
        int StepIndex,
        int StepCount,
        string CardDataId) : SubSelectionStepInfo(Id, StepIndex, StepCount, CardDataId);

    public sealed record CardEffectOptionInfo(string Id, string DescriptionKey);

    public sealed record CardEffectSelectionInfo(
        ImmutableArray<CardEffectOptionInfo> Options,
        string TemplateSelectionId,
        int OfferCount,
        string Id,
        int StepIndex,
        int StepCount,
        string CardDataId) : SubSelectionStepInfo(Id, StepIndex, StepCount, CardDataId);
}
