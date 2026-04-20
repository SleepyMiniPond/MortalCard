# Action 子系統 - 遊戲動作識別與追蹤機制

## 🎯 子系統定位與職責

**Action 子系統是 GameModel 中負責動作識別與事件追蹤的核心機制**，提供統一的動作描述介面，讓其他系統能夠識別「什麼動作由誰觸發」，並據此決定是否觸發額外的反應行為。

## 📊 動作系統架構設計

### 動作抽象層級設計
**動作介面層**：定義各種動作的共同契約與分類標準
**來源標識層**：標記動作的發起者與觸發脈絡  
**目標描述層**：定義動作影響的對象與範圍
**結果記錄層**：記錄動作執行的實際效果與數值

### 核心動作分類

#### 意圖動作 (Intent Actions)
**[IntentAction.cs](Assets/Scripts/GameModel/Action/IntentAction.cs)** 定義動作執行前的意圖聲明
- 涵蓋傷害、治療、護盾、能量調整等所有核心遊戲效果
- 提供動作預告機制，支援反應式效果的觸發判斷
- 標準化的 `BaseEffectIntentAction` 確保動作分類的一致性

#### 結果動作 (Result Actions)  
**[ResultTargetAction.cs](Assets/Scripts/GameModel/Action/ResultTargetAction.cs)** 記錄動作執行後的實際結果
- 包含具體的數值結果與狀態變化
- 提供完整的執行追蹤，支援連鎖反應的觸發
- 透過 `BaseResultAction` 建立結果記錄的標準格式

## 🔧 動作識別機制

### 來源追蹤系統
**[ActionSource.cs](Assets/Scripts/GameModel/Action/ActionSource.cs)** 實現動作來源的精確標識
- **卡牌來源**：`CardPlaySource` 記錄卡牌使用的完整脈絡
- **系統來源**：`SystemSource` 標記系統觸發的自動動作
- **觸發來源**：支援複雜的觸發鏈追蹤與來源關聯

### 目標識別系統
**[ActionTarget.cs](Assets/Scripts/GameModel/Action/ActionTarget.cs)** 定義動作影響對象
- **角色目標**：`CharacterTarget` 精確指向特定角色實體
- **玩家目標**：`PlayerTarget` 標識玩家層級的影響
- **系統目標**：`SystemTarget` 處理全域性的系統動作

### 觸發脈絡系統
**[TriggerSource.cs](Assets/Scripts/GameModel/Action/TriggerSource.cs)** 管理觸發事件的脈絡資訊
- **觸發上下文**：`TriggerContext` 提供完整的觸發環境描述
- **觸發來源**：支援卡牌、角色等不同觸發源的標識
- **反應決策**：為效果系統提供觸發判斷的資料基礎
