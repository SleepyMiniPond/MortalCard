# Target 目標與選取

> 核對日期：2026-11-22

Target 以可序列化資料描述在 TriggerContext 中要讀取的實體或集合。單一查詢回傳 Option，找不到為 None；集合查詢回傳空集合，不以 Dummy 或 null 代替。

## 視角

Selected 代表本次明確選取，Triggered 代表目前反應者，ActionCard 代表出牌 Action／Result 或 Invoke 來源中的卡片。它們不互相替代，詳見 [Action](Action.md)。

玩家可由陣營、卡片／角色擁有者、Buff 的 Owner／Caster 等關係解析。無 CurrentPlayer 的全域時機可使用 PlayerByFaction；無效陣營回傳缺值。角色集合保留 Runtime 順序，不自動略過死亡角色。

## 集合契約

- CardsOfPlayer 支援五種一般牌區，維持原順序；PlayingCard 必須另行查詢，不混入手牌。
- FilteredCardCollection 對同一卡片的條件採 AND，保留符合項目的原順序。
- CombinedCardCollection 依 SetOperationType 合併兩個集合：Difference 為第一集合減去第二集合，Union 為聯集，Intersection 為交集。以卡片 Identity 去重，保留結果中第一次出現的順序。
- 索引查詢只接受 Ascending／Descending；Random、None 或無效值回傳 None。純查詢不消耗亂數。
- Buff ById 查不到回傳 None；CardBuff 集合只看當下有效 Layer，不同時讀取被 Override 凍結的 Base Layer。

## 選取與查詢分離

MainTargetSelectable 描述 UI／AI 可選範圍，Target 描述 Effect／Condition 實際讀取的來源。ExistCard 子選取結果依群組 ID 存入本次出牌或 Invoke 的 Context，`SubSelectedCardCollection` 依 ID 讀取仍存在的卡片集合。

CardData.SubSelects 的清單順序就是選取順序。每步用主目標與前面已確認結果計算本步候選及數量；群組只能引用前面的群組，不可自我或向後引用。不同群組可選同一卡片，需要排除前一步結果時，明確配置集合差集或條件。群組 ID 的製作及驗證規則見 [GameData](GameData.md#子選取配置)。

### 數量與候選不足

需求數量由 SelectCount 求值，可達數量為需求與去重後候選數量的較小值。

| CountRule | 本步合法確認數量 | 候選少於需求時 |
|---|---|---|
| Optional | 0～可達數量 | 可選空結果或任意合法數量 |
| RequiredAvailable | 必須等於可達數量 | 必須選完現有候選 |
| RequiredExact | 必須等於原始需求 | 拒絕整次操作，不回傳可執行結果 |

需求為零時三種規則都允許空結果。候選為零、需求大於零時，Optional／RequiredAvailable 自動記錄空結果，RequiredExact 拒絕操作。數量缺值或動態求得負值時也拒絕操作；數量規則不等同效果代價的實際支付。

這些規則不省略必要主目標。自動選取找不到必要主目標，或任一步不成立時，略過該次出牌／呼叫。自動入口目前沿用隨機挑卡並選滿可達數量，包含 Optional，尚未加入戰術策略。

### 逐步查詢與完整驗證

[CardSelectionQuery](../Assets/Scripts/GameModel/Info/CardSelectionQuery.cs) 的 QueryNext 依當下卡牌配置重驗已確認前綴，再回傳下一步、完成或拒絕。查詢不執行效果、不改變正式戰鬥狀態、不消耗亂數；暫時套用的 Context 在同步查詢後還原，不跨 UI 等待保存。

全部步驟確認後，Model 以 ValidateComplete 重新驗證主目標、群組、結果型別、候選及數量。非法、重複、缺漏或過期結果會拒絕整次操作，不自動修剪。未選群組與已確認的空群組有不同語意，草稿及提交契約見 [Action](Action.md#選取草稿與提交)。這些純查詢可供未來預覽使用；完整傷害預覽仍歸 T-014。

模板與效果群組也使用相同前綴重驗流程。QueryNext 回傳完整合法池，不抽樣；PrepareNext 明確委派 CardSelectionSession，依 OfferCount 抽出不重複候選並保存，重查同一輪不重抽。正式提交必須攜帶有效 OfferIdentity，驗證來源、施放者、主目標、前綴及當時實際提供的項目，不能只選完整池中未提供的選項。

每輪模板／效果選取固定選一項；提供數量為 OfferCount 與剩餘候選數的較小值，不能放回已排除選項補滿。效果池依已選模板切換，ExcludePreviouslySelected 依先前所有效果選項 ID 排除；排除後零候選拒絕整次操作。這套提供規則與 ExistCard 的 CountRule 分開，配置見 [GameData](GameData.md#子選取配置)。

NewCard／NewPartialCard／NewEffect 仍是未接線的預留結構，與已支援的模板／效果群組不同。

AI 自動選取不是純 Target 查詢；主目標以顯式玩家決定相對敵我視角，候選範圍與 First／Random 挑選策略分開配置，見 [GameModel](GameModel.md)。

## 程式導引

[卡片查詢](../Assets/Scripts/GameModel/Target/TargetCardValue.cs)、[玩家查詢](../Assets/Scripts/GameModel/Target/TargetPlayerValue.cs)、[角色查詢](../Assets/Scripts/GameModel/Target/TargetCharacterValue.cs)、[Buff 查詢目錄](../Assets/Scripts/GameModel/Target/)、[選取宣告](../Assets/Scripts/GameModel/Target/TargetSelectable.cs)。

Value 保留查詢缺值，Condition 依該契約判斷；必填引用與列舉由 Validator 檢查，見 [Value](Value.md)、[Condition](Condition.md)。
