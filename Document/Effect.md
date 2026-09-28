# Effect 效果管線

> 核對日期：2026-09-28

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
| PlayCardEffect | 四來源共用；提交擁有者手牌的完整間接出牌請求 |
| InvokeCardEffects | 四來源共用；原地執行指定卡片的一次普通效果 |

新增能力須同步確認資料介面、Registry 與 [GameDataValidator](../Assets/Scripts/Editor/GameDataValidator.cs)。Validator 會檢查 PlayCardEffect／InvokeCardEffects 的必要目標、巢狀必要欄位、SubSelectedCardCollection 的非空群組 ID，以及卡片資料中引用的 ExistCard 群組是否存在。型別清單直接閱讀程式，不在文件複製。未知效果在 Runtime 警告並回傳空命令集合，正式資產應於驗證階段被攔截。

## 原地卡效 InvokeCardEffects

Invoke 是「借用卡片的普通效果」，不是正式出牌。`TargetCards` 可以指定雙方 HandCard／Deck／Graveyard 的卡片，不接受 PlayingCard、ExclusionZone 或 DisposeZone。

[Resolver](../Assets/Scripts/GameModel/Effect/Resolvers/InvokeCardEffectsResolver.cs) 在每次解析內依 Identity 去重，固定卡片、持有者、原牌區與施放者；不同呼叫之間不去重。[Handler](../Assets/Scripts/GameModel/Effect/Handlers/InvokeCardEffectsCommandHandler.cs) 輪到該卡時重新檢查持有者與原牌區，已移區或轉移所有權便略過。此時才讀取當前形態及選取配置，將一次普通 Effects 固定為陣列。

呼叫本身不搬牌、不付費、不檢查 Sealed、不套用 EffectRepeat，也不產生 UsedCardEvent、Played／EffectPlayed、正式出牌前後時機、Recycle 或出牌離場。普通效果本身若會移牌、消耗能量或提交 PlayCardEffect，仍照原規則執行。

自動主目標、ExistCard 候選與選取數量皆使用明確傳入的 caster；必要主目標選不到、施放者無法解析，或遇到未支援的子選取類型時略過該次呼叫。沒有主選取需求可正常執行；ExistCard 候選不足取可達數量。一次呼叫的所有普通效果共用同一份完整選取結果。

| 發起情境 | 本次 Invoke 的 caster |
|---|---|
| 一般卡片效果 | 卡片持有者 |
| Invoke 中直接再次 Invoke | 沿用目前 Invoke caster |
| PlayerBuff 發起 | Buff 宿主玩家 |
| CharacterBuff／CardBuff 發起 | 宿主角色／卡片所屬玩家 |

Buff 保存的原始 caster 不會被改寫。A 呼叫 B 的「對敵方造成 2 點傷害」，是 A 對 B 傷害；A 把回合結束呼叫手牌的 Buff 放到 B 身上，則由 B 呼叫並對 A 傷害。玩家加成取本次 caster 的 PlayerBuff，保留被呼叫卡自身屬性與 CardBuff 修正，不沿用外層 CardPlayAttribute。明確 CardOwner 查詢仍指實際持有者，固定陣營查詢也不改義。

配置時須區分「選哪張卡」與「由誰執行」：Buff 的 `TargetCards = CardsOfPlayer`，搭配 `Player = ReactionOwnerPlayer`、`Zone = HandCard`，代表宿主玩家的手牌；再包索引或篩選可縮小集合。若用 SubSelectedCardCollection，ID 指發起效果所在選取作用域的群組，不是被呼叫卡的群組；被呼叫卡另建自己的選取。

