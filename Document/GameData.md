# GameData 資料定義層

> 核對日期：2026-09-19

GameData 定義卡牌、Buff、玩家與反應規則的配置。ScriptableObject 是 Unity 資產容器，內部 Data 並非全都是 ScriptableObject，也不是由型別強制保證的不可變資料。

## 內容來源

| 來源 | 責任 |
|---|---|
| [GameContentCatalog](../Assets/Scripts/GameData/Scriptable/GameContentCatalog.cs) | Standard／Override 卡牌與三種 Buff 的權威資產集合 |
| [AllPlayerScriptable](../Assets/Scripts/GameData/Scriptable/AllPlayerScriptable.cs) | Ally／Enemy 配置 |
| [DeckScriptable](../Assets/Scripts/GameData/Scriptable/DeckScriptable.cs) | 牌組配置 |
| [ExcelDatas](../Assets/Scripts/GameData/Scriptable/ExcelDatas.cs) | 匯入常數、好感度與本地化表 |
| [ScriptableDataLoader](../Assets/Scripts/Presenter/Gameplay/ScriptableDataLoader.cs) | 讀取上述來源供戰鬥建構使用 |

依 [資產製作規範](GameData_Asset_Guidelines.md) 重新產生目錄並驗證。Build／Play Mode Gate 共用 [GameDataValidator](../Assets/Scripts/Editor/GameDataValidator.cs)，防止目錄遺漏、引用錯誤與不支援的效果進入 Runtime。

## 宣告式資料

卡牌配置主／子目標、普通效果、生命週期效果、屬性及形態規則。Buff 配置屬性、壽命、Session 與一般反應效果；CardBuff 另有卡片生命週期效果。領域規則見 [Card](Card.md)、[CardBuff](CardBuff.md)、[Player](Player.md)、[Character](Character.md)。

效果透過 [Target](Target.md)、[Value](Value.md)、[Condition](Condition.md) 組合描述對象、數值及資格。這些可執行資料仍會讀取 Runtime Context；GameData 與 GameModel 尚未拆成完全獨立的 assembly。

Property、LifeTime 與 Session Data 保存配置，對應實體由 [GameModel Factory](../Assets/Scripts/GameModel/Factory/) 建立；目前不是 Data 自帶 CreateEntity 的模式。

## 效果與時機邊界

Card 與三種 Buff 共用已註冊的核心操作，但各來源有獨立 Registry。能序列化至某欄位不代表已有正式 Runtime 入口，允許範圍以 [Effect](Effect.md) 與 [Resolver 註冊](../Assets/Scripts/GameModel/Effect/EffectDataResolver.cs) 為準。

卡片生命週期的接線狀態集中記錄於 [Card](Card.md)。EffectPlayed 仍待間接出牌流程；FormChanged 目前只走 CardData 專用入口，不能推論所有 CardBuff timing 都已生效。

共用列舉見 [GameEnum](../Assets/Scripts/GameData/GameEnum.cs)，卡牌列舉見 [CardEnum](../Assets/Scripts/GameData/Card/CardEnum.cs)；領域專用列舉亦存在於各模組，不在文件複製完整成員清單。
