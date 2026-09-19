# Entity 戰鬥實體

> 核對日期：2026-09-19

Entity 保存本場戰鬥的可變狀態，以 Identity 追蹤同一物件，並將能量、血量、卡牌區域與 Buff 管理分開。跨戰鬥保存責任見 [Instance](Instance.md)。

## 責任分工

| 實體 | 責任 | 詳細規則與程式 |
|---|---|---|
| Player | 管轄角色、牌區、能量及玩家 Buff；死亡由主角色判定 | [Player](Player.md)、[PlayerEntity](../Assets/Scripts/GameModel/Entity/Player/PlayerEntity.cs) |
| Character | 生命、護盾與角色 Buff | [Character](Character.md)、[CharacterEntity](../Assets/Scripts/GameModel/Entity/Character/CharacterEntity.cs) |
| Card | 有效形態、屬性與卡片 Buff，跨區域維持 Identity | [Card](Card.md)、[CardEntity](../Assets/Scripts/GameModel/Entity/Card/CardEntity.cs) |
| Buff | 層數、施放者、壽命及反應記憶 | [CardBuff](CardBuff.md)、[Session](Session.md) |

卡牌形態使用 Base／Self／External Override 優先層級，不再使用 mutation ID 清單。Clone 以目前有效 CardData 建立獨立卡片，不複製原 Identity、Instance 關聯、Buff 或形態歷史，見 [CardTransformation](CardTransformation.md)。

## 區域與反應

[PlayerCardManager](../Assets/Scripts/GameModel/Entity/Player/PlayerCardManager.cs) 管理 Deck、HandCard、Graveyard、ExclusionZone、DisposeZone；PlayingCard 是出牌暫態，不是一般區域。

全域查找包含 PlayingCard 與 DisposeZone。一般 Reaction 集合包含牌堆、手牌、墓地、排除區及 PlayingCard，排除 DisposeZone。戰鬥內移入 DisposeZone 不代表已寫回刪除跨戰鬥牌組。

## 不變量

狀態寫入端維護數值邊界，Effect 層另判斷操作是否合法。排隊後目標或 Buff Layer 失效是正常時序情況；相關 Handler 應安全結束，不製造假的結果與動畫。

Data 與 Runtime 元件由 [Factory](../Assets/Scripts/GameModel/Factory/) 轉換。具體欄位與 API 直接閱讀程式，各領域規則只在所屬文件維護。
