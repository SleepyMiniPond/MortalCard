# CardBuff 卡牌 Buff 系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

CardBuff 是施加在**單張卡牌**上的動態修正器。與 PlayerBuff（作用於整個玩家）和 CharacterBuff（作用於角色）不同，CardBuff 精確到特定一張卡牌，適合實現「封印某張牌」、「這張牌威力 +3」等個體化效果。

三套 Buff 系統（Card/Character/Player）共享相同的設計範式，但各自針對不同的作用對象做特化。

## 結構設計

### CardBuffData（設計時模板）

```
CardBuffData
├── ID                    # 唯一識別碼
├── Sessions{}            # 反應會話（動態狀態追蹤）
├── Effects{}             # CardTriggeredTiming → ConditionalCardBuffEffect[]
├── PropertyDatas[]       # 屬性修正工廠列表
└── LifeTimeData          # 生命週期策略工廠
```

### CardBuffEntity（戰鬥時實體）

```
CardBuffEntity
├── Identity (Guid)       # 唯一實例身份
├── CardBuffDataId        # 對應的 Data 模板 ID
├── Level                 # 疊加層數
├── Caster (Option)       # 施放者（可選）
├── LifeTimeEntity        # 運行時生命週期
├── PropertyEntity[]      # 運行時屬性修正
└── ReactionSessionEntity{} # 運行時反應會話
```

## 屬性修正

CardBuff 可以修改卡牌的屬性：

| 屬性 | 效果 |
|------|------|
| `SealedCardBuffPropertyEntity` | 封印卡牌，使其無法被打出 |
| `PowerCardBuffPropertyEntity` | 修改卡牌威力（使用 IIntegerValue 動態計算） |

屬性值的計算是**上下文敏感的**——透過 `Eval(TriggerContext)` 動態求值，結果可能因遊戲狀態不同而改變。

## 生命週期策略

| 策略 | 行為 | 適用場景 |
|------|------|----------|
| `AlwaysLifeTime` | 永不過期 | 永久性卡牌修正 |
| `TurnLifeTime` | N 回合後過期 | 「威力 +2 持續 3 回合」 |
| `HandCardLifeTime` | 卡牌離開手牌時過期 | 「手牌中威力 +1」 |

HandCardLifeTime 是 CardBuff 獨有的策略，反映了卡牌在不同區域時 Buff 效果的語義差異。

## 條件觸發效果

每個 Buff 效果都包裝在 `ConditionalCardBuffEffect` 中：
- **條件列表**：ICardBuffCondition[]（所有條件都必須滿足）
- **效果**：ICardBuffEffect（觸發時執行的效果）

效果按 `CardTriggeredTiming` 分組（與卡牌本身的觸發時機一致）。

## 反應會話（Session）

CardBuff 可以擁有反應會話，追蹤動態狀態：
- 記錄「本回合觸發了幾次」
- 記錄「是否已被激活」
- 根據遊戲事件更新計數器

詳見：[Session 反應會話](Session.md)

## CardBuffManager — 管理器

每張 CardEntity 都擁有一個 CardBuffManager，負責：
- **新增 Buff**：防止重複 ID
- **移除 Buff**：按身份或 Data ID
- **修改層數**：增減 Buff 的 Level
- **更新**：每回合更新生命週期和 Session，移除已過期的 Buff

## 與 PlayerBuff / CharacterBuff 的比較

| 面向 | CardBuff | CharacterBuff | PlayerBuff |
|------|----------|---------------|------------|
| 作用對象 | 單張卡牌 | 單個角色 | 整個玩家 |
| 典型效果 | 封印、威力修正 | 生命上限、能量上限 | 全域傷害加成 |
| 觸發時機 | CardTriggeredTiming | GameTiming | GameTiming |
| 獨有生命週期 | HandCardLifeTime | — | — |
| 屬性修正 | 封印、威力 | 最大生命、最大能量 | 16 種全域數值修正 |

## 相關文件

- [Card 卡牌系統](Card.md) — CardBuff 的宿主
- [Player 玩家系統](Player.md) — PlayerBuff 系統
- [Character 角色系統](Character.md) — CharacterBuff 系統
- [Session 反應會話](Session.md) — Buff 動態狀態追蹤
- [Entity 實體系統](Entity.md) — 實體結構總覽
