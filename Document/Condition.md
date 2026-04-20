# Condition 條件系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

條件系統提供了一套**可組合的布林判斷框架**，廣泛用於 Buff 觸發條件、效果前置條件、Session 更新規則等場景。核心設計原則是：**所有條件都可以遞迴組合**，透過 All/Any/Inverse 邏輯運算子構建任意複雜的判斷邏輯。

## 條件階層體系

```
ICondition（根介面）
├── 邏輯運算
│   ├── ConstCondition（固定 true/false）
│   ├── AllCondition（AND 組合）
│   ├── AnyCondition（OR 組合）
│   └── InverseCondition（NOT 反轉）
│
├── 值比較條件
│   ├── IntegerCondition（數值比較 ==, >, <, >=, <=, !=）
│   ├── CardCondition（卡牌屬性條件）
│   ├── PlayerCondition（玩家屬性條件）
│   ├── CharacterCondition（角色屬性條件）
│   ├── PlayerBuffCondition（玩家 Buff 條件）
│   ├── CardPlayCondition（卡牌打出上下文條件）
│   ├── CardPlayResultCondition（效果結果條件）
│   └── SessionValueCondition（Session 值條件）
│
└── 特殊條件
    └── IsTriggeredOwnerTurnCondition（觸發者是否為當前行動方）
```

## 組合模式

條件系統的核心價值在於組合性。例如：

```
AllCondition（AND）
├── PlayerCondition（玩家陣營 == Ally）
├── AnyCondition（OR）
│   ├── CardCondition（卡牌類型 == Attack）
│   └── CardCondition（卡牌類型 == Defense）
└── InverseCondition（NOT）
    └── CharacterCondition（角色生命 > 50%）
```

語義：「當友方玩家打出攻擊或防禦卡牌，且目標角色生命不超過 50% 時」。

## 值比較條件詳解

### IntegerCondition

最基礎的數值比較，使用 `ArithmeticConditionType` 枚舉：
- `Equal`, `NotEqual`, `Greater`, `Less`, `GreaterOrEqual`, `LessOrEqual`

兩端數值都使用 `IIntegerValue` 介面，可以是常數、運算式或從實體屬性動態讀取。

### CardCondition / CardValueCondition

解析一個目標卡牌（透過 `ITargetCardValue`），然後對卡牌屬性進行判斷：
- `CardEqualCondition`：卡牌 ID 是否匹配
- `CardTypesCondition`：卡牌類型是否在指定集合中
- `CardPropertyCondition`：卡牌是否擁有特定屬性（Sealed、Preserved 等）
- `CardCollectionCondition`：卡牌當前所在區域是否匹配

### PlayerCondition / PlayerValueCondition

解析一個目標玩家，然後判斷：
- `FactionCondition`：玩家陣營
- `EnergyCondition`：能量數值比較
- `PlayerBuffCondition`：玩家是否擁有特定 Buff

### CharacterCondition / CharacterValueCondition

解析一個目標角色，判斷：
- `FactionCondition`：角色所屬陣營

### CardPlayCondition / CardPlayValueCondition

讀取卡牌打出上下文（CardPlaySource），判斷：
- 打出的卡牌屬性
- 手牌位置
- 卡牌類型等

### CardPlayResultCondition / CardPlayResultValueCondition

讀取效果執行結果，判斷：
- `DamageResultCondition`：傷害結果（總傷害值、是否穿透等）

### SessionValueCondition

讀取反應會話（ReactionSession）的當前值進行判斷，用於 Buff 觸發次數限制等機制。

## 求值過程

每個條件都接收 `TriggerContext` 作為參數：

```
TriggerContext 包含：
├── Model（遊戲模型 — 讀取全域狀態）
├── Triggered（觸發來源 — 誰觸發了這個判斷）
└── Action（當前動作 — 正在發生什麼）
```

條件透過 TriggerContext 存取所有需要的資訊，保持純函式的特性（相同輸入相同輸出）。

## 使用場景

| 場景 | 範例 |
|------|------|
| Buff 觸發條件 | 「當友方角色受到傷害時」觸發 Buff 效果 |
| 效果前置條件 | 「若手牌數 < 3，則抽 2 張牌」 |
| Session 更新規則 | 「當任意卡牌被打出時，計數器 +1」 |
| 卡牌效果條件 | 「若目標角色生命值 < 30%，傷害翻倍」 |

## 與其他系統的關係

- **Buff 系統**：每個 ConditionalBuffEffect 都包含 ICondition 陣列作為前置條件
- **Session 系統**：ConditionUpdateRule 在更新前先檢查條件
- **Action 系統**：條件經常讀取 Action 的 Source 和 Target 進行判斷
- **Target 系統**：條件使用 ITargetValue 系列介面解析目標實體
