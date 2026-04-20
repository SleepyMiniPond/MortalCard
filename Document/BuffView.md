# BuffView Buff 視圖

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

BuffView 系統負責 Buff 效果在 UI 上的視覺呈現，包括 Buff 圖示、層數顯示、以及懸停提示。設計上採用**響應式訂閱**——每個 BuffView 訂閱 GameViewModel 中對應 Buff 的 ReactiveProperty，資料變化時自動刷新。

## 資料模型

### PlayerBuffInfo
玩家 Buff 的顯示資訊：
- Id：Buff Data ID
- Identity：唯一 Guid
- Level：當前層數
- SessionIntegers：Session 整數值集合
- `GetTemplateValues()`：供本地化模板替換

### CharacterBuffInfo
角色 Buff 的顯示資訊（與 PlayerBuffInfo 結構相同）。

## PlayerBuffView — 單個 Buff 圖示

每個 Buff 對應一個 PlayerBuffView 實例：
- 訂閱 `ViewModel.ObservablePlayerBuffInfo(identity)` 取得即時更新
- 顯示 Buff 層數
- 懸停時顯示本地化的 Buff 名稱與說明（透過 SimpleTitleInfoHintView）
- 使用 `CompositeDisposable` 管理訂閱生命週期
- 實作 `IRecyclable` 支援物件池回收

## PlayerBuffCollectionView — Buff 集合容器

管理單個玩家的所有 Buff 圖示：
- 維護 `_buffViewDict`（Dictionary）進行 O(1) 查詢
- 透過 BuffViewFactory 建立/回收 BuffView
- 響應遊戲事件：
  - `AddPlayerBuffEvent`：建立新 BuffView
  - `RemovePlayerBuffEvent`：移除並回收 BuffView
  - `ModifyPlayerBuffLevelEvent`：更新層數顯示

## 與其他系統的關係

- **GameViewModel**：透過 ReactiveProperty 提供即時狀態
- **Factory**：BuffViewFactory 管理 BuffView 物件池
- **LocalizeLibrary**：提供 Buff 名稱與描述的本地化文字
- **SimpleTitleInfoHintView**：懸停提示框元件

## 相關文件

- [GameView 視覺呈現層](GameView.md) — BuffView 的父系統
- [Player 玩家系統](Player.md) — PlayerBuff 的邏輯層
- [CardBuff 卡牌 Buff](CardBuff.md) — 另一套 Buff 的 View 需求
- [Factory 工廠系統](Factory.md) — BuffViewFactory
