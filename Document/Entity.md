# Entity 實體系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Entity 系統是三層資料架構（Data → Instance → Entity）的最終層，代表**戰鬥中活躍的可變狀態物件**。每個 Entity 都是一個「活的」遊戲元素，擁有唯一身份（Guid）、可變狀態、以及透過 Manager 子系統實現的複雜行為。

核心設計原則：**Composition over Inheritance**。PlayerEntity 不是一個巨型類別，而是由 EnergyManager、PlayerCardManager、PlayerBuffManager、CharacterEntity 等子系統組合而成。

## 實體階層總覽

```
PlayerEntity（玩家實體）
├── EnergyManager          # 能量管理
├── PlayerBuffManager      # 玩家 Buff 管理
│   └── PlayerBuffEntity[] # 各個 Buff 實體
├── PlayerCardManager      # 卡牌區域管理
│   ├── DeckEntity         # 牌組
│   ├── HandCardEntity     # 手牌
│   ├── GraveyardEntity    # 墓地
│   ├── ExclusionZoneEntity # 排除區
│   └── DisposeZoneEntity  # 消耗區
└── CharacterEntity[]      # 角色集合
    ├── HealthManager      # 血量/護甲管理
    └── CharacterBuffManager
        └── CharacterBuffEntity[]

CardEntity（卡牌實體）
├── CardBuffManager        # 卡牌 Buff 管理
│   └── CardBuffEntity[]
└── CardPropertyEntity[]   # 卡牌屬性集合
```

## PlayerEntity — 玩家實體

抽象基礎類別，友軍（AllyEntity）與敵軍（EnemyEntity）共用核心架構，各自特化。

### 共通能力
- 擁有一組角色（Characters）
- 管理能量（EnergyManager）
- 管理全域 Buff（PlayerBuffManager）
- 管理所有卡牌區域（PlayerCardManager）
- 判斷死亡（所有角色死亡時）
- 統合更新（`Update()` 聚合所有子系統的變化，回傳 GeneralUpdateEvent）

### AllyEntity — 友軍特化
- **好感度系統**：`DispositionManager` 管理好感度值，影響回合抽牌與能量恢復
- **Instance 連結**：保留 `OriginPlayerInstanceGuid` 供存檔還原

### EnemyEntity — 敵軍特化
- **AI 選牌**：`SelectedCardEntity` 追蹤 AI 選定的卡牌
- **行為參數**：回合抽牌數、能量回復值、最大選牌數

### Dummy 模式
`DummyPlayer` 提供空實作（Null Object Pattern），避免空值檢查散佈各處。

## CharacterEntity — 角色實體

代表一個戰鬥角色（英雄、盟友或敵人），是**血量與勝負判定**的核心單位。

### 組成
- **HealthManager**：管理生命值（HP）與護甲值（Dp/Defense Points）
- **CharacterBuffManager**：管理角色級別的 Buff
- **Identity**：唯一 Guid
- **IsDead**：HP ≤ 0 判定

### HealthManager — 雙層防禦設計

生命系統採用**護甲優先吸收**的設計：

| 傷害類型 | 行為 |
|----------|------|
| Normal / Additional | 先扣護甲，剩餘扣血 |
| Penetrate / Effective | 無視護甲，直接扣血 |

- `TakeDamage()` → 回傳 `TakeDamageResult`（實際扣血、護甲吸收、溢出值）
- `GetHeal()` → 回復生命值（不超過上限）
- `GetShield()` → 增加護甲值

所有操作都回傳**結果物件**（含 Delta 值），供事件系統產生精確的數值動畫。

## CardEntity — 卡牌實體

代表戰鬥中的一張卡牌，是 Data → Instance → Entity 三層轉換的終點。

### 核心設計

- **身份**：唯一 Guid（`Identity`），跨區域追蹤
- **資料委派**：效果/選取規則等靜態資訊委派給 CardData（透過 Library 查詢），不在 Entity 中複製
- **變異支援**：`_mutationCardDataIds` 列表支援卡牌在戰鬥中「變身」為其他卡牌
- **Buff 管理**：`CardBuffManager` 管理施加在這張卡上的 Buff
- **屬性集合**：`CardPropertyEntity[]` 定義卡牌行為（Preserved、Consumable、Sealed 等）

### 建構方式
- `CreateFromInstance(CardInstance)`：從 Instance 層建構（正常流程）
- `RuntimeCreateFromId(string)`：從 CardData ID 直接建構（動態創建卡牌）
- `Clone()`：深度複製，可選擇性包含屬性和 Buff

### CardPropertyEntity — 卡牌屬性

每種屬性是一個獨立類別，實作 `ICardPropertyEntity`：

