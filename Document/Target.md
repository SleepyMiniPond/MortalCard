# Target 目標與選取

> 核對日期：2026-09-19

Target 以可序列化資料描述在 TriggerContext 中要讀取的實體或集合。單一查詢回傳 Option，找不到為 None；集合查詢回傳空集合，不以 Dummy 或 null 代替。

## 視角

Selected 代表玩家／AI 明確選擇，Triggered 代表目前反應者，ActionCard 代表出牌 Action／Result 的來源卡片。它們不互相替代，詳見 [Action](Action.md)。

玩家可由陣營、卡片／角色擁有者、Buff 的 Owner／Caster 等關係解析。無 CurrentPlayer 的全域時機可使用 PlayerByFaction；無效陣營回傳缺值。角色集合保留 Runtime 順序，不自動略過死亡角色。

## 集合契約

- CardsOfPlayer 支援五種一般牌區，維持原順序；PlayingCard 必須另行查詢，不混入手牌。
- FilteredCardCollection 對同一卡片的條件採 AND，保留符合項目的原順序。
- 索引查詢只接受 Ascending／Descending；Random、None 或無效值回傳 None。純查詢不消耗亂數。
- Buff ById 查不到回傳 None；CardBuff 集合只看當下有效 Layer，不同時讀取被 Override 凍結的 Base Layer。

## 選取與查詢分離

MainTargetSelectable 描述 UI／AI 可選範圍，Target 描述 Effect／Condition 實際讀取的來源。ExistCard 子選取已有處理；NewCard／NewPartialCard／NewEffect 目前為預留結構，不代表已完成互動或效果消費。完整多步驟契約見 T-011。

AI 自動選取不是純 Target 查詢；目前主目標視角及 ToRandom 限制見 [GameModel](GameModel.md)，不要由標籤名稱推論行為。

## 程式導引

[卡片查詢](../Assets/Scripts/GameModel/Target/TargetCardValue.cs)、[玩家查詢](../Assets/Scripts/GameModel/Target/TargetPlayerValue.cs)、[角色查詢](../Assets/Scripts/GameModel/Target/TargetCharacterValue.cs)、[Buff 查詢目錄](../Assets/Scripts/GameModel/Target/)、[選取宣告](../Assets/Scripts/GameModel/Target/TargetSelectable.cs)。

Value 保留查詢缺值，Condition 依該契約判斷；必填引用與列舉由 Validator 檢查，見 [Value](Value.md)、[Condition](Condition.md)。
