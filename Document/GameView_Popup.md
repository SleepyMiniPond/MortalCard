# GameView Popup 彈窗面板

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Popup 面板負責**模態互動**——需要玩家注意力或選擇的臨時彈窗。這些面板在需要時顯示，完成後隱藏，使用 UniTask 實現非同步的等待-回應流程。

## 元件列表

### CardSelectionPanel — 卡牌多選面板

用於效果中的「選擇 N 張卡牌」互動（如「丟棄 2 張手牌」）：
- 追蹤已選卡牌（`_cardViewMap`）
- 根據選取數量更新確認按鈕狀態
- 支援「必須選取」模式（IsMustSelect）
- 支援最大選取數量限制
- 提供：描述文字、確認/關閉按鈕、可見性切換

### AllCardDetailPanel — 卡牌瀏覽面板

用於瀏覽卡牌集合（牌組、手牌、墓地）：
- `Open()`：顯示面板
- `Render()`：根據卡牌屬性建立顯示元件
- `Close()`：清理並隱藏
- 提供牌組/手牌/墓地的切換按鈕

### AllCardDetailPresenter — 卡牌瀏覽流程

狀態機控制瀏覽流程：
- **DeckEvent**：顯示牌組卡牌
- **HandCardEvent**：顯示手牌
- **GraveyardEvent**：顯示墓地
- **CardDetailEvent**：開啟單卡詳情彈窗
- **CloseEvent**：關閉瀏覽

使用 `IUniTaskPresenter` 進行非同步事件處理。

### SingleCardDetailPopupPanel — 單卡詳情彈窗

全螢幕的單張卡牌詳情展示：
1. 渲染卡牌視圖
2. 顯示 Buff 和關鍵字提示
3. 非同步等待玩家關閉
4. 清理並隱藏

### SimpleTitleInfoHintView — 提示框

通用的懸停提示框元件：
- 接收標題 + 說明文字
- **智慧定位**：避免超出畫面邊緣，必要時翻轉顯示方向
- 使用 `LayoutRebuilder` 確保文字換行正確

### 勝負結果面板

**GameResultWinPanel**：
- 最簡設計：顯示/隱藏勝利畫面
- 由 GameResultWinPresenter 控制

**GameResultLosePanel**：
- 提供三個按鈕：重試（Retry）、重新開始（Restart）、退出（Quit）
- 玩家選擇決定遊戲迴圈的下一步

**GameResultWinPresenter / GameResultLosePresenter**：
- 監聽按鈕事件，回傳結果型別
- LosePresenter 回傳 `GameplayLoseResult`（含反應類型：Retry/Restart/Quit）

### SubSelectionPresenter — 子選取流程

處理卡牌效果的多步驟選取互動：
- **事件**：SelectCardEvent、LongTouchCardEvent、ConfirmEvent、VisibleToggleEvent、CloseEvent
- **狀態管理**：追蹤已選卡牌、可見狀態
- **輸出**：`IReadOnlyDictionary<string, ISubSelectionAction>`（以選取群組 ID 為鍵）

### UniTaskPresenter — 非同步事件迴圈

泛型的非同步事件處理框架：
- 接收 Disposable、條件函式、取消令牌、事件處理器
- 透過 `TryEnqueueNextEvent()` 取出事件
- 非同步執行處理器
- 根據回傳值決定繼續（None）或停止（Halt）

被 UIPresenter、SubSelectionPresenter、AllCardDetailPresenter 共用。

## 相關文件

- [GameView_Panel 面板系統](GameView_Panel.md) — Popup 的父容器
- [Presenter 協調層](Presenter.md) — 觸發彈窗流程
- [CardView 卡牌視圖](CardView.md) — 卡牌詳情顯示
