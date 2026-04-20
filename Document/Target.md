# Target 目標系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

目標系統負責回答一個關鍵問題：**「效果要作用於誰？」**。它將目標解析抽象為介面層級的組合，使得效果定義可以用宣告式語法指定目標，而具體的目標實體在運行時才被解析。

核心設計：所有目標都是 `ITargetValue` 的實作，在 `TriggerContext` 下動態求值。

## 目標值型別體系

### 卡牌目標

```
ITargetCardValue（單張卡牌）
├── SelectedCard         # 當前選取的卡牌
├── PlayingCard          # 正在打出的卡牌
└── IndexOfCardCollection # 卡牌區域中特定位置的卡牌

ITargetCardCollectionValue（多張卡牌）
└── AllyHandCards        # 指定玩家的手牌集合
```

### 玩家目標

```
ITargetPlayerValue（單個玩家）
├── CurrentPlayer    # 當前行動玩家
├── OppositePlayer   # 對手玩家
├── CardOwner        # 卡牌擁有者
├── CharacterOwner   # 角色擁有者
└── SelectedPlayer   # 當前選取的玩家

ITargetPlayerCollectionValue（多個玩家）
└── SinglePlayerCollection  # 單個玩家包裝為集合
```

### 角色目標

```
ITargetCharacterValue（單個角色）
├── MainCharacterOfPlayer  # 指定玩家的主角色
└── SelectedCharacter       # 當前選取的角色

ITargetCharacterCollectionValue（多個角色）
└── SingleCharacterCollection  # 單個角色包裝為集合
```

### Buff 目標

```
ITargetPlayerBuffValue（玩家 Buff）
└── TriggeredPlayerBuff  # 觸發此效果的玩家 Buff

ITargetCardBuffValue（卡牌 Buff）
└── TriggeredCardBuff    # 觸發此效果的卡牌 Buff
```

## 選取系統（Selectable）

選取系統定義了「玩家在 UI 上可以選什麼」的規則，是卡牌打出時目標互動的核心。

### 主目標選取（IMainTargetSelectable）

```
NoneSelectable              # 不需要選取目標
CharacterSelectable         # 任意角色
CharacterAllySelectable     # 友方角色
CharacterEnemySelectable    # 敵方角色
CardSelectable              # 任意卡牌
CardAllySelectable          # 友方卡牌
CardEnemySelectable         # 敵方卡牌
```

### 子目標選取群組（ISubSelectionGroup）

支援多步驟的複雜選取，例如「選擇 2 張手牌丟棄，然後抽 3 張牌」：

```
ExistCardSelectionGroup   # 從現有卡牌中選取
NewCardSelectionGroup     # 創建新卡牌供選取
NewEffectSelectionGroup   # 選擇效果變體
```

每個群組定義最少/最大選取數量，以及是否為必須選取。

## 數值型別（Integer / Boolean）

目標系統同時提供抽象數值，用於效果計算的參數化：

### IIntegerValue（整數值）

```
ConstInteger          # 固定常數
ArithmeticInteger     # 運算式（左運算元 ○ 右運算元）
CardIntegerProperty   # 從卡牌屬性讀取（費用、威力等）
PlayerIntegerProperty # 從玩家屬性讀取（能量、生命等）
ConditionalValue      # 根據條件返回不同值
```

### IBooleanValue（布林值）

```
TrueValue   # 固定 true
FalseValue  # 固定 false
```

## 設計價值

### 組合性
目標和數值都是可組合的。例如 `ArithmeticInteger` 的左右運算元本身也是 `IIntegerValue`，可以嵌套出任意複雜的運算式。

### 延遲求值
所有目標值都在 `TriggerContext` 下動態求值，這意味著：
- 效果定義時不需要知道具體目標
- 相同的效果定義可以在不同上下文下產生不同結果
- Buff 修正可以影響目標解析結果

### UI 協同
`IMainTargetSelectable` 直接映射到 View 層的互動邏輯——View 根據選取類型決定哪些 UI 元件可以被點擊/拖曳。

## 與其他系統的關係

- **Effect 系統**：EffectDataResolver 使用 ITargetCollectionValue 解析效果目標
- **Condition 系統**：條件使用 ITargetValue 解析需要判斷的實體
- **GameView**：IMainTargetSelectable 驅動 UI 的可選取狀態
- **CardData**：效果定義中嵌入 ITargetCollectionValue 指定目標規則
