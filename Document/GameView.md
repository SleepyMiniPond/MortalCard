# GameView 系統 - 視覺呈現層架構總覽

## 🎯 系統定位與職責

**GameView 是 MortalGame 的視覺呈現層**，負責將 GameModel 的資料狀態轉換為玩家可見的視覺元素與互動介面。採用**View-Presenter 分離設計**，確保視覺邏輯與業務邏輯的清晰分工。

## 📊 系統架構設計

### 視覺分層設計思路
**View 元件層**：負責具體的視覺呈現與基礎互動回應
**Factory 創建層**：管理 View 元件的生命週期與資源回收
**Presenter 協調層**：處理複雜的視覺邏輯與跨 View 協作

### 核心子系統職責

#### [CardView](CardView.md) - 卡牌視覺系統
處理所有卡牌的視覺化與互動，涵蓋手牌弧形佈局、敵方卡牌展示、拖拽互動、詳細資訊展示等完整卡牌視覺體驗

#### [CharacterView](CharacterView.md) - 角色視覺系統
管理盟友與敵人角色的視覺呈現，提供事件驅動的動畫系統，處理傷害、治療、能量、性情等角色狀態變化的豐富動畫效果

#### [BuffView](BuffView.md) - 增益視覺系統
負責各種 Buff 效果的視覺化呈現，採用響應式更新與模板化設計，提供統一的 PlayerBuff/CharacterBuff 視覺呈現與互動體驗

#### [EventView](EventView.md) - 事件視覺系統
處理遊戲效果的動畫展演，提供傷害、治療、護盾、能量、性情等事件的即時數值動畫與 Timeline 視覺回饋

#### [Factory](Factory.md) - 工廠創建系統
提供統一的 View 元件工廠與物件池管理，優化記憶體使用與創建效能

#### [Panel](GameView_Panel.md) - 統一介面面板架構
採用三層面板設計（Info/UI/Popup）與 MVP 協調機制，提供完整的介面管理系統，包含即時資訊顯示、操作工具、彈出互動等全方位面板功能

### 互動選擇機制
透過 [ISelectableView](Assets/Scripts/GameView/ISelectableView.cs) 介面統一處理玩家選擇互動，支援靈活的目標指定系統

## 🔧 設計模式應用

### 工廠模式與物件池
[PrefabFactory](Assets/Scripts/GameView/Factory/PrefabFactory.cs) 結合物件池實現高效的 View 元件管理

### 介面驅動設計
透過 ISelectableView、IRecyclable 等介面確保系統擴展性

### 事件驅動渲染
基於 GameModel 事件流進行視覺更新，保持資料與視覺的同步

## 🌐 系統間協作

**與 GameModel 的連接**：接收遊戲狀態變化，轉換為視覺呈現
**與 UI 系統的整合**：提供互動回饋，處理玩家操作輸入
**與資源管理的協作**：透過 Factory 系統優化效能與記憶體使用
