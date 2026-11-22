# Card 卡牌系統

> 核對日期：2026-11-22

卡牌分為設計配置 CardData、跨戰鬥 CardInstance、本場戰鬥 CardEntity。移牌與變形保留 Entity Identity；Runtime 建立及 Clone 的卡片沒有來源 Instance。形態規則見 [CardTransformation](CardTransformation.md)，資料轉換見 [CardEntity](../Assets/Scripts/GameModel/Entity/Card/CardEntity.cs)。

## 卡片定義與動態合成

CardEntity 一律持有 [ICardDefinition](../Assets/Scripts/GameModel/Entity/Card/ICardDefinition.cs)。CatalogCardDefinition 引用內容資產；[ComposedCardDefinition](../Assets/Scripts/GameModel/Entity/Card/ComposedCardDefinition.cs) 保存模板與所選選項組合的獨立內容。建立新卡及 Clone 共用 RuntimeCreateFromDefinition，透過 CopyForNewCard 保護合成卡的巢狀可變資料；不同成品不共用可變效果。

合成第一輪選 StandardCardData 模板，決定費用、類型、目標、原有效果及其他基本配置；後續選項只追加 Effects 與 TriggeredEffects。普通效果依選取輪次追加，同時機觸發清單保留原項目、條件與順序，不覆蓋模板效果。

完整選取通過驗證時，先固定本次 ComposedCardDefinition；CreateComposedCardEffect 執行時才建立 Entity，沿既有 CreateCardEffectCommand 與 AddCardEvent 加入指定牌區。選片段不執行成品效果、不消耗材料牌；取消或零候選拒絕整次操作，不支付出牌費用。成品使用時重新依模板選目標，不保存合成當時的 Selected Context。

成品目前只保存在本場戰鬥，沒有來源 Instance，也不登錄為新的 Catalog 資產或寫回跨戰鬥牌組。Self／Override 期間使用有效形態的 Definition，還原後恢復原組成。

動態敘述保留模板文字，依選取輪次追加選項本地化片段；CardInfo 保存數值／規則參數，CardView 與 AiCardView 於渲染時取得文字。常數運算讀取實際效果 Value；依未來目標或狀態變動的值顯示配置的規則文字，不藉由執行效果計算敘述。時機及條件由片段作者寫明。配置與驗證見 [GameData](GameData.md#子選取配置)。

## 出牌與移牌

主動出牌須在手牌、未被 Sealed，且費用可負擔。間接出牌由 `PlayCardEffect` 排入請求，輪到請求時須仍在擁有者手牌且未被 Sealed；不支付能量，也不檢查費用。兩者共用單張流程：進入 PlayingCard → 普通 Effects → UsedCardEvent → Played 或 EffectPlayed → CardPlayResultAction → 離場。普通 Effects 依 EffectRepeat 執行至少一次；Played／EffectPlayed 依出牌原因二擇一，每次出牌只執行一次，其結果併入同次出牌結果。間接出牌失效時不產生成功事件或生命週期效果。

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
| EffectPlayed | `PlayCardEffect` 間接出牌；普通 Effects 後、離場前派送，不與 Played 同時派送 |

共用 [CardTriggeredEffectDispatch](../Assets/Scripts/GameModel/Effect/CardTriggeredEffectDispatch.cs) 先快照符合條件的 CardData 效果，再快照當下有效 CardBuff 效果，固定依此順序排程。後續形態或 Buff 變動不回頭修改已建名單。

抽多張或效果棄多張時，逐張完成狀態、事件及觸發，再處理下一張。回合清手則先提交該玩家整批移牌與事件，再以固定順序執行生命週期；Ally 完成後才輪到 Enemy。Preserved 優先於 AutoDispose，不同時觸發 Discarded。Queue 預算邊界見 [Effect](Effect.md)。

## 效果與選取

普通 Effects 與 Conditional 生命週期效果共用 [Effect 管線](Effect.md)。傷害種類由 DamageEffect 的資料區分，不是各自獨立的穿甲／追加效果型別。

InvokeCardEffects 可借用指定卡片的一次普通 Effects，但不代表出牌，也不派送出牌生命週期。牌區、施放者、選取與巢狀執行規則集中於 [原地卡效](Effect.md#原地卡效-invokecardeffects)。

主目標宣告可互動對象；ExistCard、模板與效果選項均已接入玩家 UI、敵方、PlayCardEffect 與 InvokeCardEffects 的多步驟選取，共用順序與完整結果驗證。玩家關閉任一步取消整次出牌；自動入口選取不成立時略過該次操作。規則見 [Target](Target.md#選取與查詢分離)，草稿與消費見 [Action](Action.md#選取草稿與提交)，UI 分工見 [Presenter](Presenter.md#輸入與選取)。

NewCard／NewPartialCard／NewEffect 仍為預留結構；動態合成使用獨立的模板／效果選取型別，沒有接入這些舊佔位型別。選取與動態敘述不代表完整傷害預覽已完成。

屬性 Entity 由 [CardPropertyEntityFactory](../Assets/Scripts/GameModel/Factory/CardPropertyEntityFactory.cs) 建立；卡牌修正器見 [CardBuff](CardBuff.md)。
