# Target 目標系統

> 最後更新：2026-09-06 | 版本：v3.0

## 設計目的

Target 系統以可序列化的資料描述「在目前 `TriggerContext` 中要讀取哪一個實體或集合」。Effect 與 Condition 只依賴 Target 契約，不依賴特定卡牌內容，因此同一套積木可重用於卡牌、Player Buff、Character Buff 與 Card Buff。

單一 Target 的 `Eval` 回傳 `Option<T>`；找不到對象時回傳 `None`。集合 Target 則回傳空集合，不以 `null` 或 Dummy Entity 表示缺值。此差異讓數值 Value、Condition 與 Effect 能安全傳遞「無可用目標」語意。

## 卡牌 Target 與集合

```
ITargetCardValue
├── NoneCard
├── SelectedCard                 # GameContext 的選取卡牌
├── TriggeredCard                # 目前反應觸發的卡牌
├── ActionCard                   # Card Play Action／Result 的來源卡牌
├── PlayingCardOfPlayer(Player)  # 指定玩家當下暫態正在打出的卡牌
└── IndexOfCardCollection(Collection, Index, Order)

ITargetCardCollectionValue
├── SingleCardCollection(TargetCard)
├── CardsOfPlayer(Player, Zone)
└── FilteredCardCollection(Collection, ICardValueCondition[])
```

`CardsOfPlayer` 支援 `Deck`、`HandCard`、`Graveyard`、`ExclusionZone` 與 `DisposeZone`；它保留來源區域既有順序。`PlayingCard` 不屬於一般 Card Zone，必須使用 `PlayingCardOfPlayer` 取得，不能混入手牌或其他集合查詢。

`FilteredCardCollection` 對每張卡以 AND 套用所有 `ICardValueCondition`，保留符合項目的原始順序。`IndexOfCardCollection` 只接受 `Ascending` 與 `Descending`；`Random`、`None` 或未定義 Order 都是無效資料，Runtime 回傳 `None`，Validator 會在進入遊戲前攔截。純查詢不會推進亂數狀態。

## Player 與 Character Target

```
ITargetPlayerValue
├── NonePlayer
├── CurrentPlayer
├── PlayerByFaction(Ally／Enemy)
├── OppositePlayer(Reference)
├── CardOwner(Card)
├── CharacterOwner(Character)
├── PlayerBuffContentPlayer(Buff, Owner／Caster)
├── SelectedPlayer
└── TriggeredPlayer

ITargetCharacterValue
├── NoneCharacter
├── MainCharacterOfPlayer(Player)
├── SelectedCharacter
└── TriggeredCharacter

ITargetCharacterCollectionValue
├── NoneCharacters
├── SingleCharacterCollection(Target)
└── CharactersOfPlayer(Player)
```

`PlayerByFaction` 用於沒有 Current Player 的全域 Timing；`Faction.None` 或未定義值無法解析。`CharactersOfPlayer` 回傳主角色與所有助戰角色，維持 Runtime 順序且不自動略過死亡角色。

## Buff Target 與集合

Player、Character、Card 三種 Buff 都有相同的資料形狀：

```
單一 Buff：None、Triggered、ById(Collection, BuffId)
集合來源：None、BuffsOfOwner
```

`PlayerBuffsOfPlayer`、`CharacterBuffsOfCharacter` 與 `CardBuffsOfCard` 分別從其 Owner 的 BuffManager 取得集合。`ById` 找不到時回傳 `None`；集合 Target 缺失時回傳空集合。Card Buff 集合只讀取當下有效 Layer，外部 Override 生效時不會同時讀取被覆蓋的 Base Layer。

## Selected、Triggered 與 Action 的邊界

- `Selected*` 從 `GameContext` 取得玩家或 AI 明確選擇的對象。
- `Triggered*` 從 `TriggerContext.Triggered` 取得正在反應的來源。
- `ActionCard` 是 Card Play Action／Result 所記錄的來源卡牌。
- Action 實際作用目標由 Action 的 Target 資料表達，不能以 Selected 或 Triggered 取代。

## 選取規則

`IMainTargetSelectable` 是出牌時 UI 可選主目標的宣告，與上述 Runtime Target 解析分離。它提供 `NoneSelectable`、Character／CharacterAlly／CharacterEnemy，以及 Card／CardAlly／CardEnemy 等選取範圍。`ISubSelectionGroup` 目前由 `ExistCardSelectionGroup` 描述既有卡牌的子選取規則。

View 依 Selectable 決定可互動對象；Effect 與 Condition 則在 Runtime 以 Target 解析實際實體，兩者不可混用。

## 與其他系統的關係

- [Value.md](Value.md) 以 Target 讀取可選數值並保留缺值語意。
- [Condition.md](Condition.md) 以 Target 對實體、集合與 Timing 建立組合判斷。
- Effect Resolver 以集合 Target 解析命令對象；單一 Target 或 Value 缺失時採安全 No-op。
- `GameDataValidator` 驗證必填 Target、列舉值、Reference ID 與集合查詢語意，避免錯誤資產進入 Runtime。
