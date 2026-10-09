# 專案待辦事項

> 最後更新：2026-10-09
> 狀態標記：⬜ 待開始／待排期 | 🟡 已有部分基礎，尚未完成 | 🔄 進行中 | ✅ 已完成
> T-023 已確認完成並封存；目前主線為 T-011，第 2 個工作 review 中，第 3 個工作已保存至 stash、未套用於工作區。
> 已完成任務與驗證紀錄請查看 [TODO_Archive.md](TODO_Archive.md)。

## 工作優先順序

```text
目前主線（第 2 個工作 review 中，第 3 個工作已暫存）
└─ T-011 多步驟目標選取
        ↓
    T-012 卡片合成

獨立排期
├─ T-021 初始抽牌優先（已更名，排序未接線）
├─ T-024 場景結果、重試與戰後資料銜接
├─ T-025 FormChanged 生命週期一致化
├─ T-026 Buff 資源上限接線
├─ T-027 減少好感度事件呈現
├─ T-028 可配置的卡片出牌條件
├─ T-029 GameplayManager 職責拆分
├─ T-013 敵人動態增減
└─ T-014 Preview / Simulation
```

T-010、T-017～T-020 與 T-022～T-023 已完成並封存。T-017 範圍內的卡片生命週期觸發已完成（FormChanged 專用路徑差異另列 T-025）；下一主線為 T-011 多步驟目標選取，再接 T-012。T-013、T-014 影響面較廣，不與這條主線同時進行。

---

## 主線依序完成

### T-011：多步驟自訂目標選取

- **前置**：T-017。
- **目標**：支援卡片依序要求多次不同來源、數量與條件的目標選取。
- **既有基礎**：CardData.SubSelects 已是群組集合；ExistCard 可由 Presenter／AI 依 ID 回傳，GameplayManager 會將合法結果寫入出牌 Context，Effect 可透過群組 ID 讀取選中卡片。
- **本次範圍（2026-10-04 使用者確認）**：先完成 ExistCard 多步驟選取，涵蓋玩家主動出牌、敵方出牌、效果間接出牌與 InvokeCardEffects 四個入口。玩家主動出牌使用 UI；其餘入口自動選取，共用選取規則與結果契約。Invoke 以本次施放者視角建立選取，維持不經正式出牌流程的語意。
- **本次工作**：步驟順序與唯一 ID、候選與數量規則、取消與不足處理、結果驗證及 Effect 消費、UI／場景生命週期，以及各自動選取入口的相容性。AI 先沿用既有隨機選取，不擴充戰術策略。
- **延後範圍**：NewCard／NewPartialCard／NewEffect 仍為預留，本次不實作。NewPartialCard 等 PartialCard 定義與功能就緒；NewEffect 等效果片段與合成契約確定；NewCard 依實際需求另行安排。Invoke 暫停效果並開啟玩家選取 UI 另行規劃。
- **依據**：[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs)、[GameAction](../Assets/Scripts/GameModel/Action/GameAction.cs)、[GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs)。
- **已實作的數量契約**：以 CardSelectionCountRule 的 Optional（可選 0～可達數量）、RequiredAvailable（必須選滿可達數量）、RequiredExact（必須選滿原始需求，候選不足則整次操作不成立）取代 IsMustSelect，不另加 ShortagePolicy。可達數量為需求與合法候選數量的較小值；此規則不等同效果代價的實際支付機制。
- **已確認的取消與配置**：關閉面板取消整次出牌，必選亦可取消；Optional 可選零張確認。本次不提供上一步。零候選且允許空結果時自動前進。SubSelects 清單決定順序，穩定 ID 識別結果；候選沿用 Target／Condition。提示使用 CardSelection.{CardDataId}.{SubSelectionId}，缺少專屬文字時顯示通用提示，移除獨立 PromptKey。UI 實作目前保存在第 3 個工作的 stash，恢復時同步更新兩個 ID 的接線。
- **已實作的提交驗證**：正式執行前依序重新驗證群組、候選與數量；非法或過期結果拒絕整次操作，不自動修剪。玩家取消／拒絕後恢復合法互動的接線目前保存在第 3 個工作的 stash；執行開始後的目標失效沿既有 Runtime 規則安全略過。
- **已實作的步驟相依**：逐步使用主目標及前面已確認結果計算候選與需求數量，禁止自我及向後引用；選取期間只保存結果，不執行效果。區分未選群組與已確認的空群組，查詢明確接收選取快照，不跨 UI 等待持有共用 Context。純查詢不改變正式狀態或消耗正式亂數，為 T-014 預覽保留相容性；預覽功能仍另行排期。
- **工作階段**：
  1. 規則與詳細設計：使用者已確認完成。
  2. 共用選取契約、逐步驗證、Validator 與資料遷移：review 中（待使用者確認），保留於工作區。
  3. 玩家、敵方、間接出牌與 Invoke 接線，完成 UI 取消／關閉及互動恢復：2026-10-05 依使用者要求保存至 stash，未套用於工作區；等待指示恢復。
  4. 四入口驗收、回歸與正式文件更新。
