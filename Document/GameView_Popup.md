# GameView\Popup 系統 - 彈出視窗與互動面板

## 🎯 系統定位
GameView\Popup 系統負責管理遊戲中的各種彈出視窗、詳細資訊面板與玩家互動介面。採用 **MVP 架構**設計，透過 Presenter 處理複雜的異步互動流程，並提供統一的事件驅動機制。

## 🏗️ 核心設計架構

### MVP 架構分離設計
系統嚴格遵循 **Presenter-View 分離**原則：
- **Presenter**：處理業務邏輯、事件協調、異步流程控制
- **Panel/View**：負責純粹的視覺呈現與基礎互動回應
- **UniTaskPresenter**：提供統一的異步事件處理框架

### 事件驅動互動模式
所有 Popup 都採用**基於事件的異步互動模式**：
- Record 類型事件定義，確保型別安全
- UniTask 支援的異步生命週期管理
- 統一的開啟-互動-關閉流程

## 📋 核心 Popup 系統

### 卡牌詳細資訊系統
#### [AllCardDetailPresenter](Assets/Scripts/GameView/Panel/Popup/AllCardDetailPresenter.cs) + [AllCardDetailPanel](Assets/Scripts/GameView/Panel/Popup/AllCardDetailPanel.cs)
**設計要點**：
- **多重卡牌瀏覽**：同時顯示牌組、手牌、墓地的所有卡牌
- **分類切換機制**：透過 Deck/HandCard/Graveyard 按鈕切換不同卡牌集合
- **CardDetailProperty 封裝**：整合卡牌資訊、Buff 提示、關鍵字提示的複合資料結構
- **工廠模式整合**：透過 CardViewFactory 動態建立卡牌視圖組件

#### [SingleCardDetailPopupPanel](Assets/Scripts/GameView/Panel/Popup/SingleCardDetailPopupPanel.cs)
**設計要點**：
- **單卡詳細展示**：專注於單張卡牌的完整資訊呈現
- **提示系統整合**：整合 CardPropertyHint 顯示 Buff 與關鍵字詳細說明
- **異步生命週期**：完整的開啟-等待-關閉異步流程控制

### 卡牌選擇與互動系統
#### [SubSelectionPresenter](Assets/Scripts/GameView/Panel/Popup/SubSelectionPresenter.cs) + [CardSelectionPanel](Assets/Scripts/GameView/Panel/Popup/CardSelectionPanel.cs)
**設計要點**：
- **複雜選擇邏輯處理**：支援多種卡牌選擇模式（ExistCardSelectionInfo）
- **選擇狀態管理**：追蹤已選卡牌、最大選擇數量、確認狀態
- **字典式回傳**：透過 IReadOnlyDictionary 回傳選擇結果
- **視覺回饋控制**：支援面板顯示/隱藏切換、選擇描述文字更新

### 遊戲結果展示系統
#### [GameResultWinPresenter](Assets/Scripts/GameView/Panel/Popup/GameResultWinPresenter.cs) / [GameResultLosePresenter](Assets/Scripts/GameView/Panel/Popup/GameResultLosePresenter.cs)
**設計要點**：
- **結果狀態處理**：封裝勝利/失敗的結果資料結構
- **簡潔的異步等待**：提供基礎的結果展示與關閉機制
- **未來擴展預備**：為複雜的結果動畫與統計資訊預留架構空間

## 🔧 共用基礎設施

### 異步事件處理框架 - [UniTaskPresenter](Assets/Scripts/GameView/Panel/Popup/UniTaskPresenter.cs)
**設計要點**：
- **事件佇列機制**：支援事件的排隊與順序處理
- **條件式執行**：透過 conditionFunc 控制異步流程的執行條件
- **取消支援**：完整的 CancellationToken 整合
- **事件型別安全**：基於 Record 的事件型別系統

### 提示系統 - [SimpleTitleInfoHintView](Assets/Scripts/GameView/Panel/Popup/SimpleTitleIInfoHintView.cs)
**設計要點**：
- **智慧定位**：根據目標位置自動調整提示框顯示方向
- **本地化支援**：完整的多語言標題與內容顯示
- **Layout 自適應**：自動重建版面配置以適應內容長度
- **生命週期管理**：統一的顯示-隱藏生命週期控制

## 🔗 系統間協作關係

### 與 GameModel 的資料流
- 接收 **CardInfo、CardCollectionInfo** 等遊戲資料結構
- 透過 **IGameViewModel** 訂閱遊戲狀態變化
- 回傳選擇結果給 GameModel 進行後續處理
