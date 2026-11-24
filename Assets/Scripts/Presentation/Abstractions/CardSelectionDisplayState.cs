using System;
using System.Collections.Generic;
using MortalGame.GameModel;

namespace MortalGame.Presentation.Abstractions
{
    // 每次發布完整顯示狀態，讓選取效果、提示及確認按鈕一起更新。
    public record CardSelectionDisplayState(
        IReadOnlyList<CardInfo> Candidates,
        bool IsVisible,
        bool IsConfirmable,
        IReadOnlyList<Guid> SelectedCardIdentities,
        CardSelectionPromptInfo Prompt);
}
