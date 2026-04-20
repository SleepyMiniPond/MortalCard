# 專案系統架構總覽 - MortalGame

## 🎯 專案架構概述

**MortalGame 是基於 Unity 的卡牌遊戲專案**，採用 **MVC 架構模式**與**三層資料架構**設計，實現清晰的職責分離與高度可維護性。整個系統由六個核心層級組成，各司其職又密切協作。

## 🏗️ 核心系統架構

### 📊 [GameData 系統](GameData.md) - 資料定義層
**職責**：定義遊戲中所有實體的靜態數據結構與配置規範
- 卡牌、角色、增益效果等遊戲實體的資料定義
- ScriptableObject 封裝與編輯器工具整合
- 三層資料架構：Data → ScriptableObject → 運行時實例

### 🧠 [GameModel 系統](GameModel.md) - 核心遊戲邏輯層
**職責**：實現完整的卡牌遊戲戰鬥系統架構與遊戲邏輯
- Data-Instance-Entity 三層資料流轉架構
- Action-Target-Condition-Effect 四系統協作機制
- 戰鬥實體管理：Player-Character-Card 多層級實體架構
- 條件驅動的效果執行與複雜觸發邏輯支援

### 🎨 [GameView 系統](GameView.md) - 視覺呈現層  
**職責**：將 GameModel 的資料狀態轉換為玩家可見的視覺元素
- View-Presenter 分離設計，確保視覺與業務邏輯分工
- 物件池與工廠模式實現高效的 View 元件管理
- 事件驅動渲染，保持資料與視覺同步

### 🎮 [Presenter 系統](Presenter.md) - 控制協調層
**職責**：連接 GameModel 業務邏輯與 GameView 視覺呈現的協調樞紐
- MVP 架構中的關鍵協調角色，處理玩家輸入與遊戲流程
- 複雜的依賴注入與建構者模式應用
- 為未來 UI 系統預留擴展架構

### 🏠 [Scene 系統](Scene.md) - 場景管理層
**職責**：統籌各個遊戲場景的生命週期與轉換流程
- 場景封裝、轉換控制、生命週期管理
- 統一的場景載入管理與異步流程控制
- 模組化場景設計，便於新增遊戲場景

### 🛠️ [UI 系統](UI.md) - 介面工具層  
**職責**：提供輕量級的 UI 輔助元件與工具函數
- 畫面適配、視覺呈現、動態切換等工具元件
- 數值驅動設計，可重用組件架構
- 輕量化實現，專注單一職責

## 🔗 系統間協作關係

### 資料流向
```
GameData (靜態配置) → GameModel (核心邏輯) → Presenter (協調轉換) → GameView (視覺呈現)
                                    ↓
                                Scene (場景統籌) ← UI (工具支援)
```

### 核心依賴關係
- **Scene** 依賴 **Presenter** 進行場景內組件協調
- **Presenter** 依賴 **GameModel** 進行業務邏輯處理與狀態管理
- **GameModel** 依賴 **GameData** 獲取靜態配置資料並進行三層資料流轉
- **GameView** 依賴 **UI 工具** 實現視覺呈現細節
- **所有系統** 都基於 [編程規範指南](Coding_Standards.md) 的技術標準

## 📋 專案技術特色

### 現代 C# 技術棧
- **UniTask**：異步處理標準化
- **UniRx**：響應式事件系統
- **Record/IReadOnlyList**：不可變資料結構
- **Odin Inspector**：編輯器工具整合

### 架構設計原則  
- **職責分離**：清晰的系統邊界與職責劃分
- **三層資料架構**：Data → Instance → Entity 的資料流轉
- **可擴展性**：為未來功能預留架構空間
- **編輯器友善**：重視設計師工作流程體驗