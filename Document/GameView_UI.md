# GameView UI 工具元件

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

UI 工具元件是最輕量的 View 子系統，提供簡單的按鈕互動和數值顯示。遵循**單一職責原則**——每個元件只做一件事。

## 元件列表

### DeckCardView — 牌組按鈕

- 顯示牌組剩餘卡牌數量
- 訂閱 `ViewModel.ObservableCardCollectionInfo(Faction.Ally, CardCollectionType.Deck)`
- 數量變化時自動更新文字
- 點擊觸發牌組瀏覽（透過 UIPresenter）

### GraveyardCardView — 墓地按鈕

- 顯示墓地卡牌數量
- 訂閱 `ViewModel.ObservableCardCollectionInfo(Faction.Ally, CardCollectionType.Graveyard)`
- 與 DeckCardView 相同的更新模式
- 點擊觸發墓地瀏覽

### SubmitView — 送出按鈕

- 回合結束按鈕
- 點擊時直接發送 `TurnSubmitCommand` 到 ActionReceiver
- 最簡單的互動：一個按鈕、一個命令

## 設計特點

這三個元件體現了「最小必要」的設計原則：
- 沒有複雜的狀態管理
- 沒有動畫效果
- 直接透過 UniRx 訂閱或簡單事件回調
- 各自獨立，不互相依賴

## 相關文件

- [GameView_Panel 面板系統](GameView_Panel.md) — UI 的父容器
- [GameView 視覺呈現層](GameView.md) — 事件分發與整合
