# Effect 效果管線

> 最後更新：2026-09-10 | 版本：v2.1

## 設計理念

Effect 系統是 GameModel 中最複雜也最核心的子系統，負責將「卡牌效果定義」轉換為「實際的遊戲狀態變更」。整個設計遵循**宣告式到命令式**的轉換思路：

1. **宣告式輸入**：CardEffect（「對目標造成 5 點傷害」）
2. **解析轉換**：EffectDataResolver（解析目標、建立 Action 鏈）
3. **命令式執行**：EffectCommandExecutor（執行命令、產生事件）

## 效果管線流程

```
卡牌打出
  ↓
EffectDataResolver.Resolve()
  ├── 解析目標列表（ITargetCollectionValue）
  ├── 對每個目標建立三階 Action：
  │   ├── Intent Action（意圖宣告）
  │   ├── Target Intent Action（目標綁定）
  │   └── Effect Command（待執行命令）
  └── 回傳 EffectCommandSet
  ↓
EffectCommandExecutor.Execute()
  ├── 逐一執行 Effect Command
  ├── 對每個命令：
  │   ├── 呼叫實體方法修改狀態
  │   ├── 產生 Result Action
  │   ├── 觸發 Buff 反應（UpdateReactorSessionAction）
  │   └── 產生 GameEvent 供 View 使用
  └── 回傳 EffectResult（所有 Action + 事件）
```

## EffectCommand — 命令封裝

每種遊戲操作都有對應的 EffectCommand，封裝「要改什麼」的具體指令：

### 角色生命相關
- `DamageEffectCommand` — 造成傷害（含傷害類型）
- `HealEffectCommand` — 治療
- `ShieldEffectCommand` — 獲得護甲

### 能量相關
- `GainEnergyEffectCommand` — 獲得能量
- `LoseEnergyEffectCommand` — 失去能量

### 好感度相關
- `IncreaseDispositionEffectCommand` — 增加好感度
- `DecreaseDispositionEffectCommand` — 減少好感度

### Buff 操作
- `AddPlayerBuffEffectCommand` — 添加玩家 Buff
- `RemovePlayerBuffEffectCommand` — 移除玩家 Buff
- `ModifyPlayerBuffLevelEffectCommand` — 修改 Buff 層數

### 卡牌操作
- `DrawCardEffectCommand` — 抽牌
- `MoveCardEffectCommand` — 移動卡牌到其他區域
- `CreateCardEffectCommand` — 創建新卡牌
- `CloneCardEffectCommand` — 複製卡牌

### 卡牌 Buff
- `AddCardBuffEffectCommand` — 添加卡牌 Buff
- `RemoveCardBuffEffectCommand` — 移除卡牌 Buff

### 屬性修正
- `ModifyCardAttributeEffectCommand` — 修改卡牌打出的屬性加成

## EffectDataResolver — 效果解析器

負責將宣告式的 `ICardEffect` 轉換為可執行的 `EffectCommand` 集合。

### 解析過程

1. **讀取效果定義**：從 CardData 或 BuffData 取得效果物件
2. **評估目標**：透過 ITargetCollectionValue 解析出具體的實體列表
3. **建立 Intent**：為每個目標建立 IntentAction
4. **生成命令**：為每個目標生成對應的 EffectCommand
5. **組裝 CommandSet**：將所有命令打包回傳

### Card 與 Reaction 的共用解析契約

`EffectDataResolver` 支援四種效果來源：直接出牌的 `ICardEffect`，以及
`IPlayerBuffEffect`、`ICharacterBuffEffect`、`ICardBuffEffect` 三種 Reaction Effect。
四個 Registry 都以「效果具體型別 → 對應 Resolver」直接查表；同一個核心操作會共用
同一個 Resolver 與 Command 路徑，但 Reaction Registry **不會回退查詢** Card Registry。

因此，新增一個可由 Reaction 使用的效果時，必須同時完成下列事項：

1. 讓效果型別明確實作允許的來源介面。
2. 將相同 Resolver 實例登錄到每個允許來源的 Registry。
3. 補齊 `GameDataValidator`、Round Trip 與來源行為測試。

這項限制讓「可否使用」成為可檢查的內容規則，而不是執行期猜測。未知型別在 Runtime
會記錄警告並回傳空 `EffectCommandSet`，不會執行半套效果；正式資產則必須先由
`GameDataValidator` 阻止。

### 四來源允許操作矩陣

下表的「共用」表示四種來源使用同一個 Resolver／Command 實作，而非複製四套流程。

