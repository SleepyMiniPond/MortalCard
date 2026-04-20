# Action 動作系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

動作系統是 GameModel 的**語義層**，負責定義「遊戲中發生了什麼」的精確描述。每一個遊戲事件（打牌、造成傷害、觸發 Buff）都被建模為一個結構化的 Action 物件，攜帶完整的來源、目標與語境資訊。

核心設計原則：**每個 Action 都是不可變的描述**，而非命令式的操作。這使得 Buff 反應系統可以在效果實際執行前「審視」即將發生的事情，並進行修正。

## Action 階層體系

```
IActionUnit（根介面：所有動作都有 Timing 與 Source）
├── IActionTargetUnit（帶目標的動作）
├── IEffectAction（型別化效果動作）
│   ├── IEffectTargetAction（意圖階段 — 綁定目標前）
│   └── IEffectResultAction（結果階段 — 執行完畢後）
├── ITimingAction（遊戲時機動作，如回合開始）
├── ILookAction（查詢動作，讀取不修改）
├── IUpdateAction（更新動作，Session 值更新）
└── ICreateAction（創建動作，新實體產生）
```

## 三層效果管線

這是動作系統最精巧的設計——每個效果都經歷三個階段，每個階段都讓 Buff 反應系統有機會介入。

### 1. Intent（意圖宣告）

```
DamageIntentAction
HealIntentAction
ShieldIntentAction
GainEnergyIntentAction
...
```

**語義**：「我打算對某些目標造成 X 點傷害」
**Buff 介入點**：全域修正（例如「所有傷害 +2」的 Buff 在此修改數值）

### 2. TargetIntent（目標綁定）

```
DamageIntentTargetAction
HealIntentTargetAction
ShieldIntentTargetAction
...
```

**語義**：「我打算對這個特定目標造成 X 點傷害」
**Buff 介入點**：目標特化修正（例如「對該角色的傷害 +50%」）

### 3. Result（結果確認）

```
DamageResultAction
HealResultAction
ShieldResultAction
GainEnergyResultAction
...
```

**語義**：「這個目標實際受到了 Y 點傷害」
**Buff 介入點**：結果反應（例如「受到傷害時，回復 1 點護甲」）

### 設計價值

這三層管線的價值在於：
- **修正鏈**：多個 Buff 可以在不同階段疊加修正
- **條件精確化**：Buff 可以只對特定目標或特定結果做出反應
- **因果追溯**：每個結果都能追溯到原始意圖

## ActionSource — 動作來源

每個 Action 都標記「是誰觸發的」，這對條件判斷至關重要。

| Source 類型 | 語義 |
|------------|------|
| `SystemSource` | 系統自動觸發（回合開始、遊戲開始） |
| `CardPlaySource` | 卡牌打出觸發（攜帶手牌位置、屬性修正） |
| `CardPlayResultSource` | 卡牌打出結果（包裝 CardPlaySource + 效果結果） |
| `PlayerBuffSource` | 玩家 Buff 觸發 |
| `CardBuffSource` | 卡牌 Buff 觸發 |
| `SystemExecuteStartSource` | 行動階段開始 |
| `SystemExecuteEndSource` | 行動階段結束 |

### CardPlaySource 特殊設計

`CardPlaySource` 是最豐富的 Source 類型，攜帶：
- 打出的卡牌引用
- 手牌位置索引
- `CardPlayAttributeEntity`：屬性修正容器（費用加成、威力加成、傷害修正等）

這使得「根據卡牌位置」或「根據已累積的屬性修正」做條件判斷成為可能。

## ActionTarget — 動作目標

| Target 類型 | 語義 |
|-------------|------|
| `SystemTarget` | 無特定目標（系統事件） |
| `PlayerTarget` | 玩家實體 |
| `CharacterTarget` | 角色實體 |
| `CardTarget` | 卡牌實體 |
| `PlayerAndCardTarget` | 玩家與卡牌複合目標 |

## TriggerContext — 觸發上下文

```
TriggerContext = (IGameplayModel Model, ITriggeredSource Triggered, IActionUnit Action)
```

TriggerContext 是貫穿整個效果管線的**不可變上下文物件**。透過 Record 的 `with` 語法，可以安全地複製並修改特定欄位，而不影響原始上下文。

### ITriggeredSource 型別

| 觸發來源 | 語義 |
|----------|------|
| `SystemTrigger` | 系統觸發（遊戲邏輯） |
| `CardPlayTrigger` | 卡牌打出觸發 |
| `CardTrigger` | 卡牌本身觸發（觸發效果） |
| `PlayerBuffTrigger` | 玩家 Buff 觸發 |
| `CardBuffTrigger` | 卡牌 Buff 觸發 |

## 與其他系統的協作

- **Effect 系統**：Action 定義「要做什麼」，Effect 系統負責「怎麼做」
- **Condition 系統**：讀取 Action 的來源和目標來評估條件
- **Buff 反應系統**：在 Action 的三個階段（Intent/TargetIntent/Result）介入修正
- **GameContextManager**：提供 Action 計算所需的全域上下文
