# Popup 彈窗互動

> 核對日期：2026-09-19

Popup 顯示臨時互動內容；Presenter 非同步等待操作，完成或取消時清理面板及訂閱。

## 卡片瀏覽與詳情

[AllCardDetailPresenter](../Assets/Scripts/Presenter/Gameplay/AllCardDetailPresenter.cs) 協調集合瀏覽與單卡詳情，[AllCardDetailPanel](../Assets/Scripts/GameView/Panel/Popup/AllCardDetailPanel.cs) 負責顯示。[SingleCardDetailPopupPanel](../Assets/Scripts/GameView/Panel/Popup/SingleCardDetailPopupPanel.cs) 依 Identity 訂閱最新卡片及提示資料。

[SimpleTitleInfoHintView](../Assets/Scripts/GameView/Panel/Popup/SimpleTitleIInfoHintView.cs) 提供懸停標題與說明，其檔名目前保留額外的 I。

## 子選取

[CardSelectionPanel](../Assets/Scripts/GameView/Panel/Popup/CardSelectionPanel.cs) 呈現候選卡、已選項與確認狀態。[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs) 已能逐群組處理 ExistCard，依群組 ID 回傳結果。

必選時不能直接關閉，須達指定數量；非必選可提早結束。目前尚無完整的候選不足、整次出牌取消與新卡／效果片段選取契約，因此不等同 T-011 已完成，見 [TODO](TODO.md)。

## 勝負結果

[Lose 面板](../Assets/Scripts/GameView/Panel/Popup/GameResultLosePanel.cs) 提供 Retry／Restart／Quit。[Win 面板](../Assets/Scripts/GameView/Panel/Popup/GameResultWinPanel.cs) 只有開關顯示，[Win Presenter](../Assets/Scripts/Presenter/Gameplay/GameResultWinPresenter.cs) 尚無正常完成事件，會等至場景取消。

共用等待機制見 [UniTaskPresenter](../Assets/Scripts/Presenter/Gameplay/UniTaskPresenter.cs)，場景結果限制見 [Scene](Scene.md)。
