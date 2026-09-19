# Card 卡牌系統

> 核對日期：2026-09-19

卡牌分為設計配置 CardData、跨戰鬥 CardInstance、本場戰鬥 CardEntity。移牌與變形保留 Entity Identity；Runtime 建立及 Clone 的卡片沒有來源 Instance。形態規則見 [CardTransformation](CardTransformation.md)，資料轉換見 [CardEntity](../Assets/Scripts/GameModel/Entity/Card/CardEntity.cs)。

## 出牌與移牌

主動出牌須在手牌、未被 Sealed，且費用可負擔。主要順序為：進入 PlayingCard → 普通 Effects → UsedCardEvent → Played → CardPlayResultAction → 離場。普通 Effects 依 EffectRepeat 執行至少一次；Played 每次出牌只執行一次，其結果併入同次出牌結果。

不同操作的目的地依各自規則決定，不可只由屬性名稱推測：

| 操作 | 目的地與優先順序 |
|---|---|
| 出牌離場 | Dispose 或 AutoDispose → ExclusionZone；其餘 → Graveyard |
| 回合清手 | Preserved 留手；其餘 AutoDispose → ExclusionZone；其他 → Graveyard |
| DiscardCardEffect | Dispose → DisposeZone；否則 Consumable → ExclusionZone；其他 → Graveyard |
| Recycle | 出牌離場後，將剛打出且仍在墓地的該卡移回手牌 |

Consumable 不等於可重複打出。Sealed 可來自卡片屬性或 CardBuff。InitialPriority 已有資料、Entity 與查詢，但尚未影響初始抽牌排序（T-021）。

規則來源：[GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs)、[PlayerCardManager](../Assets/Scripts/GameModel/Entity/Player/PlayerCardManager.cs)、[DiscardCardEffectResolver](../Assets/Scripts/GameModel/Effect/Resolvers/DiscardCardEffectResolver.cs)。

## 卡片生命週期

| 時機 | 正式 Runtime 入口 |
|---|---|
| Initialize | 戰鬥開始記錄的初始卡片；BeforeGameStart 新增的卡不加入原名單 |
| Drawed／EffectDrawed | 實際 Deck → HandCard 後，按抽牌鏈最初根源區分系統／效果抽牌 |
| Played | 玩家及敵人主動出牌，執行時仍在 PlayingCard |
| Preserved／Discarded | 回合清手後，先全部保留卡，再全部棄牌卡 |
| EffectDiscarded | DiscardCardEffect 成功移牌後；明確 Consume／Dispose 不派送 |
| FormChanged | Self 變形／Override 解除有 CardData 入口；Override 套用及 CardBuff 尚未接線，見 [形態文件](CardTransformation.md) |
| EffectPlayed | 尚無正式間接出牌入口，見 T-022 |

共用 [CardTriggeredEffectDispatch](../Assets/Scripts/GameModel/Effect/CardTriggeredEffectDispatch.cs) 先快照符合條件的 CardData 效果，再快照當下有效 CardBuff 效果，固定依此順序排程。後續形態或 Buff 變動不回頭修改已建名單。

抽多張或效果棄多張時，逐張完成狀態、事件及觸發，再處理下一張。回合清手則先提交該玩家整批移牌與事件，再以固定順序執行生命週期；Ally 完成後才輪到 Enemy。Preserved 優先於 AutoDispose，不同時觸發 Discarded。Queue 預算邊界見 [Effect](Effect.md)。

## 效果與選取

普通 Effects 與 Conditional 生命週期效果共用 [Effect 管線](Effect.md)。傷害種類由 DamageEffect 的資料區分，不是各自獨立的穿甲／追加效果型別。

主目標宣告可互動對象；子選取已有依群組逐次處理 ExistCard 的基礎。NewCard／NewPartialCard／NewEffect 仍是預留結構，多步驟取消、不足及 Model 消費契約尚未完整，見 T-011。選取宣告見 [TargetSelectable](../Assets/Scripts/GameModel/Target/TargetSelectable.cs)，UI 處理見 [SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs)。

屬性 Entity 由 [CardPropertyEntityFactory](../Assets/Scripts/GameModel/Factory/CardPropertyEntityFactory.cs) 建立；卡牌修正器見 [CardBuff](CardBuff.md)。
