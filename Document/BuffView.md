# BuffView 子系統 - 增益效果視覺化呈現機制

## 🎯 子系統定位與職責

**BuffView 子系統是 GameView 中負責增益效果視覺化的專門機制**，提供統一的 Buff 狀態視覺呈現與玩家互動介面。此系統採用**模板化設計**與**響應式更新**，確保不同類型的 Buff 效果都能獲得一致且直觀的視覺回饋。

## 📊 BuffView 系統架構設計

### 視覺分層設計
**資訊抽象層**：統一的 BuffInfo 資料結構，封裝 Buff 的視覺化資訊
**視覺呈現層**：具體的 BuffView 元件，處理圖示、數值、互動等視覺元素
**集合管理層**：BuffCollectionView 統籌多個 Buff 的集合顯示與生命週期
**工廠支援層**：透過 Factory 模式實現 BuffView 的高效創建與回收

### 核心 BuffInfo 資訊架構

#### PlayerBuffInfo 資料結構
**[PlayerBuffInfo.cs](Assets/Scripts/GameView/BuffView/PlayerBuffInfo.cs)** 玩家增益的視覺化資訊封裝
- **身份識別**：透過 `Id` 連接 GameData 定義，`Identity` 追蹤唯一實例
- **等級顯示**：`Level` 提供 Buff 疊加層數的視覺化數值
- **會話數據**：`SessionIntegers` 封裝 Session 系統的動態計數資訊
- **模板支援**：`GetTemplateValues()` 為本地化文字提供動態數值替換

#### CharacterBuffInfo 資料結構
**[CharacterBuffInfo.cs](Assets/Scripts/GameView/BuffView/CharacterBuffInfo.cs)** 角色增益的視覺化資訊封裝
- **統一設計**：與 PlayerBuffInfo 保持一致的資料結構設計
- **角色特化**：專門針對角色層級 Buff 的視覺化需求
- **模板一致性**：相同的模板值生成機制，確保視覺呈現的統一性

### PlayerBuff 視覺化系統

#### PlayerBuffView 核心視覺元件
**[PlayerBuffView.cs](Assets/Scripts/GameView/BuffView/PlayerBuffView.cs)** 單一玩家 Buff 的完整視覺呈現
- **視覺元素**：整合 `_buffIcon` 圖示與 `_levelText` 等級顯示
- **響應式更新**：透過 UniRx 訂閱 GameModel 的 Buff 狀態變化
- **互動支援**：滑鼠懸停顯示詳細資訊，離開時自動隱藏
- **資源管理**：實現 `IRecyclable` 介面，支援物件池回收機制

#### 響應式更新機制
- **即時同步**：Buff 狀態變化立即反映到視覺呈現
- **自動清理**：CompositeDisposable 確保訂閱的正確清理
- **記憶體安全**：避免記憶體洩漏與無效訂閱

#### PlayerBuffCollectionView 集合管理
**[PlayerBuffCollectionView.cs](Assets/Scripts/GameView/BuffView/PlayerBuffCollectionView.cs)** 玩家所有 Buff 的集合管理

## 💡 互動體驗設計

### 資訊提示系統
- **懸停顯示**：滑鼠懸停時顯示詳細的 Buff 資訊
- **即時反饋**：Buff 等級與效果數值的即時更新
- **優雅隱藏**：滑鼠離開時自動隱藏詳細資訊

### 視覺回饋機制
- **等級指示**：清晰的數值顯示 Buff 疊加層數
- **圖示識別**：直觀的圖示幫助玩家快速識別 Buff 類型
- **動態更新**：Buff 狀態變化時的平滑視覺轉換

## 🔄 與其他系統協作

### 與 GameModel 系統協作
**資料來源**：接收 GameModel 中 PlayerBuff/CharacterBuff 的狀態資料

### 與 Session 系統協作
**會話數據**：顯示 Session 系統的計數與狀態資訊

### 與本地化系統協作
**文字模板**：透過 LocalizeLibrary 提供多語言的 Buff 描述

### 與 Factory 系統協作
**物件管理**：利用工廠模式實現高效的視覺元件生命週期管理
