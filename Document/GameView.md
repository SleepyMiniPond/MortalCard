# GameView 視覺呈現

> 核對日期：2026-09-19

GameView 消費 Model 事件、顯示狀態並將互動交回 Presenter。戰鬥規則由 Model 決定，View 另有拖曳、焦點與合法選取等互動判斷。

## 事件與顯示狀態

[GameplayView](../Assets/Scripts/GameView/GameplayView.cs) 逐事件分發更新，不會為所有事件等待完整動畫播放。角色動畫由各 Worker 排程。

[GameViewModel](../Assets/Scripts/Presenter/Gameplay/GameInfoModel.cs) 保存卡片、Buff、牌區及好感度等可觀察資料；血條、能量條與回合顯示亦有直接事件更新。契約位於 [PresentationContracts](../Assets/Scripts/Presentation/Abstractions/PresentationContracts.cs)。

卡片以 Identity 訂閱最新 CardInfo。GeneralUpdateEvent 更新一般資料，CardFormChangedEvent 更新有效形態；移牌、出牌等事件可只帶身份與區域，不必重建完整卡片快照。

目前 Render 有 IncreaseDispositionEvent 分支，卻沒有 DecreaseDispositionEvent 分支；雖存在減少好感度的畫面處理方法，尚未由事件分發接入，見 [TODO](TODO.md)。

## 互動與資源

Presenter 在結算時控制互動開關。卡片拖曳與聚焦遇到形態改變時，依最新資料重新驗證規則。動態卡片、Buff 圖示與數字動畫使用 Factory；回收前必須清理各自訂閱與播放狀態。

角色呈現目前仍是一個 Ally／一個 Enemy View，不能視為完整多角色動態集合。

## 子系統導引

| 領域 | 文件 |
|---|---|
| 手牌、拖曳、聚焦與詳情 | [CardView](CardView.md) |
| 玩家 Buff 圖示 | [BuffView](BuffView.md) |
| 角色動畫排程與取消 | [CharacterView](CharacterView.md) |
| 數字動畫 | [EventView](EventView.md) |
| 物件池 | [Factory](Factory.md) |
| 資訊、彈窗、按鈕 | [Panel](GameView_Panel.md) |

通用 Unity UI 工具另位於 [Assets/Scripts/UI](../Assets/Scripts/UI/)，不等同於 GameView 的 Panel/UI。