被呼叫效果立即加入目前 Runner，不另開執行器。例如 A 呼叫 B、C，B 呼叫 D，順序為 B（途中先完成 D）→ C → A 後續效果。Invoke 與巢狀呼叫共用鏈預算；其中的 PlayCardEffect 仍等根批次完成後依 FIFO 正式出牌。實際傷害等結果沿既有管線交付一次；來源與時機見 [Action](Action.md#原地卡效與預覽來源)。

## 排程與預算

同一 Runner 中的立即衍生項目在目前項目之後優先執行，共用該 Runner 的 Scope／Budget；超限會停駐並保留診斷與未執行項目。

正式根入口使用 `IGameplayModel.RunEffectBatch`，由 GameplayManager 建立 [CardPlayChain](../Assets/Scripts/GameModel/Effect/CardPlayChain.cs)。同一張牌的各次 EffectRepeat、Played／EffectPlayed、一般 Timing 與後續間接牌雖使用不同 Runner，仍共用同一 ExecutionScope。獨立根批次及直接建立的 Runner 保持獨立預算；靜態 RunToCompletion 不參與 GameplayManager 出牌鏈。

`EnqueueCardPlay` 只提交卡片／擁有者 Identity 與請求來源，不在 Handler 中執行完整出牌。根批次完成後依 FIFO 處理；A 排入 B、C，B 再排入 D，順序為 A 完整結束 → B → C → D。輪到請求時才重新檢查手牌、Sealed 與自動選取；選取視角為擁有者。每張成功出牌在離場、Recycle、AfterPlayCardEnd 之後判定勝負，結束時清除其餘請求。

`PlayCardEffect.TargetCards` 支援 Card／PlayerBuff／CharacterBuff／CardBuff 四來源。Resolver 依卡片 Identity 在本次候選內去重，固定卡片、擁有者與請求來源；不同效果解析之間不去重，因此 Recycle 後可再次被請求打出。Handler 只入列，不產生成功 Action 或事件。完整出牌核心免付費執行 Effects 與一次 EffectPlayed，不派送 Played；成功進入 PlayingCard 時移除敵方對應預選，失效請求不動預選。

GameplayManager 以 Option<CardPlayChain> 表達是否處於執行鏈內。主動出牌與效果批次以不同的根工作資料呼叫 _RunCardPlayChain，由方法內的 switch 明確執行，不傳入委派。根方法統一處理 Budget、例外事件保存與 finally 清理；正常完成時，主動出牌入口將回傳事件存入 _gameEvents，效果批次入口則將 EffectResult 交給呼叫端，避免重複加入事件。根方法透過 TryDequeue 取出請求、計算 Budget 並直接執行。

每個 Queue item、出牌嘗試（包含失效請求）及 Repeat 輪次均計入預算，避免零效果牌或極大 Repeat 繞過上限。Budget 超限會中止整條鏈，不繼續派送尚未發生的成功事件；中止前已完成項目的事件會保留。PlayingCard 與 Selected 作用域必定釋放，異常離場另提供真實牌區同步；例外／取消／勝負結束後清除請求，下一根批次使用新預算。

抽牌與效果棄牌的逐卡觸發沿用其原 Runner；回合清手的同一玩家 Preserved／Discarded 共用一個 Runner。FormChanged 的 Self 變形／Override 解除走專用 CardData 路徑；Override 套用及 CardBuff 派送尚未接線，詳見 [CardTransformation](CardTransformation.md)。

## 上下文與失效

Owner 是反應來源的實際宿主玩家；Caster 在 Buff 上是建立時保存的施放者，一般直接卡牌效果取卡片持有者，Invoke 卡效則取本次 Invoke caster。無法解析時回傳缺值，不猜測 CurrentPlayer，見 [ReactionContextQuery](../Assets/Scripts/GameModel/Effect/ReactionContextQuery.cs)。

Selected 是目前卡效使用的選取，PlayingCard 仍是暫態；反應規劃不以反應來源覆寫兩者。Runner 建構時注入該場戰鬥的 ContextManager，每筆排程只保存可選的 GameContext 快照，不另存 manager。快照包含 SelectedPlayer／SelectedCharacter／SelectedCard 與群組選取，不是整場戰鬥的複本。

入列統一使用 Enqueue，以 Tail／Immediate 決定位置；介面只保留集合入口，單項及命令輸入由 extensions 轉交。省略 selection 表示繼承目前工作，`GameContext.EMPTY.Some()` 表示明確清空。每筆工作執行時套用自己的選取，衍生工作繼承；巢狀 Invoke 可覆寫，完成、例外、取消或預算中止都還原作用域。因此內層選 Y 不會讓外層後續原本選 X 的效果改打 Y。

排隊後來源區域、目標或 Layer 已失效，相關操作安全 No-op，不產生假的 Result／Event。資料缺漏與不支援型別由 Validator 處理。

數值缺值及合法範圍見 [Value](Value.md)、[Coding_Standards](Coding_Standards.md)；卡片時機的固定順序見 [Card](Card.md)。