| 操作 | Card | PlayerBuff | CharacterBuff | CardBuff |
|------|:----:|:----------:|:-------------:|:--------:|
| Damage、Shield、Heal | ✓ | ✓ | ✓ | ✓ |
| GainEnergy、LoseEnegy、Increase／DecreaseDisposition | ✓ | ✓ | ✓ | ✓ |
| DrawCard | ✓ | ✓ | ✓ | ✓ |
| DiscardCard、ConsumeCard、DisposeCard | ✓ | ✓ | ✓ | ✓ |
| CreateCard、CloneCard | ✓ | ✓ | ✓ | ✓ |
| Add／ModifyLevel／Remove PlayerBuff | ✓ | ✓ | ✓ | ✓ |
| Add／Remove CardBuff | ✓ | ✓ | ✓ | ✓ |
| ModifyCardPlayAttribute | — | ✓ | ✓ | ✓ |
| ApplyCardFormOverride | ✓ | — | — | — |

`ModifyCardPlayAttributeEffect` 是既有的 Reaction 專用修正語意，不屬於 Card 的直接效果。
`ApplyCardFormOverrideEffect` 涉及卡片形態與生命週期，維持 Card 專用；將卡片生命週期
觸發接入 Queue 是後續 T-017 的工作，T-020 沒有藉此開放 `CardTriggeredTiming`。

舊的 `AddCardBuffPlayerBuffEffect` 與 `RemoveCardBuffPlayerBuffEffect` 已在正式資產遷移後
完全刪除；PlayerBuff 要新增或移除 CardBuff 時，一律使用共用的
`AddCardBuffEffect`／`RemoveCardBuffEffect`，不保留舊型別相容分支。

### Reaction Context 與 Queue 邊界

- **Owner**：PlayerBuff 為被觸發玩家、CharacterBuff 為角色所屬玩家、CardBuff 與卡牌
  Trigger 為觸發卡片的實際持有玩家。
- **Caster**：Buff 使用建立時記錄的 Caster；直接卡牌效果使用卡片持有玩家。無合法來源時
  回傳空值，不會猜測 Current Player。
- **Selected Card**：仍由 `GameContext` 持有；Reaction Planner 不覆寫既有選取。
- **Playing Card**：只從請求玩家的暫態 Playing Card 取得，不與一般牌區混用。
- **順序與快照**：同一個 Timing 依 PlayerBuff → CharacterBuff → CardBuff 建立反應項目；
  每一來源在建立時快照，後續新增／移除的 Buff 不會回頭加入同一次派送。
- **失效契約**：目標、來源區域或 Layer 在執行前已失效時，命令安全 No-op，且不產生
  Result 或 Event。

`ReactionOriginTiming` 會隨反應鏈傳遞，因此條件和值可辨識最初觸發的 `GameTiming`；它不會
因後續 Queue 執行而遺失。

### 數值解析

效果中的數值並非簡單常數，而是透過 `IIntegerValue` 介面動態評估。這允許：
- `ConstInteger`：固定值
- `ArithmeticInteger`：運算式（加減乘除）
- `CardIntegerProperty`：從卡牌屬性讀取
- `PlayerIntegerProperty`：從玩家屬性讀取
- `ConditionalValue`：根據條件返回不同值

## EffectCommandExecutor — 命令執行器

負責實際執行命令並處理所有副作用。

### 執行流程（以 DamageCommand 為例）

```
1. 讀取 DamageEffectCommand（目標角色、傷害值、傷害類型）
2. 透過 GameFormula 計算最終傷害值（套用 Buff 修正）
3. 呼叫 CharacterEntity.HealthManager.TakeDamage()
4. 取得 TakeDamageResult（實際扣血、護甲吸收、溢出值）
5. 建立 DamageResultAction
6. 觸發 UpdateReactorSessionAction（讓 Buff 對傷害結果做反應）
7. 產生 DamageEvent 供 View 播放動畫
8. 檢查角色是否死亡 → 產生死亡事件
```

### Buff 反應整合

每個命令執行後，都會觸發 `UpdateReactorSessionAction`，讓所有監聽該動作類型的 Buff 有機會做出反應。這是實現「受到傷害時觸發效果」等被動機制的核心。

## EffectEventResult — 結果聚合

將一次效果執行的所有產出打包：
- **Actions**：所有產生的 Action（Intent、TargetIntent、Result）
- **Events**：所有產生的 GameEvent

## 與其他系統的關係

```
CardEffect (GameData)
    ↓ 定義「做什麼」
EffectDataResolver
    ↓ 解析目標、建立命令
EffectCommand
    ↓ 封裝「怎麼做」
EffectCommandExecutor
    ├── 呼叫 Entity 方法修改狀態
    ├── 透過 GameFormula 計算數值
    ├── 產生 Result Action → Buff 反應系統
    └── 產生 GameEvent → View 層
```

## 設計價值

1. **可擴展性**：已批准的核心操作可由四種來源共用同一個 Resolver、Command 與 Executor 路徑
2. **可追溯性**：每個效果的完整執行鏈（Intent → Target → Result）都被記錄
3. **Buff 友善**：三階 Action 管線讓 Buff 有充分的介入時機
4. **數值透明**：所有計算透過 GameFormula 集中處理，易於調試
