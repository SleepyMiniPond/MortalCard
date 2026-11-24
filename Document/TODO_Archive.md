# 任務與階段歷史

> 整理日期：2026-11-22
> 各項完成日期與驗證數字為當時紀錄；T-023 的驗證於 2026-09-28 執行，2026-09-29 確認封存。現在行為以系統文件及程式為準，待辦見 [TODO](TODO.md)。
> 完成狀態限於各任務範圍，不代表所有預留列舉、流程入口或後續整合都已完成。

## T-021：初始抽牌優先與效果選單設定（已完成）

- **狀態**：✅ 2026-11-22 使用者最終確認第 4 個工作，四項工作全部結案。
- **初始牌序**：雙方在 BeforeGameStart／初始 Initialize 完成後、AfterGameStart 前，依當下有效 InitialPriority 穩定分組一次；同組保留洗牌順序，不額外消耗亂數、不增加抽牌數、不召回已移出的牌。初始化新增或變形的牌仍在 Deck 即參與；後續新增與墓地回洗不重新優先。
- **呈現與開場效果**：InitialDeckOrderedEvent 同步 View 牌序；沿用 Buff 的 AfterGameStart 反應，不新增 CardTriggeredTiming。Initialize 避免抽牌屬製卡規範，Runtime 不拒絕或延後抽牌。
- **Editor 設定**：正式 EffectMenuSettings.asset 分別管理 GameTiming／CardTriggeredTiming 的效果停用清單，支援型別選單、相同時機聯集、規則修改後重新開啟選單生效。停用項目灰色置底、不可選，無 toggle／hint；既有配置保留並可編輯，Runtime 不讀此設定。
- **使用者確認與清理**：使用者確認 Inspector 及設定資產體驗後授權清理；確認無外部依賴，透過 Unity AssetDatabase 移除 Assets/Acceptance 的試用 CardBuff、空資料夾與全部 meta。正式設定、程式及回歸測試保留。
- **最終驗證（2026-11-22）**：清理後 Unity 刷新／重新編譯及 GameDataValidator 0 error，完整 EditMode 950／950、PlayMode 45／45，0 failed／skipped。本輪最終確認只更新文件，不重跑已通過的測試。
- **驗證界線與警告**：初始抽牌由 19 項行為測試覆蓋，未宣稱另做人工 Play 玩法驗收；Inspector 與設定由 AI 實際操作及使用者確認。既有 RenderGraph 相容模式、NoOpCardBuffEffect 測試空效果警告保留。
- **規則來源**：[Card](Card.md)、[GameModel](GameModel.md)、[資產製作規範](GameData_Asset_Guidelines.md#開場時機與效果選單)。下一項 T-024 由新對話接手。

### T-001：消除 Switch Expression 雙重派發

採用型別 Registry 分離 Resolver 與 Handler，移除大型雙重派發。新增型別仍須登錄對應 Registry，不能說完全不改動註冊本體。現況見 [Effect](Effect.md)。

- **狀態**：✅ 已完成（2026-05-11）
- **歷史驗證**：原封存項目未列獨立驗證結果；本輪未補推測數字。

### T-002：接通 CardBuff / CharacterBuff 觸發管線

PlayerBuff、CharacterBuff、CardBuff 的一般反應時機已接入效果流程；目前統一由 Timing Planner／Queue 排程，與卡片生命週期分開。見 [Effect](Effect.md)。

- **狀態**：✅ 已完成（2026-05-15）
- **歷史驗證**：原封存項目未列獨立驗證結果；本輪未補推測數字。

### T-003：評估 Effect Queue 機制

建立 Effect Queue、立即衍生排程與預算停駐診斷。完成範圍不含效果取消／替代，也不保證整條完整出牌鏈共用預算。見 [Effect](Effect.md)。

- **狀態**：✅ 已完成（2026-06-29）
- **歷史驗證結果**：
  - `dotnet build MortalGame.EditModeTests.csproj`：0 error，2 warnings（既有 `GameplayManager.OneTurnStart` / `OnTurnEnd` 未使用）
  - Unity MCP `assets-refresh`：成功
  - Unity MCP `tests-run` EditMode：63 passed / 0 failed / 0 skipped
  - 測試中仍有 `NoOpCardBuffEffect` 未知 resolver warning，屬於既有測試用空效果案例

### T-004：建立核心 EditMode 測試與測試資料建構器

建立核心 EditMode 測試、資料建構器及可注入戰鬥測試接縫，覆蓋 Buff Timing 與 Selected Context。測試入口見 [EditMode](../Assets/Tests/EditMode/)。

- **狀態**：✅ 已完成（2026-06-25）
- **歷史驗證結果**：
  - `BuffTimingPipelineTests`：6 passed / 0 failed
  - `GameContextManagerTests`：4 passed / 0 failed
  - 完整 EditMode 測試：56 passed / 0 failed
  - 驗證使用 Unity `6000.0.3f1` batchmode，在臨時專案副本執行，避免與使用者開啟中的 Unity Editor 搶 project lock

### T-005：修正場景級 UniTask 取消與 Presenter 生命週期

建立 Main → Scene → Presenter → UI／Model 取消鏈，監督必要並行工作並等待角色動畫清理。勝利面板完成入口不在當時範圍，現由 T-024 追蹤。見 [Presenter](Presenter.md)、[CharacterView](CharacterView.md)。

- **狀態**：✅ 已完成（2026-07-12）
- **歷史驗證結果**：
  - Unity AssetDatabase refresh：成功，0 compile error
  - Unity EditMode Tests：111 tests，狀態 Passed，0 failed（新增 4 項生命週期測試）
  - `dotnet build MortalGame.Scene.csproj`：0 warning / 0 error
  - `dotnet build MortalGame.EditModeTests.csproj`：0 warning / 0 error
  - 七個 EventView Prefab 的 PlayableDirector 序列化引用保持有效

### T-006：導入戰鬥專用決定性亂數服務

以戰鬥種子建立並注入 IGameRandom，洗牌與既有子選取使用同一服務。當時主目標 ToRandom 仍取第一個；此缺口已於 T-022 的共用選取工作完成。見 [GameModel](GameModel.md)。

- **狀態**：✅ 已完成（2026-07-07）
- **歷史驗證結果**：
  - Unity AssetDatabase refresh：0 compile error
  - Unity EditMode tests：73 passed / 0 failed / 0 skipped
  - `dotnet build MortalGame.EditModeTests.csproj`：0 error

### T-007：完成模組命名空間、依賴反轉與 asmdef 遷移

建立 Runtime、UI、Presentation Abstractions、GameView、Presenter、Scene 的 assembly 邊界，將協調程式移回 Presenter。GameData／GameModel 仍同屬 Runtime；完整內容資料與執行層拆分不在完成範圍。見 [SystemArchitecture](SystemArchitecture.md)。

- **狀態**：✅ 已完成（2026-07-11）
- **歷史驗證結果**：
  - Unity AssetDatabase refresh：0 compile error
  - Unity EditMode tests：107 passed / 0 failed / 0 skipped
  - PlayMode Tests 目前只有 asmdef，尚無可執行測試案例
  - `dotnet build MortalGame.Scene.csproj`：0 warning / 0 error
  - `dotnet build MortalGame.EditModeTests.csproj`：0 error；僅有既有 UniRx 過時 API 與未使用事件警告
  - Unity 掃描全部 Prefab / Scene：未發現 Missing Script
  - ScriptableObject、Prefab、Scene 資產未發現舊 `Assembly-CSharp` managed-reference 型別識別

### T-008：建立 ScriptableObject 資料驗證與 Resolver / Handler 註冊檢查

建立共用 GameDataValidator、Registry 與內容引用檢查，以及 Unity 驗證選單；測試與人工操作共用規則。後續由 T-018 擴充 Catalog、Play／Build Gate。見 [資產規範](GameData_Asset_Guidelines.md)。

- **狀態**：✅ 已完成（2026-07-23）
- **歷史驗證結果**：
  - Unity MCP `assets-refresh`：成功，0 compile error
  - Unity MCP `tests-run` EditMode：67 passed / 0 failed / 0 skipped
  - `dotnet build MortalGame.EditModeTests.csproj`：0 warning / 0 error
- **工具化歷史驗證結果**：
  - Unity AssetDatabase refresh：0 compile error
  - `ScriptableObjectDataValidationTests`：3 passed / 0 failed / 0 skipped

### T-016：重構 TriggerTiming 與 UpdateReactorSessionAction 的時機模型

統一 Before／After Timing，先更新 Session 再規劃 Buff 反應；WholeTurn／PlayCard 有明確重置與清除邊界，Buff 回合壽命於 AfterTurnEnd 更新。當時遷移工具及舊列舉處理屬歷史工作，不代表目前仍保留工具。見 [Session](Session.md)。

- **狀態**：✅ 已完成（2026-07-12）
- **歷史驗證結果**：
  - Unity AssetDatabase refresh：0 compile error。
  - Migration Dry Run：0 safe / 0 review，已無可識別的舊 timing 資料。
  - GameTiming 序列化與 Migration mapping 測試通過。
  - T-016 Timing Pipeline、Buff Timing、Effect Queue 與 ScriptableObject validation 測試通過。

### T-015：CardInfo 氾濫 — 重複製造與事件冗餘

移除未被消費的重複 CardInfo 與 PlayingCard 快照；卡片 Buff 事件及移牌／出牌事件按需求只帶 Identity，一般狀態更新由 GeneralUpdateEvent 處理。見 [GameView](GameView.md)。

- **狀態**：✅ 已完成（2026-07-22）
- **歷史驗證**：原封存項目未列獨立驗證結果；本輪未補推測數字。

### T-010：卡片變身（保留狀態）

完成 Base／Self／External Override 層級、可取代 Override、Buff Layer、Clone、身份式 View 更新與勝利形態 ChangeSet 收集。戰鬥外套用與磁碟存檔未包含；FormChanged 各入口與 CardBuff 派送差異另列 T-025。見 [CardTransformation](CardTransformation.md)。

- **狀態**：✅ 已完成（2026-08-10）
- **歷史驗證結果**：
  - Unity 編譯：0 error。
  - GameData Validator 定向測試：11 項全數通過。
  - T-010 EditMode：74 項全數通過。
  - 完整 EditMode：222 項全數通過。

### T-018：統一內容目錄與資料驗證

以 GameContentCatalog 統一 Card／Buff 內容來源，Runtime 載入與 Validator 使用一致資料；加入 Play Mode／Build Gate。AllPlayerScriptable 仍保留玩家配置責任。見 [GameData](GameData.md)。

- **狀態**：✅ 已完成（2026-08-19）
- **歷史驗證結果**：
  - Unity 編譯：0 error。
  - 正式內容 `GameDataValidator.ValidateAll()`：通過，0 error。
  - 完整 EditMode：242 passed / 0 failed / 0 skipped。
  - Play Mode Gate smoke test：有效資料可正常進入 Play Mode，並已正常退出。
  - 測試保留 2 筆既有 `NoOpCardBuffEffect` 未知 Resolver warning，屬測試用空效果案例。

### T-019：通用遊戲狀態查詢／Value／Condition

完成可組合 Target／Value／Condition、Option 整數缺值、集合語意、反應起因 Timing 與資料驗證。查詢不消耗亂數；定時炸彈效果執行由 T-020 接續。見 [Target](Target.md)、[Value](Value.md)、[Condition](Condition.md)。

- **狀態**：✅ 已完成（2026-09-07）
- **歷史驗證結果**：
  - Unity 編譯：0 error。
  - 刀／盾整合測試：2 passed / 0 failed。
  - 定時炸彈查詢整合測試：3 passed / 0 failed。
  - 集合查詢整合測試：4 passed / 0 failed。
  - GameData Play Mode Gate：3 passed / 0 failed。
  - 完整 EditMode：486 passed / 0 failed / 0 skipped。
  - 測試保留 2 筆既有 `NoOpCardBuffEffect` 未知 Resolver warning，屬測試用空效果案例。

### T-020：統一 Reaction Effect 執行能力

四種來源明確註冊共用核心效果；ModifyCardPlayAttribute 保留 Reaction 專用，ApplyCardFormOverride 保留 ICardEffect 路徑。完成 Owner／Caster／Selected 邊界與舊效果資料遷移。見 [Effect](Effect.md)。

- **狀態**：✅ 已完成（2026-09-10）
- **歷史驗證結果**：
  - Unity 編譯：0 error。
  - 正式內容 `GameDataValidator.ValidateAll()`：0 errors。
  - 定向測試：定時炸彈與三來源傷害 6 passed、Registry 96 passed、Play Mode Gate 3 passed、Build Gate 3 passed。
  - 完整 Unity EditMode：603 passed / 0 failed / 0 skipped。
  - 測試保留 2 筆既有 `NoOpCardBuffEffect` unknown Resolver warning，屬測試用安全空 CommandSet 案例。

### T-017：完成 CardTriggeredTiming 生命週期觸發管線

共用 CardData → 有效 CardBuff 快照派送，完成 Initialize、Drawed／EffectDrawed、Played、Preserved／Discarded、EffectDiscarded。保留逐卡事件順序與原 Queue 連鎖。EffectPlayed／InvokeCardEffects 分別由 T-022／T-023 接續；FormChanged 差異由 T-025 追蹤。見 [Card](Card.md)。

- **狀態**：✅ 已完成（2026-09-18）
- **歷史驗證結果**：
  - Unity 編譯：0 error。
  - 正式內容 `GameDataValidator.ValidateAll()`：0 errors。
  - `ScriptableObjectDataValidationTests`：20 passed，包含 `ConditionalCardEffect` Timing、多型 Effect 與資產 Round Trip。
  - GameData Play Mode Gate：3 passed；Build Gate：3 passed；實際 Play Mode smoke test 可正常進入並退出。
  - 生命週期定向回歸：Dispatch 4 passed、Initialize 1 passed、Effect Queue／抽牌 39 passed、出牌 9 passed、清手 8 passed、卡片操作 27 passed。
  - 完整 EditMode：652 passed / 0 failed / 0 skipped。
  - 測試保留 2 則既有 `NoOpCardBuffEffect` 未知類型 Warning，屬測試用安全空 CommandSet 案例。

### T-022：完成 `EffectPlayed` 間接出牌能力

完成擁有者手牌的免付費間接出牌、主動／間接共用單張流程、顯式選取視角與 ExistCard 群組結果、四來源 PlayCardEffect，以及 FIFO 出牌鏈與共同 Budget。成功出牌依原因派送 Played 或 EffectPlayed；失效請求不產生成功事件。Validator、Odin 多型資料與畫面牌區事件已驗證。相關規則見 [Action](Action.md)、[Card](Card.md)、[Effect](Effect.md) 與 [Target](Target.md)。

- **狀態**：✅ 已完成（2026-09-23）。
- **歷史驗證結果**：Unity 編譯 0 error；正式內容 `GameDataValidator.ValidateAll()` 0 error；完整 EditMode 721 passed / 0 failed / 0 skipped；PlayMode 3 passed / 0 failed / 0 skipped。
- **驗證限制**：未執行實際 Player Build 或人工遊玩；測試紀錄有 2 則既有 `NoOpCardBuffEffect` Warning 與 3 則預期的 Budget 中止診斷。
- **後續邊界**：`InvokeCardEffects` 由 T-023 接續；其他子選取類型仍屬 T-011；更一般的出牌條件與 GameplayManager 職責拆分分別由 T-028、T-029 追蹤。

### T-023：完成 `InvokeCardEffects` 原地執行卡效能力

完成 Card／PlayerBuff／CharacterBuff／CardBuff 四來源原地卡效，支援雙方手牌、牌堆與墓地；執行前重新檢查持有者與原牌區。選取及玩家加成採明確 caster，Buff 發起時使用宿主玩家。Queue 統一入列介面，逐項保存與還原選取，巢狀 Invoke 與間接出牌共用鏈預算。四來源序列化與既有 Validator 已驗證。正式規則見 [Effect](Effect.md#原地卡效-invokecardeffects)、[Action](Action.md) 與 [Target](Target.md)。

- **狀態**：✅ 五項工作均已確認完成，2026-09-29 封存。
- **歷史驗證（2026-09-28）**：Unity 編譯成功；新增資產測試 15/15，完整 EditMode 775 passed / 0 failed / 0 skipped；差異格式檢查通過。
- **驗證限制**：本項未執行 PlayMode、實際 Player Build 或人工遊玩；測試保留既有 Render Graph 相容模式與 NoOpCardBuffEffect 警告，以及預期的 Budget 中止診斷。封存當日僅更新文件，未重跑測試。
- **後續邊界**：其他子選取類型與完整多步驟契約仍屬 T-011；未新增獨立 Invoke GameTiming、專用演出或正式測試卡牌資產。封存當時下一主線 T-011 尚未開始；最新進度以 TODO 為準。

## 現況導引

測試來源：[EditMode](../Assets/Tests/EditMode/)、[PlayMode](../Assets/Tests/PlayMode/)。T-007 當時「PlayMode 無案例」僅是歷史狀態，目前已有 CharacterAnimationWorkerTests。

歷史驗證中的工具、警告與測試數量不可當成最新結果；需要確認執行狀態時，依受影響範圍重新驗證。

## T-011：多步驟自訂目標選取（已完成）

- **狀態**：✅ 2026-11-14 使用者確認第 4 個工作「驗收與文件」完成，四個工作全部結案。
- **完成範圍**：ExistCard 多步驟順序、穩定群組 ID、前步結果相依、三種 CountRule、整次取消、提交重驗與選取作用域；玩家主動出牌透過 UI，敵方、PlayCardEffect 與 InvokeCardEffects 自動選取，共用規則。Invoke 保留本次施放者視角及原地卡效語意。
- **人工驗收**：由 AI 製作 8 種單一功能測試卡、獨立牌組與兩個入口，涵蓋兩步相依、必選不足、可達數量、零需求、排除前選、間接出牌與 Invoke；使用者確認測試卡與關閉操作通過，並明確確認 T-011 最後一步完成。
- **驗收修正**：修正數值特效銷毀後仍回收的競態，以及 Submit 按鈕／手牌 View 銷毀後仍被互動清理存取；正式修正與回歸測試保留。
- **最近自動化驗證（2026-11-14）**：Unity 編譯成功，完整 PlayMode 40／40，0 fail／skip；清理前後 GameDataValidator.ValidateAll 均 0 error。本輪清理未重跑測試套件；2026-10-15 的 EditMode 853／853 為歷史結果。Render Graph 相容模式 warning 保留。
- **驗收產物清理**：2026-11-14 依使用者指示，確認正式資產無反向引用後，透過 Unity AssetDatabase 刪除 Assets/AcceptanceTests 及對應 .meta；正式 Catalog、牌組、Build Settings 不需修改，也未新增 ignore。
- **延後範圍**：NewCard／NewPartialCard／NewEffect、AI 戰術策略、Invoke 開玩家選取 UI 與傷害預覽不在本次完成範圍。合成所需 PartialCard／效果片段由 T-012 依需求定義，預演歸 T-014。
- **規則來源**：[Target](Target.md#選取與查詢分離)、[Action](Action.md#選取草稿與提交)、[Presenter](Presenter.md#輸入與選取)。下一主線 T-012 待開始。

### 先前階段紀錄

> 下列內容保留原紀錄時間的狀態、驗證、接線與 stash 敘述；當時「待驗收／尚未結案」不代表目前狀態。2026-11-14 的完成與清理結果以上方封存摘要為準。

- **前置**：T-017。
- **目標**：支援卡片依序要求多次不同來源、數量與條件的目標選取。
- **既有基礎**：CardData.SubSelects 已是群組集合；ExistCard 可由 Presenter／AI 依 ID 回傳，GameplayManager 會將合法結果寫入出牌 Context，Effect 可透過群組 ID 讀取選中卡片。
- **本次範圍（2026-10-04 使用者確認）**：先完成 ExistCard 多步驟選取，涵蓋玩家主動出牌、敵方出牌、效果間接出牌與 InvokeCardEffects 四個入口。玩家主動出牌使用 UI；其餘入口自動選取，共用選取規則與結果契約。Invoke 以本次施放者視角建立選取，維持不經正式出牌流程的語意。
- **本次工作**：步驟順序與唯一 ID、候選與數量規則、取消與不足處理、結果驗證及 Effect 消費、UI／場景生命週期，以及各自動選取入口的相容性。AI 先沿用既有隨機選取，不擴充戰術策略。
- **延後範圍**：NewCard／NewPartialCard／NewEffect 仍為預留，本次不實作。NewPartialCard 等 PartialCard 定義與功能就緒；NewEffect 等效果片段與合成契約確定；NewCard 依實際需求另行安排。Invoke 暫停效果並開啟玩家選取 UI 另行規劃。
- **依據**：[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs)、[GameAction](../Assets/Scripts/GameModel/Action/GameAction.cs)、[GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs)。
- **已實作的數量契約**：以 CardSelectionCountRule 的 Optional（可選 0～可達數量）、RequiredAvailable（必須選滿可達數量）、RequiredExact（必須選滿原始需求，候選不足則整次操作不成立）取代 IsMustSelect，不另加 ShortagePolicy。可達數量為需求與合法候選數量的較小值；此規則不等同效果代價的實際支付機制。
- **已實作的取消與配置**：關閉面板取消整次出牌，必選亦可取消；Optional 可選零張確認。本次不提供上一步。零候選且允許空結果時自動前進。SubSelects 清單決定順序，穩定 ID 識別結果；候選沿用 Target／Condition。Presenter 使用 CardSelection.{CardDataId}.{SubSelectionId} 查詢提示，缺少專屬文字時顯示 CardSelection_Default；面板接收已組好的文字與確認狀態，不持有數量規則。
- **已實作的提交驗證**：正式執行前依序重新驗證群組、候選與數量；非法或過期結果拒絕整次操作，不自動修剪。玩家取消／拒絕後依最新模型恢復手牌與結束回合互動；場景取消與戰鬥結束不恢復。執行開始後的目標失效沿既有 Runtime 規則安全略過。
- **已實作的步驟相依**：逐步使用主目標及前面已確認結果計算候選與需求數量，禁止自我及向後引用；選取期間只保存結果，不執行效果。區分未選群組與已確認的空群組，查詢明確接收選取快照，不跨 UI 等待持有共用 Context。純查詢不改變正式狀態或消耗正式亂數，為 T-014 預覽保留相容性；預覽功能仍另行排期。
- **工作階段**：
  1. 規則與詳細設計：使用者已確認完成。
  2. 共用選取契約、逐步驗證、Validator 與資料遷移：2026-10-09 使用者確認 review 完成並已 commit，該步提交為 b377075（cardSelectionQuery）。
  3. 玩家、敵方、間接出牌與 Invoke 接線，完成 UI 取消／關閉及互動恢復：2026-10-15 使用者確認 review 完成，已提交於 b2c4d4b（cardselection flow）。
  4. 四入口驗收、回歸與正式文件更新：2026-10-15 文件與自動化驗證完成，進行中；警告 Popup 接線與實際 UI 驗收沿使用者自行操作 Unity 的分工完成。
- **資產盤點結果**：透過 Unity 檢查 38 個專案 ScriptableObject，找到 20 份 CardData、0 個選取群組，沒有需轉換舊值的資產；移除舊欄位後重新 import 全部卡片資產正常。
- **遷移方式（使用者指定）**：第 2 個工作直接以 CountRule 取代 IsMustSelect，先匯出舊值，再批次更新所有受影響的既有資產；不保留向下相容欄位或雙軌讀取邏輯。
- **本階段完成條件**：ExistCard 多步驟選取順序穩定、結果能依步驟識別取得，四個入口遵循確認後的規則，取消與場景生命週期可正確收斂；其他選取類型保留後續擴充追蹤。
- **目前工作區驗證（2026-10-15）**：第 4 個工作本輪 Unity 編譯與 GameDataValidator.ValidateAll 均 0 error；完整 EditMode 853／853、PlayMode 30／30 通過，0 fail、0 skip。四入口、三種數量規則、順序相依、提交重驗與取消／清理的自動化覆蓋已核對。既有 NoOpCardBuffEffect 測試及 Render Graph 相容模式 warning 保留。
- **第 4 個工作剩餘驗收（2026-10-15）**：Unity 實際讀取的 20 份卡片資產均未配置 SubSelects；已儲存 Prefab／Scene 尚無 WarningPopupPanel 參考。玩家多步驟選卡、警告顯示、提示排版及實際操作尚待接線與驗收，不能以自動化測試代替。警告 Popup／測試卡資產依使用者要求由其自行操作 Unity。
- **正式文件收斂（2026-10-15）**：已更新 Target、Action、GameData、資產製作規範、Card、Effect、GameModel 與 Presenter，數量與候選不足規則集中於 Target；文件連結有效。
- **目前工作**：第 4 個工作「驗收與文件」進行中，確認警告 Popup 接線、實際遊戲 UI 與四入口行為，完成必要回歸及正式系統文件更新。
- **第 3 個工作補正（2026-10-05）**：依使用者決策省略選取期間形態比對保護，移除 Action／草稿的起始 CardData ID；仍依當下卡牌配置完整重驗選取。補正後 Unity 編譯 0 error，完整 EditMode 830／830、定向 PlayMode 4／4 通過。
- **暫存備份**：Git stash 06058ede70418fcf39426f87ba651f790f93bf79「T-011 第 3 步：四入口與 UI 收斂（暫存，保留第 2 步）」仍保留。已抽取第 3 步差異並整合，不覆蓋第 2 步提交，不使用 stash pop 或 --index。
- **第 2 個工作 review 補正（2026-10-05）**：以 CombinedCardCollection 與通用 SetOperationType 取代專用 ExcludedCardCollection，支援差集、聯集、交集；以卡片 Identity 去重並保留穩定順序。Unity 編譯 0 error，完整 EditMode 826／826 通過；第 3 個工作維持暫存。
- **狀態**：🔄 第 1～3 個工作已完成；第 4 個工作進行中，T-011 尚未整體結案。
- **提示識別補正（2026-10-09）**：第 2 步移除 PromptKey 配置與驗證，查詢資訊提供 CardDataId 及群組 Id；第 3 步已完成兩個 ID 的提示查詢與 fallback。同步恢復 XLSX／ExcelDatas 的 6 個共用文字及 Prefab 文字空間；Validator 保留文字表通用檢查，不寫死 UI 必備 key；專屬文字可缺省。
- **第 3 步 review 補正（2026-10-10）**：先前曾以 UniTask 等待模型結果與 Render，後續已依使用者確認改回 GameEvent 通知；目前以操作佇列 Count 阻擋重複輸入，移除 _commandInProgress。出牌完成事件帶 CompletedCardIdentity，僅移除對應指令；結束回合由 PlayerExecuteEndEvent 移除。選取取消／提交前拒絕直接移除並 Restore；操作流程 await 佇列項目離開後再處理下一次輸入。Validator 寫死的六個 UI key 檢查已移除。
- **第 3 步事件管線回歸（2026-10-10）**：移除 OnPlayerActionCompleted、提交結果等待與 Render 完成通知。正式提交只 EnqueueAction，成功由既有 PlayerExecuteStartEvent 恢復；失敗依序送出 CardPlayRejectedEvent 及 PlayerExecuteStartEvent，提示關閉後才處理恢復事件。僅提交前取消／拒絕由 Presenter 主動 Restore。
- **第 3 步四流程整理（2026-10-10）**：RunBattle、ProcessCommands、RenderEvents、UI 於 _Run 同層並聯，共用一次取消及清理管理；移除 _GameplayBattleActions。正常戰鬥結束及任一例外都等待所有流程清理，背景流程提早返回會回報錯誤。
- **第 3 步互動責任補正（2026-10-10）**：移除 CanAcceptPlayerAction，以開始／結束事件控制 View 互動，操作佇列阻擋重複輸入，取消與 BattleResult 限制恢復時機。啟動流程前關閉互動；結束事件完整解除手牌與拖曳訂閱。新增初始停用、結束事件解除訂閱及戰鬥結束不恢復／不接收輸入測試，完整 PlayMode 28／28 通過。
- **第 3 步自動選取迴圈補正（2026-10-10）**：SelectSubTargets 改依 SubSelects 配置步數執行 for；每步仍由 QueryNext 計算相依候選，最後確認 Completed。Unity 編譯 0 error，定向選取整合與呈現測試 27／27 通過。其他選取類型的 Info 與結果契約仍待後續功能接入。
- **第 3 步子選取面板補正（2026-10-10）**：選取改依 StepIndex／StepCount 執行 for，最後重新確認完整結果。恢復 IUniTaskPresenter 與明確的按鈕 Event／EventHandler，詳情以 await 串接，移除流程旗標與輪詢。共用執行器等待期間阻擋底層事件，結束時清除回呼與工作。新增剩餘步驟、取消清理與詳情等待回歸測試，等待影格推進的案例放在 PlayMode。
- **第 3 步職責拆分（2026-10-14）**：RunSubSelection 移至 CardSelectionFlow，該流程持有 SubSelectionPresenter；SubSelectionPresenter.Run 只控制單一步驟面板。文字警告拆成 WarningPopupPresenter／WarningPopupPanel，由 GameplayPresenter 轉譯失敗訊息；移除選卡面板為警告用途增加的 CanToggleVisibility。Prefab 與 Scene 未修改，使用者自行建立警告面板、設定 _panel／_messageText／_closeButtons，並接到 GameplayView._warningPopupPanel；此接線及實際畫面驗收仍待處理。
- **第 3 步警告按鈕補正（2026-10-14）**：WarningPopupPanel 移除 Awake 與 callback 綁定；WarningPopupPresenter.Run 依既有 UI Presenter 慣例，使用 _SetupSubscriptions 訂閱 CloseButtons、送出 CloseEvent，並在 Run 結束時解除訂閱及關閉面板。Inspector 接線欄位不變。
- **第 3 步面板啟動補正（2026-10-14）**：移除 IUniTaskPresenter.Run 的 onStarted 參數，選取與警告 Presenter 恢復 Open → Run → finally Close；測試在 Run 啟動後模擬輸入，EditMode 的 PlayerLoop 推進由測試端管理。保留取消、詳情等待及訂閱清理，未改動 Prefab／Scene。
- **第 3 步固定按鈕接線補正（2026-10-15）**：選卡面板提供 CloseButtons／ConfirmButton／VisibleToggleButton，由 SubSelectionPresenter._SetupSubscriptions 建立及管理點擊訂閱。移除 Property 固定按鈕 callback 與 Panel.Init 轉接訂閱，動態卡片點擊／長按仍由 SelectionProperty 配置。測試改用實際 Button，新增重開與關閉後的收合行為驗證；Inspector 序列化欄位不變，未修改 Prefab／Scene。
- **第 3 步共用步驟契約補正（2026-10-10）**：NextStep 改使用 SubSelectionStepInfo，ExistCardSelectionInfo 繼承共用步驟資訊。Draft 保存 ISubSelectionAction，ExistCard 結果保留不可變快照；玩家與自動選取透過型別分派，共用迴圈不直接建立 ExistCard 結果。提交驗證依 Info／Action 型別分派；其他選取類型仍未實作。
- **第 3 步重新檢查與優化（2026-10-09）**：自動選取只維護 Draft，全部完成後才轉為 Actions；本步取消以 Option 區分空選取；移除 Panel 不使用的 CountRule／MaxSelectCount 及正式 Presenter 的測試專用建構子。擴大詳情面板 finally 清理範圍。新增測試找出並修正回合開始事件延後顯示時提早解除操作鎖的問題；補提示 fallback、後續步取消重試、詳情期間阻擋回呼及面板開啟失敗清理測試。

## T-012：卡片合成系統（已完成）

- **狀態**：✅ 2026-11-22 使用者確認本輪測試成功並授權清理及暫存正式內容，五項工作全部結案。
- **完成範圍**：第一輪選完整模板，後續輪次追加 Effects／TriggeredEffects；依模板切池、資產明填 OfferCount、排除已選選項、候選不足提供剩餘項目、零候選拒絕整次選取。採動態定義，不使用固定配方表或預建成品資產。
- **執行整合**：ICardDefinition 區分 Catalog 與動態內容，CardSelectionSession 管理候選生命週期；玩家 UI、敵方、PlayCardEffect 與 InvokeCardEffects 共用選取規則，建立成品、Clone 與形態還原保留獨立組成。描述片段直接參考效果數值。
- **驗收修正**：修正無擁有者模板預覽缺少費用／威力、提示列刷新累積、敵方新增手牌提前建立預選 View 造成下一回合重複 Identity，以及 Odin 共用數值的 Inspector 參考路徑警告；保留對應正式修正與回歸測試。
- **人工與 Play 驗收**：使用者確認本輪成功。隔離資產涵蓋三輪合成、零候選、Clone、Invoke、間接出牌、暫時形態及雙卡合成；AI 另驗證連續三次結束回合、雙卡流程中途取消不扣費／不產卡、兩張成品獨立建立及出牌效果。
- **清理與最終驗證（2026-11-22）**：確認外部資產無驗收引用後，透過 Unity AssetDatabase 刪除 Assets/Acceptance 及全部 .meta，切回正式 Gameplay 場景。清理後 Unity 編譯與正式 GameDataValidator 0 error，完整 EditMode 912／912、PlayMode 45／45，皆 0 fail／skip；正式 Catalog、牌組、場景與 Build Settings 無需回填測試引用。
- **既有警告**：GameplayManager 兩個未使用事件 CS0067、Render Graph 相容模式，以及 NoOpCardBuffEffect 測試空效果警告仍保留。
- **邊界**：組成保存限本場戰鬥，不含跨戰鬥持久化或完整預演。Play／Invoke 的合成子選取目前採自動選擇；排除已選選項以整次選取的 Option Id 為範圍，多組模板可使用不同選項 Id 區隔。
- **規則來源**：[Card](Card.md)、[GameData](GameData.md)、[Target](Target.md)、[Effect](Effect.md)、[資產製作規範](GameData_Asset_Guidelines.md)。下一項工作由使用者另行安排。
