# Condition 條件系統

> 最後更新：2026-09-06 | 版本：v3.0

## 設計目的

`ICondition` 在 `TriggerContext` 下回傳布林結果，供 Conditional Effect、反應規則與 Session 更新規則共用。條件只讀取當下狀態；不建立內容專用的「定時炸彈條件」或「刀盾條件」，而是以 Target、Value 與條件積木組合表達。

當單一 Target 或 `IIntegerValue` 無法取得時，依賴該值的 Condition 一律回傳 `false`。這讓 Runtime 的正常時序失效安全結束，同時由 Validator 攔截可在編輯期判定的錯誤資料。

## 邏輯與 Timing 積木

```
ICondition
├── ConstCondition
├── AllCondition
├── AnyCondition
├── InverseCondition
├── GameTimingCondition
└── IsTriggeredOwnerTurnCondition
```

`AllCondition`、`AnyCondition` 與 `InverseCondition` 可遞迴組合。空白條件清單或缺少必要子條件不屬於有效企劃資料，會由 Validator 攔截。`GameTimingCondition` 比較的是整條反應鏈的 `ReactionOriginTiming`；非 Timing 反應鏈及 `GameTiming.None` 皆為 `false`。

## 數值與實體條件

```
IntegerCondition(Value, IIntegerValueCondition[])
├── IntegerCompare（Equal、NotEqual、Greater、Less、GreaterOrEqual、LessOrEqual）

CardCondition(Card, ICardValueCondition[])
├── CardIdentityCondition
├── BaseCardDataIdCondition
├── CardFormCondition
├── CardTypesCondition／CardThemesCondition／CardRaritiesCondition
└── CardPropertiesCondition

PlayerCondition(Player, IPlayerValueCondition[])
├── PlayerFactionCondition
├── PlayerEnergyCondition
└── PlayerIsDeadCondition

CharacterCondition(Character, ICharacterValueCondition[])
├── CharacterFactionCondition
└── CharacterIsDeadCondition

CardPlayCondition／CardPlayResultCondition
└── 分別讀取 Card Play Source 與 Effect Result

CardFormOverrideSessionCondition
└── 讀取目前 External Override 所持有的 Reaction Session
```

Card Identity 比較戰鬥實體 Identity；Base CardData 比較不受變形影響的原始資料；Card Form 與 Type／Theme／Rarity／Property 則比較目前有效形態。玩家死亡只由 Main Character 的死亡狀態判定，助戰角色死亡不會單獨造成 Player 死亡。

## 集合與 Buff 條件

```
CardCollectionContainsCondition(Collection, Card)
CardCollectionAnyCondition(Collection, ICardValueCondition[])
CardCollectionAllCondition(Collection, ICardValueCondition[])

PlayerBuffCondition／CharacterBuffCondition／CardBuffCondition
└── 各自接受 ID 等 Buff Value Condition

PlayerBuffCollectionContainsIdCondition
CharacterBuffCollectionContainsIdCondition
CardBuffCollectionContainsIdCondition
```

`CardCollectionContainsCondition` 以 Card Identity 判定同一張戰鬥實體卡。Any 與 All 都要求集合至少有一張卡；空集合一律為 `false`，避免把「沒有任何卡」誤解成「所有卡都符合」。同一張卡的多個 `ICardValueCondition` 固定以 AND 評估。

例如「持有者回合結束時，卡片仍在持有者手牌」可由 `GameTimingCondition`、`IsTriggeredOwnerTurnCondition`、`CardsOfPlayer(CardOwner(ActionCard), HandCard)` 與 `CardCollectionContainsCondition` 組合；不需額外專用 Condition 類別。

## Action 結果與 Session 條件

`CardPlayCondition` 可對出牌位置與來源卡牌建立條件；`CardPlayResultCondition` 可對 Effect Result 類型、Damage 結果與結果目標建立條件。`CardFormOverrideSessionCondition` 透過 Session Key 讀取目前 External Override 的 Reaction Session，並以「值是否已更新」、布林或整數比較進行判斷。這些條件同樣只讀取 Context，不改變遊戲狀態。

## Context 與驗證邊界

`TriggerContext` 提供 Model、Triggered、Action 與反應鏈起因 Timing。條件可讀取這些資料，但不得變更狀態或消耗亂數。

`GameDataValidator` 會檢查必填巢狀引用、空集合條件、無效比較列舉、無效 Timing、無效卡片集合區域、Buff ID 與 Reference ID。Runtime 則負責把正常的缺值與時序失效表達為 `false` 或安全 No-op。

## 與其他系統的關係

- [Target.md](Target.md) 定義條件讀取對象的來源與缺值契約。
- [Value.md](Value.md) 定義整數資料來源、算術與 `Option<int>` 的傳遞規則。
- Conditional Buff Effect 可直接接受 `ICondition`；具體查詢對象由 Target 與 Condition 類別決定。
