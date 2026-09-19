# Effect 效果管線

> 核對日期：2026-09-19

Effect 將宣告式資料轉成狀態變更：Resolver 解析目標與數值 → Command 表達操作 → Queue 安排順序 → Handler 套用狀態並產生結果與事件。

需要先建立直覺時，閱讀 [Effect Queue 觸發循環圖解](EffectQueue_VisualGuide.md)：包含 Runner 循環、Buff 工作展開、插隊順序與逐步範例。

## 責任邊界

| 元件 | 責任與程式入口 |
|---|---|
| Resolver | [解析資料與註冊來源](../Assets/Scripts/GameModel/Effect/EffectDataResolver.cs)，缺少合法目標／值時不建立命令 |
| Queue | [EffectQueueRunner](../Assets/Scripts/GameModel/Effect/EffectQueueRunner.cs) 排程命令及衍生效果，維護 Scope 與預算 |
| Executor／Handler | [命令派發](../Assets/Scripts/GameModel/Effect/EffectCommandExecutor.cs) 與[狀態寫入](../Assets/Scripts/GameModel/Effect/Handlers/) |
| Timing Planner | [快照一般反應來源](../Assets/Scripts/GameModel/Effect/TimingDispatchPlanner.cs)，依 PlayerBuff → CharacterBuff → CardBuff 排程 |
| 卡片生命週期 | [CardTriggeredEffectDispatch](../Assets/Scripts/GameModel/Effect/CardTriggeredEffectDispatch.cs)，規則集中於 [Card](Card.md) |

數值公式在相應 Resolver 評估，Handler 使用命令數值套用狀態。結果 Action 供 Session／觀察更新，GameEvent 供畫面呈現；EffectResult.Actions 彙整 Result，不是所有 Intent 的完整歷史。

## 來源能力

Card、PlayerBuff、CharacterBuff、CardBuff 各有明確的 Resolver Registry，不會因其他來源已註冊便自動支援。

| 操作範圍 | 來源 |
|---|---|
| 傷害、治療、護盾、能量、好感度、抽牌、移牌、建立／複製卡牌、PlayerBuff 與 CardBuff 操作 | 四來源共用 |
| ModifyCardPlayAttribute | 三種 Buff Reaction 專用 |
| ApplyCardFormOverride | ICardEffect 路徑，可用於普通或卡片生命週期效果；不開放三種 Buff Registry |

新增能力須同步確認資料介面、Registry 與 [GameDataValidator](../Assets/Scripts/Editor/GameDataValidator.cs)。型別清單直接閱讀程式，不在文件複製。未知效果在 Runtime 警告並回傳空命令集合，正式資產應於驗證階段被攔截。

## 排程與預算

同一 Runner 中的立即衍生項目在目前項目之後優先執行，共用該 Runner 的 Scope／Budget；超限會停駐並保留診斷與未執行項目。

靜態 RunToCompletion 每次建立新 Runner。現有出牌流程的各次 EffectRepeat、Played 與一般 Timing 根入口分別建立 Runner，因此尚無涵蓋整條出牌鏈的共同預算。T-022 要求的間接出牌鏈 Budget 是後續能力，不能當作既有保證。

抽牌與效果棄牌的逐卡觸發沿用其原 Runner；回合清手的同一玩家 Preserved／Discarded 共用一個 Runner。FormChanged 的 Self 變形／Override 解除走專用 CardData 路徑；Override 套用及 CardBuff 派送尚未接線，詳見 [CardTransformation](CardTransformation.md)。

## 上下文與失效

Owner 是反應來源的實際宿主玩家；Caster 是 Buff 建立時保存的施放者，直接卡牌效果則取卡片持有者。無法解析時回傳缺值，不猜測 CurrentPlayer，見 [ReactionContextQuery](../Assets/Scripts/GameModel/Effect/ReactionContextQuery.cs)。

Selected 仍是操作選取，PlayingCard 仍是暫態；反應規劃不改寫兩者。排隊後來源區域、目標或 Layer 已失效，相關操作安全 No-op，不產生假的 Result／Event。資料缺漏與不支援型別由 Validator 處理。

數值缺值及合法範圍見 [Value](Value.md)、[Coding_Standards](Coding_Standards.md)；卡片時機的固定順序見 [Card](Card.md)。
