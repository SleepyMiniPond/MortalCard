# Popup 彈窗互動

> 核對日期：2026-11-12

Popup 顯示臨時互動內容；Presenter 非同步等待操作，完成或取消時清理面板及訂閱。

## 卡片瀏覽與詳情

[AllCardDetailPresenter](../Assets/Scripts/Presenter/Gameplay/AllCardDetailPresenter.cs) 協調集合瀏覽與單卡詳情，[AllCardDetailPanel](../Assets/Scripts/GameView/Panel/Popup/AllCardDetailPanel.cs) 負責顯示。[SingleCardDetailPopupPanel](../Assets/Scripts/GameView/Panel/Popup/SingleCardDetailPopupPanel.cs) 依 Identity 訂閱最新卡片及提示資料。

[SimpleTitleInfoHintView](../Assets/Scripts/GameView/Panel/Popup/SimpleTitleIInfoHintView.cs) 提供懸停標題與說明，其檔名目前保留額外的 I。

## 子選取

[CardSelectionFlow](../Assets/Scripts/Presenter/Gameplay/CardSelectionFlow.cs) 管理多步驟草稿及逐步查詢；[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs) 只處理單一步驟。[CardSelectionPanel](../Assets/Scripts/GameView/Panel/Popup/CardSelectionPanel.cs) 呈現候選、選中標記、提示與確認狀態。

關閉任一步都會取消整次出牌，必選也能取消；確認按鈕只有在數量合法時可用。Optional 可確認零張，與取消不同。前一步只保存結果，全部完成並通過 Model 重驗後才正式出牌。數量與候選不足的完整規則集中於 [Target](Target.md#數量與候選不足)。

固定按鈕由 Presenter 訂閱，隨 Run 結束清理；收合及卡片詳情不推進步驟。詳情等待期間不處理底層選取操作。NewCard／NewPartialCard／NewEffect 仍未實作；T-011 的完成範圍與驗收紀錄見 [任務封存](TODO_Archive.md#t-011多步驟自訂目標選取已完成)。

## 文字警告

[WarningPopupPresenter](../Assets/Scripts/Presenter/Gameplay/WarningPopupPresenter.cs) 接收 GameplayPresenter 已轉譯的失敗文字，開啟獨立 [WarningPopupPanel](../Assets/Scripts/GameView/Panel/Popup/WarningPopupPanel.cs)，等待關閉後清理訂閱與畫面。警告不借用選卡面板；正式提交被拒絕時，等待警告關閉後才處理恢復互動事件。

目前 [WarningPopup Prefab](../Assets/Prefab/UI/OverlayCanvas/WarningPopup.prefab) 與 [Gameplay 場景](../Assets/Scenes/Gameplay.unity) 已有序列化接線，場景實例預設停用；本輪僅靜態核對，未驗收實際排版及點擊。

## 勝負結果

[Lose 面板](../Assets/Scripts/GameView/Panel/Popup/GameResultLosePanel.cs) 提供 Retry／Restart／Quit。[Win 面板](../Assets/Scripts/GameView/Panel/Popup/GameResultWinPanel.cs) 只有開關顯示，[Win Presenter](../Assets/Scripts/Presenter/Gameplay/GameResultWinPresenter.cs) 尚無正常完成事件，會等至場景取消。

共用等待機制見 [UniTaskPresenter](../Assets/Scripts/Presenter/Gameplay/UniTaskPresenter.cs)，場景結果限制見 [Scene](Scene.md)。
