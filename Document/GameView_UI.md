# GameView\UI 系統 - 介面操作工具組件

## 🎯 系統定位
GameView\UI 系統提供專注於玩家操作與遊戲流程控制的輕量級介面組件。不同於 Info 系統專注於資訊顯示，UI 系統的組件主要負責**觸發遊戲動作**與**快捷操作入口**。

## 🏗️ 核心設計理念

### 操作導向設計
每個 UI 組件都圍繞**特定玩家操作**設計：
- **按鈕驅動**：主要互動透過按鈕點擊觸發
- **狀態響應**：即時反映遊戲狀態變化
- **動作整合**：直接與 GameModel 的動作系統連接

## 📋 核心組件詳析

### 牌堆互動組件 - [DeckCardView](Assets/Scripts/GameView/Panel/UI/DeckCardView.cs)
**設計要點**：
- **數量監控**：即時顯示牌堆剩餘卡牌數量
- **響應式更新**：透過 UniRx 訂閱 CardCollectionInfo 變化
- **操作入口**：提供牌堆相關操作的觸發點（透過 DeckButton）

### 墓地互動組件 - [GraveyardCardView](Assets/Scripts/GameView/Panel/UI/GraveyardCardView.cs)
**設計要點**：
- **墓地狀態追蹤**：監控墓地卡牌數量變化
- **對稱設計**：與 DeckCardView 採用相同的設計模式
- **快捷存取**：提供墓地檢視的快速入口

### 回合提交組件 - [SubmitView](Assets/Scripts/GameView/Panel/UI/SubmitView.cs)
**設計要點**：
- **動作觸發器**：直接觸發遊戲核心動作
- **流程控制**：控制回合結束的關鍵操作
- **指令整合**：與 GameplayActionReciever 直接連接

## 🔄 系統設計模式

### 響應式狀態同步
**設計優勢**：
- 確保 UI 狀態與遊戲資料的即時同步
- 透過 CompositeDisposable 管理訂閱生命週期
- 避免手動輪詢檢查狀態變化

### 指令模式整合
**設計優勢**：
- 清晰的操作與結果對應關係
- 透過指令模式與 GameModel 解耦
- 支援複雜的動作組合與序列
