# Card 卡牌系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

卡牌是 MortalGame 最核心的遊戲元素。卡牌系統完整體現了三層資料架構的設計哲學：**CardData（設計模板）→ CardInstance（持久快照）→ CardEntity（戰鬥實體）**。這三層分離使得同一張設計卡牌可以在不同遊戲場景中產生不同的實例，而每個實例在戰鬥中又可以擁有獨立的狀態。

## 三層流轉

### CardData（設計時）
設計師在 Unity 編輯器中配置的卡牌模板，定義卡牌的「先天特質」：
- **分類**：類型（Attack/Defense/Speech/Sneak/Special/Item）、稀有度、門派主題
- **基礎數值**：費用（0-10）、威力（0-20）
- **效果定義**：直接效果列表 + 觸發效果列表
- **目標規則**：主目標選取 + 子目標選取群組
- **屬性工廠**：PropertyData 列表，透過 CreateEntity() 產生運行時屬性

### CardInstance（存檔時）
Record 類型的不可變快照，代表牌組中的一張具體卡牌：
- **唯一身份**：InstanceGuid（跨場景追蹤）
- **資料引用**：CardDataId（指向哪張設計卡牌）
- **附加屬性**：AdditionPropertyDatas（超出 CardData 定義的額外屬性）

### CardEntity（戰鬥中）
完整的戰鬥運行時物件：
- **唯一身份**：Identity（戰鬥內 Guid）
- **動態資料**：支援卡牌「變身」（`_mutationCardDataIds`）
- **Buff 管理**：CardBuffManager 管理施加在卡上的 Buff
- **屬性實體**：運行時的 CardPropertyEntity 集合

## 卡牌效果體系

### 直接效果（Effects）

打出卡牌時立即執行的效果列表，按目標類型分類：

**角色目標效果**
- DamageEffect（造成傷害）、PenetrateDamageEffect（穿甲傷害）
- AdditionalAttackEffect（追加傷害）、EffectiveAttackEffect（確實傷害）
- ShieldEffect（給予護甲）、HealEffect（治療）

**玩家目標效果**
- GainEnergyEffect / LoseEnergyEffect（能量增減）
- AddPlayerBuffEffect / RemovePlayerBuffEffect（Buff 操作）
- IncreaseDispositionEffect / DecreaseDispositionEffect（好感度增減）

**卡牌目標效果**
- DrawCardEffect / DiscardCardEffect / ConsumeCardEffect / DisposeCardEffect
- CreateCardEffect（創建新卡牌，可附帶 Buff）
- CloneCardEffect（複製卡牌）
- AddCardBuffEffect / RemoveCardBuffEffect

### 觸發效果（TriggeredEffects）

基於 `CardTriggeredTiming` 的事件驅動效果：
- 抽到時（Drawed）、打出時（Played）、保留時（Preserved）、丟棄時（Discarded）
- 每個觸發效果包裝為 `TriggeredCardEffect`，內含時機 + ICardEffect 列表

### 效果參數化

所有效果的數值和目標都使用抽象介面：
- **數值**：`IIntegerValue`（可以是常數、運算式、從實體屬性讀取）
- **目標**：`ITargetCollectionValue`（動態解析目標實體列表）

這種參數化設計使得效果定義可以高度複用。

## 卡牌屬性系統

屬性代表卡牌的**行為標記**，影響卡牌在不同區域轉換時的行為：

| 屬性 | 效果 | 來源 |
|------|------|------|
| Preserved | 回合結束保留在手牌 | CardPropertyData |
| Consumable | 可重複打出 | CardPropertyData |
| Dispose | 打出後進入消耗區 | CardPropertyData |
| AutoDispose | 自動進入消耗區 | CardPropertyData |
| Sealed | 被封印，無法打出 | CardBuffPropertyData |
| Recycle | 墓地中可被回收 | CardPropertyData |
| InitialPriority | 初始抽牌優先 | CardPropertyData |

### Data→Entity 工廠模式

每個 `ICardPropertyData` 都有 `CreateEntity()` 方法，產生對應的 `ICardPropertyEntity`。這是 Data 層到 Entity 層的橋樑。

## 卡牌目標選取

### 主目標（MainSelect）
定義玩家打出卡牌時需要選取的目標類型：
- None：不需要選取（自動效果）
- Character（All/Ally/Enemy）：選取角色
- Card（All/Ally/Enemy）：選取卡牌

附帶 `TargetLogicTag` 供 AI 使用（ToEnemy/ToAlly/ToRandom）。

### 子目標（SubSelects）
支援多步驟複雜選取：
1. 從現有卡牌選取（ExistCardSelectionGroup）
2. 從新建卡牌選取（NewCardSelectionGroup）
3. 從效果變體選取（NewEffectSelectionGroup）

## 卡牌區域流動

```
DeckEntity（牌組）──抽牌──→ HandCardEntity（手牌）
    ↑                           │
    │ 回收(Recycle)          打出 │ 丟棄
    │                           ↓
    ← ─ ─ ─ ─ ─ ─ ─ ─ ─  GraveyardEntity（墓地）
                                │
                          排除   │ 消耗(Dispose)
                                ↓
ExclusionZoneEntity ←─── DisposeZoneEntity
```

## 卡牌 Buff 系統

詳見：[CardBuff 卡牌 Buff 系統](CardBuff.md)

CardBuff 是施加在單張卡牌上的修正器，可以改變卡牌的威力、費用、或添加/移除屬性。

## 與門派主題的關係

每張卡牌可以屬於一或多個門派主題（CardTheme）：
- **唐門**（TangSect）
- **峨嵋**（Emei）
- **嵩山**（Songshan）
- **丐幫**（BeggarClan）
- **滇蒼**（DianCang）

門派主題影響卡牌的風格分類，未來可能擴展為門派親和度或門派專屬機制。

## 相關文件

- [CardBuff 卡牌 Buff](CardBuff.md) — 卡牌級別的 Buff 系統
- [Effect 效果管線](Effect.md) — 卡牌效果的執行流程
- [Target 目標系統](Target.md) — 目標選取規則
- [Entity 實體系統](Entity.md) — CardEntity 的詳細結構
- [Instance 實例層](Instance.md) — CardInstance 的設計
