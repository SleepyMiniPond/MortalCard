using MortalGame.GameModel;

namespace MortalGame.Presentation.Abstractions
{
    // 提供提示所需的資訊，文案查找與格式化由面板處理。
    public record CardSelectionPromptInfo(
        string CardDataId,
        string SelectionId,
        int StepIndex,
        int StepCount,
        int SelectedCount,
        int MaxCount,
        CardSelectionCountRule CountRule);
}