- **資產盤點結果**：透過 Unity 檢查 38 個專案 ScriptableObject，找到 20 份 CardData、0 個選取群組，沒有需轉換舊值的資產；移除舊欄位後重新 import 全部卡片資產正常。
- **遷移方式（使用者指定）**：第 2 個工作直接以 CountRule 取代 IsMustSelect，先匯出舊值，再批次更新所有受影響的既有資產；不保留向下相容欄位或雙軌讀取邏輯。
- **本階段完成條件**：ExistCard 多步驟選取順序穩定、結果能依步驟識別取得，四個入口遵循確認後的規則，取消與場景生命週期可正確收斂；其他選取類型保留後續擴充追蹤。
- **目前工作區驗證（2026-10-05）**：第 3 個工作退回後，Unity 編譯 0 error，完整 EditMode 815／815 通過。暫存前的第 3 個工作曾通過完整 EditMode 830／830、PlayMode 15／15；該紀錄屬 stash 版本。既有 warning 包含 OneTurnStart／OnTurnEnd 未使用事件、NoOpCardBuffEffect 測試及 Render Graph 相容模式。
- **下一步**：先 review 第 2 個工作，再依使用者指示恢復第 3 個工作的四入口與 UI 接線；第 4 個工作待開始。
- **第 3 個工作補正（2026-10-05）**：依使用者決策省略選取期間形態比對保護，移除 Action／草稿的起始 CardData ID；仍依當下卡牌配置完整重驗選取。補正後 Unity 編譯 0 error，完整 EditMode 830／830、定向 PlayMode 4／4 通過。
- **暫存位置**：Git stash 06058ede70418fcf39426f87ba651f790f93bf79「T-011 第 3 步：四入口與 UI 收斂（暫存，保留第 2 步）」；保留既有 staged 內容與使用者的 Attack_Special.asset 修改。
- **第 2 個工作 review 補正（2026-10-05）**：以 CombinedCardCollection 與通用 SetOperationType 取代專用 ExcludedCardCollection，支援差集、聯集、交集；以卡片 Identity 去重並保留穩定順序。Unity 編譯 0 error，完整 EditMode 826／826 通過；第 3 個工作維持暫存。
- **狀態**：🟡 第 2 個工作 review 中；第 3 個工作已暫存、待恢復；第 4 個工作待開始。
- **提示識別補正（2026-10-09）**：已移除 PromptKey 配置與驗證，查詢資訊提供 CardDataId 及群組 Id；專屬提示可缺省。Unity 編譯 0 error，完整 EditMode 826／826 通過。第 3 個工作恢復時再依兩個 ID 組合 key 並接上通用提示。

---

## 前置完成後開始

### T-012：卡片合成系統（自訂藥水）

- **目標**：讓玩家透過多輪選擇效果片段，組合或轉換成新的卡片結果。
- **前置**：T-010 卡片變身、T-011 多步驟目標選取。
- **開始前需決定**：採用動態效果組合，或使用預先定義的 CardData 組合表。
- **選取前置邊界**：T-011 本階段只完成 ExistCard 與共用多步驟流程；合成若需要 PartialCard／效果片段，仍需先定義相關功能，再接入 NewPartialCard／NewEffect，不能視為 T-011 本階段已提供。
- **建議階段**：先完成單一固定配方的垂直切片，再擴充多種片段與組合規則。
- **完成條件**：合成選取、結果建立、狀態保存與畫面更新形成完整流程。
- **狀態**：⬜ 未開始

---

## 長期／獨立排期

### T-021：完成 `CardProperty.InitialPriority` 初始抽牌優先

