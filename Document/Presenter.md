# Presenter 系統 - 控制協調層架構總覽

## 🎯 系統定位與職責

**Presenter 是 MortalGame 的控制協調層**，負責連接 GameModel 業務邏輯與 GameView 視覺呈現，實現 MVP 架構模式中的關鍵協調角色。處理玩家輸入、遊戲流程控制與跨系統資料協調。

## 📊 系統架構設計

### MVP 協調設計思路
**輸入處理層**：接收並轉換玩家操作為遊戲指令
**流程控制層**：管理遊戲狀態轉換與業務流程執行  
**資料協調層**：在 Model 與 View 之間進行資料轉換與同步

### 核心子系統職責

#### [Gameplay](Assets/Scripts/Presenter/Gameplay) - 遊戲流程控制系統
管理核心戰鬥流程，處理玩家操作輸入，協調戰鬥邏輯與視覺呈現的同步

#### [LevelMap](Assets/Scripts/Presenter/LevelMap) - 關卡地圖控制系統
控制地圖導航與關卡選擇，處理玩家在世界地圖中的移動與互動

### 關鍵組件架構

#### 遊戲流程管理
- **GameplayPresenter**：核心戰鬥流程的統一控制點
- **Context**：遊戲資料的集中管理與依賴注入容器
- **BattleBuilder**：戰鬥場景的建構與初始化邏輯

#### 玩家互動處理
- **InterAction 子系統**：標準化的玩家指令處理與驗證機制
- **IGameplayActionReceiver**：統一的玩家操作接收介面

## 🔧 設計模式應用

### 建構者模式
透過 [BattleBuilder](Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs) 統一管理複雜的戰鬥場景初始化

### 依賴注入容器
[Context](Assets/Scripts/Presenter/Gameplay/Context.cs) 提供集中化的資料依賴管理

### 指令模式
InterAction 系統實現標準化的玩家操作處理流程

## 🌐 系統間協作

**與 GameModel 的連接**：將業務邏輯結果轉換為 View 可理解的資料格式
**與 GameView 的協調**：處理視覺回饋與玩家輸入的雙向溝通
**與資料層的整合**：透過 Context 與 ScriptableDataLoader 統一管理遊戲資料

## 🚀 擴展架構設計

### 模組化設計
各 Presenter 模組獨立運作，便於新增不同遊戲場景的控制邏輯
