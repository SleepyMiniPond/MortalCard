# GameView\Panel 系統 - 統一介面面板架構

## 🎯 系統定位與職責
GameView\Panel 系統是 MortalGame 中**統一的介面面板管理架構**，整合了遊戲中所有面板相關的視覺組件與互動邏輯。採用**分層設計**與**MVP 協調架構**，確保不同類型面板的清晰職責分工與高效協作。

## 🏗️ 系統架構總覽

### 三層面板架構
Panel 系統採用**功能導向的三層分類設計**：

#### 📊 [Info 層](GameView_Info.md) - 資訊顯示專層
**職責**：戰鬥過程中的即時資訊展示
- **我方/敵方狀態面板**：生命值、能量、性格的雙陣營對稱顯示
- **數值條組件**：HealthBar、EnergyBar 等視覺化數值元件
- **回合與提示**：TopBarInfo、GameKeyWord 等輔助資訊組件

#### 🎮 [UI 層](GameView_UI.md) - 操作工具專層  
**職責**：玩家操作與遊戲流程控制
- **牌堆/墓地互動**：DeckCardView、GraveyardCardView 的快捷操作入口
- **回合控制**：SubmitView 的流程管理組件
- **輕量化設計**：專注單一操作功能，避免複雜視覺處理

#### 🖼️ [Popup 層](GameView_Popup.md) - 彈出互動專層
**職責**：複雜的彈出視窗與深度互動
- **卡牌詳細檢視**：SingleCard、AllCard 的完整資訊展示
- **選擇互動系統**：SubSelection 的複雜卡牌選擇邏輯
- **遊戲結果面板**：Win/Lose 的結果狀態處理

### MVP 協調機制 - [UIPresenter](Assets/Scripts/GameView/Panel/UIPresenter.cs)
**設計要點**：
- **跨層協調樞紐**：連接 UI 層操作與 Popup 層展示
- **事件驅動橋接**：將 UI 按鈕點擊轉換為 Popup 詳細檢視
- **異步流程管理**：透過 UniTaskPresenter 處理複雜的面板切換流程

**協調流程**：
```csharp
UI Layer Button Click → UIPresenter Event → Popup Layer Detail Panel
```

## 🔗 系統間協作模式

### 資料流向設計
```
GameModel (資料源) 
    ↓
Info Layer (即時資訊顯示)
    ↓
UI Layer (操作觸發) → UIPresenter (協調轉換) → Popup Layer (詳細互動)
```

### 職責邊界清晰分工

| 層級 | 主要職責 | 複雜度 | 互動模式 | 生命週期 |
|------|----------|--------|----------|----------|
| **Info** | 資訊展示 | 簡單 | 被動響應 | 持久顯示 |
| **UI** | 操作觸發 | 輕量 | 主動點擊 | 常駐可用 |
| **Popup** | 深度互動 | 複雜 | 異步對話 | 按需開啟 |

### 技術整合統一性

#### 響應式更新機制
- **Info 層**：透過 UniRx 訂閱 GameViewModel 狀態變化
- **UI 層**：透過 Observable 監聽按鈕點擊與狀態更新
- **Popup 層**：透過 UniTask 處理異步互動與事件流

#### 本地化支援
- **統一 LocalizeLibrary**：三層都整合相同的本地化系統
- **多語言文字**：標題、說明、關鍵字的完整本地化支援
- **動態切換**：支援語言切換時的即時文字更新

#### 資源管理最佳化
- **Factory 模式**：UI/Popup 層共用 CardViewFactory 建立卡牌視圖
- **物件池整合**：減少重複建立相同類型組件的記憶體開銷
- **CompositeDisposable**：確保事件訂閱的正確生命週期管理


## 🔧 協作最佳實踐

### UIPresenter 的協調角色
- **事件轉換器**：將簡單的 UI 點擊轉換為複雜的 Popup 互動
- **生命週期管理**：確保 Popup 的正確開啟與關閉流程
- **取消支援**：透過 CancellationToken 處理異步流程的中斷

### 統一的設計模式
- **MVP 架構**：Presenter 處理邏輯，View 負責視覺
- **事件驅動**：基於 UniRx 與 UniTask 的響應式程式設計
- **資料驅動渲染**：所有組件都採用統一的資料更新模式