- **排期**：T-017 已完成，本項獨立排期，不插入 T-022 → T-023 主線。
- **目標**：讓 `InitialPriorityPropertyData` 對應的 `CardProperty.InitialPriority` 真正影響戰鬥初始抽牌順序。
- **現況**：目前只有 `InitialPriorityPropertyData`、`InitialPriorityPropertyEntity` 與查詢／轉換測試；正式牌堆建立與抽牌流程尚未讀取此屬性來排序。
- **開始前需決定**：優先卡影響第一手或整個牌堆、同優先級的排序規則、洗牌與可重現亂數的互動，以及 `BeforeGameStart`／`CardTriggeredTiming.Initialize` 新增或修改優先卡時是否影響本場戰鬥。
- **已完成範圍**：舊名稱已改為 CardProperty.InitialPriority，保留序列化數值；剩餘工作是排序、抽牌行為及驗證。
- **依據**：[CardPropertyEntityFactory](../Assets/Scripts/GameModel/Factory/CardPropertyEntityFactory.cs)、[GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs)、[DeckEntity](../Assets/Scripts/GameModel/Entity/Card/DeckEntity.cs)。
- **狀態**：🟡 更名及查詢已完成；排序功能待規劃。

### T-013：戰鬥中敵人動態增減

- **目標**：支援戰鬥中新增敵人、逃跑或移除非死亡敵人。
- **主要影響**：角色集合、目標解析、勝負判定、EnemyLogic、CharacterView 建立與動畫生命週期。
- **關鍵方向**：將單一 CharacterView 管理改為依角色 Identity 管理的動態集合，並使用 Factory／物件池建立及回收 View。
- **完成條件**：角色增減不破壞選取、勝負判定與動畫佇列，戰鬥結束可完整清理所有角色資源。
- **狀態**：⬜ 未開始

### T-014：Preview / Simulation 預演管線

- **目標**：在不修改正式戰鬥狀態的情況下，預覽卡片效果、目標與結果。
- **主要方向**：區分 `Preview`、`Simulation`、`Execution` 三種用途。
- **已提出的使用需求（2026-10-04）**：手牌顯示加成後數字，指向敵人時顯示預測傷害。T-011 設計需保留部分選取與完整選取的區別，以及不污染正式狀態的查詢方式；預覽功能仍歸本項，未納入 T-011 實作範圍。
- **建議階段**：先做 Resolver 層的輕量 Preview；完整 Simulation sandbox 等 AI 或除錯需求明確後再設計。
- **既有基礎**：T-001 Resolver／Handler、T-003 Effect Queue、T-006 決定性亂數；CardInfo.CreatePreview 只提供卡牌威力預覽，尚非效果結果預演或獨立狀態沙盒。
- **完成條件**：Preview 不污染正式狀態，且相同輸入能產生穩定、可供 UI 使用的預演資訊。
- **狀態**：⬜ 未開始

## 本輪核對新增的工作（待排期）

以下是程式現況與文件宣稱不符的缺口；本輪僅記錄，未修改程式。它們不自動改變既定主線優先順序。

### T-024：場景結果、重試與戰後資料銜接

- **範圍**：Scene／Presenter／戰鬥外狀態；獨立排期，先完成結果流程，再接續戰後資料套用。
- **現況**：Win Presenter 無正常完成入口；Main 的 retry 未逐次重設、結果分支與回地圖語意未完整，Retry 也會重建種子與戰鬥設定。Main 尚未消費勝利 CardInstanceChangeSet。
- **工作**：定義 Win／Retry／Restart／Quit 的狀態轉移、保存同關重試設定、提供勝利完成操作，再定義並套用戰後 Domain 狀態。磁碟存檔另行規劃。
- **完成條件**：各結果能離開目前流程；Retry 後再 Quit 不再重試；同關設定可重現；只有應提交的結果寫回，失敗／取消不提交。
- **依據**：[Main](../Assets/Scripts/Scene/Main.cs)、[GameResultWinPresenter](../Assets/Scripts/Presenter/Gameplay/GameResultWinPresenter.cs)、[BattleBuilder](../Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs)、[Instance](Instance.md)。
- **狀態**：⬜ 待排期。

### T-025：FormChanged 生命週期一致化

