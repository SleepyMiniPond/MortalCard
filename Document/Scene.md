# Scene 系統 - 場景管理層架構總覽

## 🎯 系統定位與職責

**Scene 是 MortalGame 的場景管理層**，負責統籌各個遊戲場景的生命週期、場景間轉換流程，以及場景內部組件的初始化與協調。實現遊戲不同階段的流程控制與資源管理。

## 📊 系統架構設計

### 場景流程設計思路
**場景封裝層**：每個場景獨立封裝其內部邏輯與資源依賴
**轉換控制層**：統一管理場景載入、卸載與狀態轉換
**生命週期層**：協調場景內各系統的初始化與執行順序

### 核心場景職責

#### [GameplayScene](Assets/Scripts/Scene/GameplayScene.cs) - 核心戰鬥場景
統籌戰鬥流程的完整執行，協調 Context、BattleBuilder、GameplayPresenter 的初始化與運作

#### [LevelMapScene](Assets/Scripts/Scene/LevelMapScene.cs) - 關卡地圖場景  
管理世界地圖的導航與關卡選擇，提供玩家探索與進度管理介面

#### [MenuScene](Assets/Scripts/Scene/MenuScene.cs) - 主選單場景
處理遊戲啟動後的主要導航入口，目前實現基礎的等待輸入機制

#### [LoadingScene](Assets/Scripts/Scene/LoadingScene.cs) - 載入過渡場景
提供場景轉換期間的緩衝與載入進度顯示，確保流暢的用戶體驗

### 場景管理核心

#### [SceneLoadManager](Assets/Scripts/Scene/SceneLoadManager.cs) - 統一載入管理
提供標準化的場景載入介面，封裝 Unity SceneManager 的複雜度，確保場景轉換的可靠性

## 🔧 設計模式應用

### 工廠建構模式
GameplayScene 透過 [BattleBuilder](Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs) 實現複雜戰鬥場景的組裝與初始化

### 異步流程控制
所有場景使用 UniTask 實現非阻塞的場景流程管理

### 組件協調模式
場景作為各系統組件的協調器，管理 Presenter、View 等組件的生命週期

## 🌐 系統間協作

**與 Presenter 層的整合**：為各 Presenter 提供運作環境與初始化支援
**與資料層的連接**：透過 Context 傳遞確保場景間的資料一致性
**與 Unity 引擎的橋接**：封裝 Unity 場景系統，提供業務層友善的介面

## 🚀 場景擴展架構

### 模組化場景設計
每個場景獨立實現其核心邏輯，便於新增其他遊戲場景（如設定、商店等）

### 統一場景介面
SceneLoadManager 提供一致的載入模式，支援未來場景的快速整合
