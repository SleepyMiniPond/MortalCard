# Session 子系統 - 條件觸發與生命週期管理機制

## 🎯 子系統定位與核心價值

**Session 子系統是 GameModel\Entity 中專門處理「區域變數」型態機制的創新設計**，專門實現需要**特定生命週期**與**條件計數觸發**的遊戲機制。此系統讓設計師能在編輯器中快速配置複雜的觸發條件，例如「這回合內每3次出牌就XXX」或「這場戰鬥每5次受到攻擊就XXX」等機制。

## 🏗️ Session 系統三層架構

### 核心設計理念
**臨時狀態管理**：不同於永久性的 Buff 系統，Session 專門管理有明確生命週期的臨時狀態  
**條件觸發設計**：支援複雜的條件判斷與計數觸發機制  
**編輯器友善**：透過 Odin Inspector 提供設計師直觀的配置介面  
**生命週期自動化**：根據指定的生命週期範圍自動管理狀態重置與清理

### ReactionSession 核心機制

#### 反應會話實體
**[ReactionSessionEntity.cs](Assets/Scripts/GameModel/Entity/Session/ReactionSessionEntity.cs)** 反應會話的核心控制器
- **生命週期管控**：根據 `SessionLifeTime` 自動管理會話的啟動、重置、清理
- **數值封裝**：透過 Option 模式安全封裝 Boolean/Integer 數值存取
- **觸發更新**：接收 `TriggerContext` 並協調內部 SessionValue 的更新邏輯
- **狀態追蹤**：透過 `IsSessionValueUpdated` 追蹤會話是否有數值變化

#### 生命週期類型設計
```
📅 WholeGame：整場戰鬥的持續性會話
🔄 WholeTurn：單一回合內的臨時會話  
⚡ PlayCard：單次出牌過程的短期會話
```

### SessionValue 數值管理系統

#### 數值實體核心
**[SessionValueEntity.cs](Assets/Scripts/GameModel/Entity/Session/SessionValueEntity.cs)** 會話數值的具體實現
- **型別支援**：支援 Boolean 與 Integer 兩種基礎數值類型
- **規則驅動**：基於 TimingRule 系統，在特定時機觸發數值更新
- **條件判斷**：每個更新規則包含完整的條件檢查邏輯
- **運算支援**：Boolean 支援 AND/OR/覆寫，Integer 支援 加法/覆寫

#### Boolean 會話數值
- **邏輯運算**：`AndOrigin` 與原值 AND、`OrOrigin` 與原值 OR、`Overwrite` 直接覆寫
- **狀態追蹤**：適合實現開關型態的觸發機制
- **條件更新**：基於 `ConditionBooleanUpdateRule` 的條件驅動更新

#### Integer 會話數值  
- **計數機制**：`AddOrigin` 累積計數、`Overwrite` 重置計數
- **閾值判斷**：適合實現「每N次就觸發」的計數機制
- **數值運算**：基於 `ConditionIntegerUpdateRule` 的數值計算邏輯

### SelectedCard 選擇管理系統

#### 卡牌選擇實體
**[SelectedCardEntity.cs](Assets/Scripts/GameModel/Entity/Session/SelectedCardEntity.cs)** 臨時卡牌選擇狀態管理
- **容量控制**：透過 `MaxCount` 限制同時選擇的卡牌數量
- **選擇管理**：`TryAddCard`/`RemoveCard` 安全的卡牌選擇操作
- **批次操作**：`UnSelectAllCards` 一次性清空所有選擇
- **身份查詢**：透過 `TryGetCard` 根據卡牌 Identity 快速查找

## 🛠️ Data 層配置設計

### 編輯器配置優勢
**[ReactionSessionData.cs](Assets/Scripts/GameData/Session/ReactionSessionData.cs)** 為設計師提供強大的編輯器配置能力
- **視覺化配置**：透過 Odin Inspector 的 `[TableList]` 提供表格化的規則配置
- **時機選擇**：`[ValueDropdown]` 提供所有可用的 `GameTiming` 選項
- **規則組合**：支援多個 TimingRule 的複雜組合邏輯

## 🔧 更新規則系統

### 條件驅動更新
**[SessionValueUpdateRule.cs](Assets/Scripts/GameData/Session/SessionValueUpdateRule.cs)** 統一的條件更新框架
- **條件檢查**：每個更新規則包含完整的 `ICondition` 陣列
- **數值來源**：支援 `IBooleanValue`/`IIntegerValue` 動態數值計算
- **操作類型**：明確定義不同數值類型的可用操作

### 觸發時機整合
- **與 Action 系統協作**：接收各種遊戲動作的觸發信號
- **與 Condition 系統協作**：利用完整的條件判斷體系
- **與 Target 系統協作**：支援基於目標的條件判斷