- **範圍**：卡片形態與 CardTriggeredTiming；獨立於已完成的 T-017，安排於下一次形態／生命週期整合時。
- **現況**：Self Apply／Revert、Override 解除會執行 CardData FormChanged；Override 套用只產生形態事件，不執行該效果。上述路徑皆未派送 CardBuff FormChanged。
- **工作**：確認各種形態操作的觸發契約，再統一派送、Context、快照與 Queue Budget。
- **完成條件**：各入口的事件與效果順序有明確規則，CardData／有效 CardBuff 的支援範圍一致且有行為測試。
- **依據**：[CardFormQueueItems](../Assets/Scripts/GameModel/Effect/CardFormQueueItems.cs)、[Override Handler](../Assets/Scripts/GameModel/Effect/Handlers/ApplyCardFormOverrideEffectCommandHandler.cs)。
- **狀態**：⬜ 待契約確認與排期。

### T-026：Buff 資源上限接線

- **範圍**：PlayerBuff／CharacterBuff 與資源 Manager；獨立排期。
- **現況**：MaxHealth／MaxEnergy 有資料或 Entity 定義，但目前資源上限由 Manager 建構值提供，沒有消費 Buff 上限屬性的動態接線。
- **工作**：先確認加成與移除時的上限／目前值／護盾收斂規則，再完成公式、狀態及事件更新。
- **完成條件**：新增、變更與移除 Buff 確實影響有效上限，且不破壞資源不變量。
- **依據**：[CharacterEntity](../Assets/Scripts/GameModel/Entity/Character/CharacterEntity.cs)、[PlayerEntity](../Assets/Scripts/GameModel/Entity/Player/PlayerEntity.cs)、[GameFormula](../Assets/Scripts/GameModel/GameFormula.cs)。
- **狀態**：⬜ 待規則確認與排期。

### T-027：減少好感度事件呈現

- **範圍**：GameView；獨立小型修正，不併入 T-022。
- **現況**：GameplayView 已有減少好感度的處理方法，但 Render 缺少 DecreaseDispositionEvent 分支，事件無法即時更新該畫面與動畫。
- **完成條件**：減少好感度事件更新 ViewModel 並交由角色呈現，驗證與增加事件一致的狀態同步。
- **依據**：[GameplayView](../Assets/Scripts/GameView/GameplayView.cs)。
- **狀態**：⬜ 待排期。

### T-028：可配置的卡片出牌條件

- **範圍**：CardData／出牌合法性；獨立於 T-022，待實際卡牌需求出現後排期。
- **現況**：已有手牌、`Sealed`、主動出牌費用與選取有效性檢查；自動選取遇到必要主目標無候選時略過，ExistCard 子選取不足則取可達數量。尚無可配置的一般出牌前置條件。
- **工作**：定義可序列化的出牌前置條件、失敗提示、玩家／敵人／間接出牌是否共用，以及條件應在哪個 Context 與時點評估。
- **完成條件**：企劃可在 CardData 配置必要條件；所有正式出牌入口使用同一合法性規則，失敗時不支付費用、不移入 `PlayingCard`、不產生出牌事件。
- **狀態**：⬜ 未來待排期。

### T-029：GameplayManager 職責拆分

- **範圍**：GameModel 內部重構；T-022 完成後另行評估排期，不插入目前主線。
- **現況**：單張卡的驗證、付費、效果與離場已抽至 [CardPlayExecutor](../Assets/Scripts/GameModel/Effect/CardPlayExecutor.cs)，並區分出牌準備、重複效果與中止同步。GameplayManager 保留回合、選取作用域、出牌鏈、事件交付與勝負檢查；責任邊界見 [GameModel](GameModel.md#單張出牌與出牌鏈)。
- **剩餘工作**：出牌鏈協調是否再拆，留待後續重構評估；維持可直接閱讀的具名流程，避免只為消除少量重複而引入委派抽象。
- **完成條件**：責任與依賴清楚，既有 FIFO、Budget、選取作用域、事件順序與中止清理測試全部通過，遊戲行為不變。
- **狀態**：🟡 單張出牌流程已抽離、通過回歸並經使用者確認；其餘拆分另行評估，不插入 T-011 主線。

## 未來可能方向（非待辦）

- Effect Queue 的效果取消／替代。
- 戰鬥重播與 AI Simulation。
- GameData 與 GameModel 的完整 Content Spec → Runtime Compiler 程序集拆分；T-018 只先統一內容目錄與驗證來源，不在同一任務內擴張為資料層重寫。

這些項目目前不排入工作順序；等實際需求或風險出現後，再建立新的 T 編號。