| 屬性 | 效果 |
|------|------|
| `PreservedPropertyEntity` | 回合結束時保留在手牌 |
| `ConsumablePropertyEntity` | 可重複使用 |
| `DisposePropertyEntity` | 打出後移至消耗區 |
| `AutoDisposePropertyEntity` | 自動消耗 |
| `SealedPropertyEntity` | 被封印，無法打出 |
| `RecyclePropertyEntity` | 進入墓地後可回收到手牌 |
| `InitialPriorityPropertyEntity` | 初始抽牌優先 |

## 卡牌區域系統（Zone Pattern）

卡牌在戰鬥中存在於不同的「區域」，區域轉換是卡牌流動的核心機制。

```
DeckEntity（牌組）
  ↓ 抽牌
HandCardEntity（手牌）
  ↓ 打出 / 丟棄
GraveyardEntity（墓地） ←→ 回收 Recycle 屬性的卡牌
  ↓ 排除
ExclusionZoneEntity（排除區）— 本場戰鬥移除

DisposeZoneEntity（消耗區）— 永久移除
```

### CardCollectionZone — 基礎類別
所有區域繼承同一基礎，提供統一的新增/移除/查詢介面。

### 特殊行為
- **DeckEntity**：`PopCardOrNone()` 從頂部抽牌；`EnqueueCardsThenShuffle()` 洗牌（Fisher-Yates）
- **HandCardEntity**：`ClearHand()` 回合結束清理，Preserved 卡牌保留，其餘進入墓地
- **GraveyardEntity**：`PopRecycleCards()` 過濾出 Recycle 屬性的卡牌回收

## Buff 實體系統（三套平行結構）

CardBuff、CharacterBuff、PlayerBuff 三套 Buff 實體結構高度對稱：

### 共通結構

```
BuffEntity
├── Identity (Guid)        # 唯一身份
├── BuffDataId             # 對應 Data 模板
├── Level                  # 疊加層數（可增減）
├── Caster (Option)        # 施放者（可選）
├── LifeTimeEntity         # 生命週期策略
├── PropertyEntity[]       # 屬性修正列表
└── ReactionSessionEntity{} # 反應會話集合
```

### 生命週期策略（LifeTimeEntity）

| 策略 | 行為 |
|------|------|
| AlwaysLifeTime | 永不過期 |
| TurnLifeTime | 每回合結束計數 -1，歸零則過期 |
| HandCardLifeTime | 卡牌離開手牌時過期（僅 CardBuff） |

### BuffManager — 管理器

每個 Manager 提供統一的操作介面：
- `AddBuff()`：新增 Buff（禁止重複 ID）
- `RemoveBuff()`：移除 Buff
- `ModifyBuffLevel()`：修改層數
- `Update()`：更新所有 Buff 的生命週期與 Session，移除已過期的 Buff

## EnergyManager — 能量管理

管理玩家的行動能量池。

| 操作 | 類型 | 語義 |
|------|------|------|
| RecoverEnergy | RoundStartRecover | 回合開始恢復 |
| ConsumeEnergy | PlayCardConsume | 打牌消耗 |
| GainEnergy | GainEffect | 效果獲得 |
| LoseEnergy | LoseEffect | 效果失去 |

所有操作回傳帶 Delta 值的結果物件。

## DispositionManager — 好感度管理

友軍獨有的數值系統，代表與盟友的關係好壞：
- `IncreaseDisposition()` / `DecreaseDisposition()`
- 數值範圍 0 ~ MaxDisposition
- 好感度影響回合能量恢復與抽牌數量

## PlayerCardManager — 卡牌總管理

協調所有卡牌區域的操作：
- `TryPlayCard()`：從手牌取出卡牌進入打出狀態（回傳 IDisposable，結束時自動處理）
- `ClearHandOnTurnEnd()`：回合結束清理手牌
- `RecycleCardOnPlayEnd()`：回收墓地中有 Recycle 屬性的卡牌
- `MoveCard()`：區域間轉移
- `CreateNewCard()`：動態建立新卡牌
- `GetCardOrNone()`：搜尋所有區域（鏈式 Option.Else）

## Instance 層

詳見：[Instance 實例層](Instance.md)

- **CardInstance**：Record 類型，橋接 CardData 與 CardEntity
- **AllyInstance**：Record 類型，持久化玩家狀態

## 設計模式總結

| 模式 | 應用 |
|------|------|
| **Composition** | PlayerEntity 由多個 Manager 組合 |
| **Zone Pattern** | 卡牌在不同區域間流動 |
| **Strategy** | 生命週期策略、屬性行為 |
| **Null Object** | DummyCard、DummyCharacter、DummyPlayer |
| **Factory** | Data.CreateEntity()、CreateFromInstance() |
| **Result Object** | 所有狀態變更回傳 Delta 結果 |
| **Option Pattern** | 安全的實體查詢與可選引用 |
| **RAII** | TryPlayCard 回傳 IDisposable 管理卡牌生命週期 |
