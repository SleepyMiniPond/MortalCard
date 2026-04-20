# CardView 子系統 - 卡牌視覺化與互動機制

## 🎯 子系統定位與職責

**CardView 子系統是 GameView 中負責所有卡牌視覺化與互動的核心機制**，涵蓋從手牌、敵方卡牌到詳細資訊展示的完整卡牌視覺體驗。此系統採用**多場景適配設計**與**統一介面架構**，確保卡牌在不同遊戲情境下都能提供一致且豐富的視覺表現。

## 📊 CardView 系統架構設計

### 多場景卡牌視覺設計
**基礎視覺層**：CardView/AiCardView 提供卡牌的核心視覺呈現  
**集合管理層**：AllyHandCardView/EnemySelectedCardView 管理卡牌集合的佈局與互動  
**詳細展示層**：FocusCardDetailView/CardDetailInfoView 提供卡牌的詳細資訊展示  
**資訊抽象層**：CardInfo/CardStatusInfo 封裝卡牌的視覺化資料

### 核心資訊架構

#### CardInfo 卡牌資訊記錄
**[CardInfo.cs](Assets/Scripts/GameView/CardView/CardInfo.cs)** 卡牌視覺化的完整資訊封裝
- **身份追蹤**：透過唯一識別碼建立卡牌實例與資料的對應關係
- **動態數值**：包含原始與當前的費用、威力數值變化
- **增益整合**：整合 BuffInfo、Properties、Keywords 的完整狀態
- **模板支援**：為本地化文字提供動態數值替換機制

### 基礎卡牌視覺元件

#### CardView 主要卡牌視覺
**[CardView.cs](Assets/Scripts/GameView/CardView/CardView.cs)** 盟友卡牌的完整視覺實現
- **多介面整合**：同時實現回收、選擇、拖拽等多種功能介面
- **多渲染模式**：支援手牌互動、點擊回應、簡化展示等不同渲染需求
- **拖拽系統**：完整的拖拽狀態管理與目標識別機制
- **位置動畫**：支援平滑的位置調整與偏移動畫
- **聚焦功能**：處理卡牌聚焦時的內容顯示與隱藏

#### AiCardView 敵方卡牌視覺
**[AiCardView.cs](Assets/Scripts/GameView/CardView/AiCardView.cs)** 敵方卡牌的簡化視覺實現
- **目標類型**：明確標識為敵方卡牌目標以支援目標選擇系統
- **簡化介面**：專注於基本資訊顯示，不包含拖拽等複雜互動
- **統一渲染**：使用相同的本地化與模板系統確保視覺一致性

## 🎴 集合管理系統

### AllyHandCardView 盟友手牌管理
**[AllyHandCardView.cs](Assets/Scripts/GameView/CardView/AllyHandCardView.cs)** 盟友手牌的集合視覺管理
- **弧形佈局**：實現優雅的手牌弧形排列，模擬真實卡牌手感
- **聚焦動畫**：控制卡牌聚焦時其他卡牌的位移避讓效果
- **拖拽支援**：完整的拖拽開始、進行、結束流程管理
- **箭頭指示**：提供拖拽時的視覺引導線系統
- **集合維護**：透過雙重索引機制維護卡牌集合的高效管理

### EnemySelectedCardView 敵方選擇卡牌管理
**[EnemySelectedCardView.cs](Assets/Scripts/GameView/CardView/EnemySelectedCardView.cs)** 敵方選擇卡牌的集合管理
- **線性佈局**：實現整齊的水平排列展示敵方選擇的卡牌
- **牌庫指示**：整合牌庫按鈕與計數文字顯示敵方牌庫資訊
- **選擇追蹤**：追蹤敵方選擇的卡牌並提供適當的視覺回饋
- **響應式更新**：自動同步牌庫數量變化的即時顯示

## 🔍 詳細展示系統

### FocusCardDetailView 聚焦詳細展示
**[FocusCardDetailView.cs](Assets/Scripts/GameView/CardView/FocusCardDetailView.cs)** 卡牌聚焦時的詳細資訊展示
- **動態定位**：根據目標卡牌位置智慧調整詳細面板的顯示位置
- **完整資訊**：整合卡牌基本資訊、Buff 詳情、關鍵字說明的完整展示
- **提示整合**：提供 Buff 與關鍵字的詳細說明提示系統
- **Canvas 適配**：確保在不同解析度下正確的螢幕坐標轉換

### CardDetailInfoView 資訊展示面板
**[CardDetailInfoView.cs](Assets/Scripts/GameView/CardView/CardDetailInfoView.cs)** 獨立的卡牌詳細資訊展示元件

## 🎮 互動介面設計

### ICardView 統一介面
CardView 定義了完整的卡牌視覺介面，整合回收、選擇、拖拽等多種功能需求，提供初始化、多種渲染模式、位置管理等核心功能。

### 拖拽系統設計
拖拽系統提供開始拖拽與持續拖拽的介面，支援拖拽狀態的即時回饋，包含無目標、有效目標、無效目標等狀態指示，確保玩家獲得清晰的拖拽回饋。

### 渲染屬性系統
- **RuntimeHandCardProperty**：手牌的完整互動屬性，包含懸停與拖拽事件處理
- **CardClickableProperty**：可點擊卡牌的點擊與長按事件配置
- **CardSimpleProperty**：簡化的卡牌資訊展示模式