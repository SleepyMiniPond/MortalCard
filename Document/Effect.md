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

正式根入口使用 `IGameplayModel.RunEffectBatch`，由 GameplayManager 建立 [CardPlayChain](../Assets/Scripts/GameModel/Effect/CardPlayChain.cs)。同一張牌的各次 EffectRepeat、Played／EffectPlayed、一般 Timing 與後續間接牌雖使用不同 Runner，仍共用同一 ExecutionScope。獨立根批次及直接建立的 Runner 保持獨立預算；靜態 RunToCompletion 不參與 GameplayManager 出牌鏈。

`EnqueueCardPlay` 只提交卡片／擁有者 Identity 與請求來源，不在 Handler 中執行完整出牌。根批次完成後依 FIFO 處理；A 排入 B、C，B 再排入 D，順序為 A 完整結束 → B → C → D。輪到請求時才重新檢查手牌、Sealed 與自動選取；選取視角為擁有者。每張成功出牌在離場、Recycle、AfterPlayCardEnd 之後判定勝負，結束時清除其餘請求。

`PlayCardEffect.TargetCards` 支援 Card／PlayerBuff／CharacterBuff／CardBuff 四來源。Resolver 依卡片 Identity 在本次候選內去重，固定卡片、擁有者與請求來源；不同效果解析之間不去重，因此 Recycle 後可再次被請求打出。Handler 只入列，不產生成功 Action 或事件。完整出牌核心免付費執行 Effects 與一次 EffectPlayed，不派送 Played；成功進入 PlayingCard 時移除敵方對應預選，失效請求不動預選。

GameplayManager 以 Option<CardPlayChain> 表達是否處於執行鏈內。主動出牌與效果批次以不同的根工作資料呼叫 _RunCardPlayChain，由方法內的 switch 明確執行，不傳入委派。根方法統一處理 Budget、例外事件保存與 finally 清理；正常完成時，主動出牌入口將回傳事件存入 _gameEvents，效果批次入口則將 EffectResult 交給呼叫端，避免重複加入事件。_DrainCardPlayRequests 透過 TryDequeue 取出請求、計算 Budget 並直接執行。

每個 Queue item、出牌嘗試（包含失效請求）及 Repeat 輪次均計入預算，避免零效果牌或極大 Repeat 繞過上限。Budget 超限會中止整條鏈，不繼續派送尚未發生的成功事件；中止前已完成項目的事件會保留。PlayingCard 與 Selected 作用域必定釋放，異常離場另提供真實牌區同步；例外／取消／勝負結束後清除請求，下一根批次使用新預算。

抽牌與效果棄牌的逐卡觸發沿用其原 Runner；回合清手的同一玩家 Preserved／Discarded 共用一個 Runner。FormChanged 的 Self 變形／Override 解除走專用 CardData 路徑；Override 套用及 CardBuff 派送尚未接線，詳見 [CardTransformation](CardTransformation.md)。

## 上下文與失效

Owner 是反應來源的實際宿主玩家；Caster 是 Buff 建立時保存的施放者，直接卡牌效果則取卡片持有者。無法解析時回傳缺值，不猜測 CurrentPlayer，見 [ReactionContextQuery](../Assets/Scripts/GameModel/Effect/ReactionContextQuery.cs)。

Selected 仍是操作選取，PlayingCard 仍是暫態；反應規劃不改寫兩者。排隊後來源區域、目標或 Layer 已失效，相關操作安全 No-op，不產生假的 Result／Event。資料缺漏與不支援型別由 Validator 處理。

數值缺值及合法範圍見 [Value](Value.md)、[Coding_Standards](Coding_Standards.md)；卡片時機的固定順序見 [Card](Card.md)。
