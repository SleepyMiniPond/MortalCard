# Session 反應會話系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Session 系統解決了 Buff 系統中一個精巧的需求：**Buff 需要追蹤動態狀態**。

例如一個 Buff 的效果是「每回合首次造成傷害時，額外造成 2 點傷害」。為了實現「首次」這個限制，Buff 需要記住「本回合是否已經觸發過」。Session 就是為此而生的——它是 Buff 附帶的可變狀態容器，根據遊戲時機自動更新。

## 核心概念

### ReactionSessionData（設計時定義）

每個 Session 定義：
- **初始值**：Boolean 或 Integer
- **生命週期**（LifeTime）：決定何時重置
  - `WholeGame`：整場遊戲存在
  - `WholeTurn`：每回合重置
  - `PlayCard`：每次打牌重置
- **更新規則**：在特定 GameTiming、滿足特定條件時，如何更新值

### ReactionSessionEntity（運行時實體）

持有當前值，並在遊戲事件發生時評估更新規則。

## 兩種會話型別

### SessionBoolean — 布林狀態

適用於「是否已觸發」類型的追蹤。

**更新操作**：
| 操作 | 行為 |
|------|------|
| Overwrite | 直接覆寫為新值 |
| AndOrigin | 新值 = 舊值 AND 更新值 |
| OrOrigin | 新值 = 舊值 OR 更新值 |

**典型用途**：
- 「本回合是否已造成傷害」→ 初始 false，TurnStart 重置為 false，造成傷害時 OrOrigin true
- 條件效果檢查此值：若為 false（尚未觸發），觸發效果

### SessionInteger — 整數計數器

適用於「觸發了幾次」或「累計了多少」類型的追蹤。

**更新操作**：
| 操作 | 行為 |
|------|------|
| Overwrite | 直接覆寫為新值 |
| AddOrigin | 新值 = 舊值 + 更新值 |

**典型用途**：
- 「本回合打出幾張牌」→ 初始 0，TurnStart 重置為 0，每次打牌 AddOrigin 1
- 條件效果檢查此值：若 ≤ 3，觸發額外效果

## 更新規則

每個 Session 可以有多組更新規則，每組綁定到特定的 GameTiming：

```
TimingRule
├── GameTiming        # 何時觸發此規則（TurnStart、PlayCardEnd 等）
└── UpdateRules[]     # 條件更新規則列表
    └── ConditionUpdateRule
        ├── Conditions[]  # 前置條件（全部滿足才執行）
        └── NewValue      # 更新值
        └── Operation     # 更新操作（Overwrite/And/Or/Add）
```

**求值流程**：
1. 遊戲觸發特定 GameTiming
2. 找到匹配該 Timing 的所有 TimingRule
3. 對每個 ConditionUpdateRule：
   a. 評估所有 Conditions
   b. 全部通過 → 執行更新操作
   c. 有任一未通過 → 跳過

## 生命週期管理

Session 的重置時機由 LifeTime 決定：

| LifeTime | 重置時機 | 典型場景 |
|----------|----------|----------|
| WholeGame | 不重置（或手動重置） | 累計整場遊戲的統計 |
| WholeTurn | TurnStart 時重置 | 「每回合首次」限制 |
| PlayCard | PlayCardStart 時重置 | 「本次打牌」內的追蹤 |

### 延遲初始化

Session 值採用**延遲初始化**——在首次被存取時才建立，這避免了不必要的物件建立開銷。

## 與 Buff 系統的整合

```
Buff 效果觸發流程：

1. GameTiming 發生（例如 TurnStart）
2. Buff 的 ReactionSessions 收到通知
   → Session 根據 UpdateRules 更新自己的值
3. Buff 的 ConditionalEffect 被檢查
   → 條件中可能包含 SessionValueCondition
   → SessionValueCondition 讀取 Session 當前值
   → 判斷是否滿足觸發條件
4. 若條件通過 → 執行 Buff 效果
```

## SelectedCardEntity（附屬實體）

Session 子目錄下還包含 `SelectedCardEntity`，用於追蹤敵人 AI 選定的卡牌：
- `Cards`：已選定的卡牌集合
- `MaxCount`：選取上限
- `TryAddCard()` / `RemoveCard()` / `UnSelectAllCards()`

這與 Session 系統的「狀態追蹤」哲學一致——追蹤一個臨時的、會在回合間重置的狀態。

## 設計價值

1. **關注點分離**：Buff 的「效果」和「追蹤狀態」分開定義
2. **可組合性**：Session 的條件和更新規則可以任意組合
3. **宣告式**：設計師只需定義「什麼時候更新」和「怎麼更新」，執行邏輯自動完成
4. **生命週期明確**：每個 Session 的有效範圍清晰定義

## 相關文件

- [CardBuff 卡牌 Buff](CardBuff.md) — Session 的宿主之一
- [Player 玩家系統](Player.md) — PlayerBuff 的 Session
- [Character 角色系統](Character.md) — CharacterBuff 的 Session
- [Condition 條件系統](Condition.md) — SessionValueCondition 讀取 Session 值
- [GameModel 核心邏輯](GameModel.md) — GameTiming 觸發 Session 更新
