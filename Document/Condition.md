# Condition 子系統 - 多層條件判定組裝機制

## 🎯 子系統定位與職責

**Condition 子系統是 GameModel 中負責條件判定邏輯的組合式框架**，提供編輯器友善的條件組裝介面，讓設計師能夠在編輯器中方便地組合多層複雜的條件判定邏輯，支援靈活的觸發條件設計。

## 📊 條件系統架構設計

### 分層條件組裝設計
**基礎條件層**：提供各種實體與數值的基礎判定邏輯
**數值比較層**：實現數值條件的標準化比較操作  
**邏輯組合層**：支援條件間的邏輯運算與複合判斷
**編輯器表達層**：透過 Serializable 與 Odin Inspector 提供視覺化編輯

### 核心條件架構

#### 條件基礎介面
**[ModelCondition.cs](Assets/Scripts/GameModel/Condition/ModelCondition.cs)** 定義條件系統的核心架構
- **統一評估介面**：`ICondition` 提供基於 `TriggerContext` 的條件評估
- **分類專用介面**：`ICardBuffCondition`、`IPlayerBuffCondition` 等針對不同系統的專門介面
- **邏輯組合條件**：`AllCondition` 等支援複雜條件組合的邏輯運算

#### 數值條件判定
不同資料類型的條件判定專門介面：

**[IntegerValueCondition.cs](Assets/Scripts/GameModel/Condition/IntegerValueCondition.cs)** - 整數條件判定
- 標準化比較：`IntegerCompare` 支援等於、大於、小於等所有算術比較操作
- 動態數值支援：透過 `IIntegerValue` 實現基於遊戲狀態的條件判定
- 枚舉驅動：使用 `ArithmeticConditionType` 確保條件類型的一致性

**[BooleanValueCondition.cs](Assets/Scripts/GameModel/Condition/BooleanValueCondition.cs)** - 布林條件判定  
- 真假判定：`IsTrueCondition`、`IsFalseCondition` 提供基礎布林邏輯
- 等值比較：`IsEqualCondition` 支援布林值間的比較操作

### 實體條件判定

#### 遊戲實體條件
針對不同遊戲實體的條件判定邏輯：

**[CardValueCondition.cs](Assets/Scripts/GameModel/Condition/CardValueCondition.cs)** - 卡牌條件判定
- 卡牌比較：`CardEqualCondition` 透過 Identity 進行精確的卡牌比較
- 類型條件：`CardTypesCondition` 支援卡牌類型的條件判斷

**[PlayerValueCondition.cs](Assets/Scripts/GameModel/Condition/PlayerValueCondition.cs)** - 玩家條件判定
- 陣營關係：`PlayerFactionCondition` 判定玩家間的陣營關係（同陣營/敵對）
- 關係比較：支援複雜的玩家關係判斷邏輯

**[CharacterValueCondition.cs](Assets/Scripts/GameModel/Condition/CharacterValueCondition.cs)** - 角色條件判定
提供角色實體的專門條件判斷機制

#### 特殊事件條件
**[CardPlayValueCondition.cs](Assets/Scripts/GameModel/Condition/CardPlayValueCondition.cs)** - 卡牌使用條件
- 位置條件：`CardPlayPositionCondition` 判定卡牌在手牌中的位置（最新/最舊）
- 使用脈絡：基於 `CardPlaySource` 的豐富使用情境條件

## 🔧 編輯器組裝機制

### 視覺化條件編輯
所有條件類別使用 `[Serializable]` 與 `[HorizontalGroup]` 等 Odin Inspector 特性，提供直觀的條件組裝體驗

### 組合式邏輯設計
透過介面繼承與組合，支援任意複雜度的條件邏輯組裝

### 上下文驅動評估
所有條件評估基於統一的 `TriggerContext`，確保條件判斷的準確性與一致性

---

**子系統複雜度**：⭐⭐⭐⭐ (複雜的條件組合邏輯與編輯器支援)  
**維護重點**：條件評估準確性、組合邏輯完整性、編輯器使用體驗