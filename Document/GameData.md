# GameData 資料定義層

> 核對日期：2026-11-22

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

巢狀內容驗證先執行共同規則，再執行各資產專屬規則；卡牌及三種 Buff 共用同一份檢查清單。[ValueConditionValidator](../Assets/Scripts/Editor/ValueConditionValidator.cs) 負責條件、整數運算與靜態溢位檢查，GameDataValidator 保留資產路徑、驗證順序及訊息去重。新增共同規則應接到共同入口，避免漏驗某種資產。

## 宣告式資料

卡牌配置主／子目標、普通效果、生命週期效果、屬性及形態規則。Buff 配置屬性、壽命、Session 與一般反應效果；CardBuff 另有卡片生命週期效果。領域規則見 [Card](Card.md)、[CardBuff](CardBuff.md)、[Player](Player.md)、[Character](Character.md)。

效果透過 [Target](Target.md)、[Value](Value.md)、[Condition](Condition.md) 組合描述對象、數值及資格。這些可執行資料仍會讀取 Runtime Context；GameData 與 GameModel 尚未拆成完全獨立的 assembly。

Property、LifeTime 與 Session Data 保存配置，對應實體由 [GameModel Factory](../Assets/Scripts/GameModel/Factory/) 建立；目前不是 Data 自帶 CreateEntity 的模式。

## 子選取配置

CardData.SubSelects 依清單順序宣告步驟。目前使用 ExistCardSelectionGroup 配置候選、動態需求數量及 CountRule；數量與不足行為集中於 [Target](Target.md#數量與候選不足)。CountRule 已取代 IsMustSelect，不保留舊欄位或相容讀取。

群組 ID 在同張卡內必須非空且唯一。候選或數量若引用 SubSelectedCardCollection，只能使用前面已宣告的 ExistCard 群組；效果中的引用也須對應本張卡的群組。Validator 檢查 ID、相依順序、CountRule、必要巢狀資料及可靜態求得的負數；動態求值與玩家結果合法性由 Runtime 查詢／提交驗證處理。

選取提示從 UI 本地化表使用 `CardSelection.{CardDataId}.{SubSelectionId}` 查詢，缺少專屬文字時使用 `CardSelection_Default`。資產不另存 PromptKey；新增專屬提示時，依這兩個 ID 加入文字表。具體製作與檢查見 [資產製作規範](GameData_Asset_Guidelines.md)。NewCard／NewPartialCard／NewEffect 的 Runtime 選取仍未接線。

動態合成使用 [CardCompositionData](../Assets/Scripts/GameData/Card/CardCompositionData.cs)：

| 配置 | 責任 |
|---|---|
| CardTemplateSelectionGroup | TemplateCardDataIds 提供 Catalog 中的 Standard 模板 |
| CardEffectSelectionGroup | TemplateSelectionId 引用前面的模板群組；PoolsByTemplateId 對應各模板的選項池 |
| CardEffectOption | 穩定 Id、DescriptionKey、Effects、TriggeredEffects 及 DescriptionParameters |
| CreateComposedCardEffect | 引用 TemplateSelectionId，指定接收玩家及一般牌區目的地 |

輪數由 SubSelects 清單決定，每輪選一項；模板／效果群組的 OfferCount 必須明確設定正數，不預填數量。模板池必須完整，選項依 Id 排除此前選擇，不比較效果內容去重。候選不足及抽樣規則見 [Target](Target.md#逐步查詢與完整驗證)。選項內的子選取引用以成品模板為作用域，不以合成來源卡的群組替代。

DescriptionKey 與規則參數 RuleKey 使用 UI 本地化表。片段佔位沿用 `{:damage}` 格式；DescriptionParameters 的 Value 必須直接引用本選項普通或觸發效果中的 IIntegerValue，不另外配置數值副本。非純常數參數須提供 RuleKey。Validator 檢查描述鍵、佔位參數、保留名稱 cost／power 及來源綁定；文字是否完整表達時機、條件與效果仍需審閱。

## 效果與時機邊界

Card 與三種 Buff 共用已註冊的核心操作，但各來源有獨立 Registry。能序列化至某欄位不代表已有正式 Runtime 入口，允許範圍以 [Effect](Effect.md) 與 [Resolver 註冊](../Assets/Scripts/GameModel/Effect/EffectDataResolver.cs) 為準。

卡片生命週期的接線狀態集中記錄於 [Card](Card.md)。EffectPlayed 已接入 PlayCardEffect 的間接出牌流程；FormChanged 目前只走 CardData 專用入口，不能推論所有 CardBuff timing 都已生效。

共用列舉見 [GameEnum](../Assets/Scripts/GameData/GameEnum.cs)，卡牌列舉見 [CardEnum](../Assets/Scripts/GameData/Card/CardEnum.cs)；領域專用列舉亦存在於各模組，不在文件複製完整成員清單。
