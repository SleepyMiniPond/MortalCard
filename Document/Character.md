# Character 角色系統

> 核對日期：2026-09-19

角色承載生命、護盾與 CharacterBuff。玩家可持有多個角色，但目前玩家死亡只由主角色決定；助戰角色死亡不單獨造成敗北。判定入口見 [PlayerEntity](../Assets/Scripts/GameModel/Entity/Player/PlayerEntity.cs) 與 [GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs)。

## 生命與護盾

[HealthManager](../Assets/Scripts/GameModel/Entity/Character/HealthManager.cs) 維護生命及護盾，回傳實際變化量供事件呈現。

| 操作 | 規則 |
|---|---|
| Normal／Additional 傷害 | 護盾先吸收，剩餘傷害扣生命 |
| Penetrate／Effective 傷害 | 略過護盾，直接扣生命 |
| 治療 | 不超過最大生命 |
| 獲得護盾 | 目前上限也是最大生命 |

[CharacterEntity](../Assets/Scripts/GameModel/Entity/Character/CharacterEntity.cs) 組合 HealthManager 與 BuffManager。傷害公式在 Resolver 求值，Handler 套用結果；不能假設傷害 Handler 自動派送死亡時機。

## CharacterBuff

角色 Buff 具有層數、壽命、Session 與一般 GameTiming 反應，核心效果共用 [Effect 管線](Effect.md)。資料與狀態管理見 [CharacterBuffData](../Assets/Scripts/GameData/CharacterBuff/CharacterBuffData.cs) 與 [CharacterBuffManager](../Assets/Scripts/GameModel/Entity/Character/CharacterBuff/CharacterBuffManager.cs)。

MaxHealth／MaxEnergy 屬性資料與實體已存在，但目前 CharacterEntity／PlayerEntity 的上限直接讀取各 Manager，不能將屬性存在等同於已動態影響資源上限。此接線缺口見 [TODO](TODO.md)。

角色畫面見 [CharacterView](CharacterView.md)；動態增減角色仍屬 T-013。
