# CardBuff 卡牌修正器

> 核對日期：2026-09-19

CardBuff 作用於單張卡牌，PlayerBuff 作用於玩家，CharacterBuff 作用於角色。CardBuff 目前可提供封印、威力及 EffectRepeat 修正；具體資料見 [CardBuffPropertyData](../Assets/Scripts/GameData/CardBuff/CardBuffPropertyData.cs)。

## 反應與壽命

[CardBuffData](../Assets/Scripts/GameData/CardBuff/CardBuffData.cs) 分開保存卡片生命週期 Effects 與一般 GameTiming 的 BuffEffects。一般反應由 Timing Planner 排程；生命週期以 [Card](Card.md) 的接線表為準。Initialize、抽牌、主動出牌、清手及效果棄牌已共用派送；FormChanged 尚未接入 CardBuff，EffectPlayed 尚無正式出牌入口。

Always 壽命不自動過期；Turn 壽命於 AfterTurnEnd 扣減；HandCard 壽命追蹤卡片是否仍在手牌。層數為 0 不等於移除，更新時由壽命決定是否過期。實作入口：[CardBuffEntity](../Assets/Scripts/GameModel/Entity/CardBuff/CardBuffEntity.cs)、[CardBuffLifeTimeEntity](../Assets/Scripts/GameModel/Entity/CardBuff/CardBuffLifeTimeEntity.cs)。

條件及 Session 機制見 [Condition](Condition.md)、[Session](Session.md)，效果來源見 [Effect](Effect.md)。

## Layer 所有權

[CardBuffLayerManager](../Assets/Scripts/GameModel/Entity/CardBuff/CardBuffLayerManager.cs) 對外提供目前有效 Layer：

- Self Transform 沿用 Base Layer。
- External Override 建立新的 Override Layer，期間凍結 Base Layer。
- 新 Override 取代舊 Override，舊層失效且不會在解除後復活。
- 解除目前 Override 時丟棄該層 Buff，恢復原 Base Layer 的身份、層數、壽命與 Session。
- 排隊命令持有 LayerHandle，失效後安全 No-op。

PlayerBuff 不屬於此 Layer，仍可作用於 Override 形態。完整形態契約見 [CardTransformation](CardTransformation.md)。
