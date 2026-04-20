# Player 玩家系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

玩家（Player）是戰鬥中最高層級的「控制單位」，管轄角色、卡牌、能量、Buff 等所有子系統。設計上採用**抽象基類 + 特化子類**的策略，AllyEntity 和 EnemyEntity 共享核心架構但各有獨特機制。

玩家系統也是 Composition Pattern 的最佳展示——PlayerEntity 不是一個萬行大類別，而是由 5 個 Manager 子系統組合而成。

## PlayerEntity — 抽象基類

### 組成結構

```
PlayerEntity（抽象）
├── Identity (Guid)           # 唯一身份
├── Faction (Ally/Enemy)      # 陣營
├── EnergyManager             # 能量管理
├── PlayerBuffManager         # 全域 Buff 管理
│   └── PlayerBuffEntity[]
├── PlayerCardManager         # 卡牌區域管理
│   ├── Deck                  # 牌組
│   ├── HandCard              # 手牌
│   ├── Graveyard             # 墓地
│   ├── ExclusionZone         # 排除區
│   └── DisposeZone           # 消耗區
├── Characters[]              # 角色集合
│   └── CharacterEntity
├── MainCharacter             # 主角色（第一個角色）
└── IsDead                    # 所有角色死亡
```

### Update 聚合更新

`Update(TriggerContext)` 方法是關鍵設計——它遍歷所有子系統（Buff、角色、角色Buff、卡牌），收集所有變化，打包成 `GeneralUpdateEvent` 回傳。這保證了 View 層可以在一次更新中獲得所有最新狀態。

## AllyEntity — 友軍特化

### 獨有機制

- **好感度系統**（DispositionManager）：管理 0 ~ Max 的好感度值
  - 好感度影響回合開始的能量恢復和抽牌數量
  - 透過 DispositionLibrary 查詢各等級的加成效果
- **Instance 連結**：保留 `OriginPlayerInstanceGuid`，用於與 AllyInstance 建立關聯（存檔/讀檔）

### 好感度的遊戲意義

好感度是 MortalGame 的特色機制之一——它代表友軍角色之間的關係深度。高好感度提供更多資源（能量、抽牌），鼓勵玩家在戰鬥中做出維護關係的選擇。

## EnemyEntity — 敵軍特化

### 獨有機制

- **AI 選牌**：`SelectedCardEntity` 追蹤 AI 在準備階段選定的卡牌
  - `TryGetRecommandSelectCard()` 使用貪心策略選取高費用卡牌
  - `SelectedCardMaxCount` 限制每回合最多選幾張牌
- **行為參數**：
  - `TurnStartDrawCardCount`：每回合自動抽牌數
  - `EnergyRecoverPoint`：回合能量回復量

## PlayerBuff — 玩家 Buff 系統

### 設計定位

PlayerBuff 是**全域性**的數值修正，影響該玩家控制的所有卡牌、角色。這是三套 Buff 中作用範圍最廣的。

### PlayerBuffData（設計時）

```
PlayerBuffData
├── ID, MaxLevel
├── Sessions{}            # 反應會話
├── BuffEffects{}         # GameTiming → ConditionalPlayerBuffEffect[]
├── PropertyDatas[]       # 全域屬性修正
└── LifeTimeData          # 生命週期策略
```

### 效果類型

| 效果 | 作用 |
|------|------|
| EffectiveDamagePlayerBuffEffect | 造成確實傷害 |
| AdditionalDamagePlayerBuffEffect | 造成追加傷害 |
| CardPlayEffectAttributeAdditionPlayerBuffEffect | 修改卡牌打出時的效果屬性 |
| AddCardBuffPlayerBuffEffect | 對所有卡牌施加 Buff |
| RemoveCardBuffPlayerBuffEffect | 移除所有卡牌上的 Buff |

### 屬性修正（16 種全域數值）

| 屬性 | 效果 | 計算方式 |
|------|------|----------|
| AllCardPower | 所有卡牌威力加成 | IIntegerValue |
| AllCardCost | 所有卡牌費用修正 | IIntegerValue |
| NormalDamageAddition | 普通傷害固定加成 | IIntegerValue |
| NormalDamageRatio | 普通傷害百分比加成 | float |
| MaxHealth | 最大生命值加成 | IIntegerValue |
| MaxEnergy | 最大能量加成 | IIntegerValue |
| HealRatio | 治療倍率 | float |

### PlayerBuffLibrary

查詢服務，使用 Option 模式。重要方法：
- `GetBuffEffects(buffId, GameTiming)` → `Option<ConditionalPlayerBuffEffect[]>`
- `GetBuffProperties(buffId)` → `IPlayerBuffPropertyData[]`

### PlayerBuffManager

管理玩家所有 Buff 的新增/移除/修改/更新。`Update()` 回傳變化的 Buff 集合。

## EnergyManager — 能量管理

能量是打出卡牌的資源，每回合恢復，打牌消耗。

### 操作類型

```
回合開始 → RecoverEnergy（RoundStartRecover）
打出卡牌 → ConsumeEnergy（PlayCardConsume）
效果獲得 → GainEnergy（GainEffect）
效果失去 → LoseEnergy（LoseEffect）
```

所有操作受 MaxEnergy 上限約束，回傳帶 Delta 的結果物件。

## PlayerCardManager — 卡牌總管理

協調五個卡牌區域和一個「打出中」狀態的複雜管理器。

### 關鍵流程

**打牌流程**：
```
TryPlayCard(CardEntity)
  → 從手牌移除卡牌
  → 設為 PlayingCard 狀態
  → 回傳 IDisposable
  → Dispose 時根據卡牌屬性決定去向：
    ├── Dispose 屬性 → DisposeZone
    └── 其他 → Graveyard
```

**回合結束清理**：
```
ClearHandOnTurnEnd()
  → 分離 Preserved 卡牌（保留）與其他卡牌（丟棄）
  → 產生 DiscardHandCardEvent
```

**卡牌回收**：
```
RecycleCardOnPlayEnd()
  → 從墓地篩選 Recycle 屬性的卡牌
  → 移回手牌
```

**跨區域搜尋**：
```
GetCardOrNone(Guid)
  → HandCard → Deck → Graveyard → ExclusionZone → DisposeZone
  → Option 鏈式查詢
```

## DispositionManager — 好感度管理（友軍獨有）

好感度值影響遊戲資源獲取：
- **IncreaseDisposition**：增加好感度（上限約束）
- **DecreaseDisposition**：減少好感度（下限 0）
- 好感度等級透過 DispositionLibrary 轉換為回復能量和抽牌加成

## Instance 層

### AllyInstance（Record 類型）

持久化友軍玩家狀態：
- Identity、NameKey、CurrentDisposition
- CurrentHealth、MaxHealth、CurrentEnergy、MaxEnergy
- Deck（CardInstance 列表）
- HandCardMaxCount

### 設計意義

AllyInstance 是跨場景保留玩家狀態的機制——戰鬥結束後，玩家的血量、好感度、牌組變化可以持久保存。

## 相關文件

- [Entity 實體系統](Entity.md) — PlayerEntity 在實體階層中的位置
- [Character 角色系統](Character.md) — 玩家管轄的角色
- [Card 卡牌系統](Card.md) — 玩家管理的卡牌
- [CardBuff 卡牌 Buff](CardBuff.md) — PlayerBuff 可以影響卡牌 Buff
- [Session 反應會話](Session.md) — Buff 動態狀態
- [Instance 實例層](Instance.md) — AllyInstance 的設計
