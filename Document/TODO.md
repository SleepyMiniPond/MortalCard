# 專案待辦事項

> 最後更新：2026-09-23
> 狀態標記：⬜ 待開始／待排期 | 🟡 已有部分基礎，尚未完成 | 🔄 進行中 | ✅ 已完成
> T-022 已完成並封存；下一主線為 T-023。
> 已完成任務與驗證紀錄請查看 [TODO_Archive.md](TODO_Archive.md)。

## 工作優先順序

```text
現在可開始
└─ T-023 InvokeCardEffects 原地執行卡效
        ↓
    T-011 多步驟目標選取
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

T-010、T-017～T-020 與 T-022 已完成並封存。T-017 範圍內的卡片生命週期觸發已完成（FormChanged 專用路徑差異另列 T-025）；下一主線為 T-023 `InvokeCardEffects` 原地執行卡效。T-011 與 T-012 依主線順序排在其後；T-013、T-014 影響面較廣，不與這條主線同時進行。

---

## 主線依序完成

### T-023：完成 `InvokeCardEffects` 原地執行卡效能力

- **前置**：T-022 的共用自動選取入口；不依賴完整間接出牌 Queue。
- **目標**：執行位於牌堆／墓地的指定卡片普通 Effects，但不把卡片視為打出。
- **已確認契約**：卡片留在原區域；不進入 `PlayingCard`；不支付能量；不受 `Sealed` 限制；不套用 `EffectRepeat`；只執行一次普通 Effects；不產生 `UsedCardEvent`，也不派送 `Played`／`EffectPlayed`。
- **自動選取**：無玩家選取階段，沿用 T-022 的主目標與 SubSelection 自動選取能力；`ToAlly`／`ToEnemy` 同樣以卡片擁有者為視角。
- **Queue 規則**：直接進入既有 Effect Queue，連鎖 `InvokeCardEffects` 共用同一 Queue Budget。
- **命名**：程式與技術文件使用 `InvokeCardEffects`；題材化名稱只留給未來翻譯與顯示文字。
- **程式核對**：尚無 InvokeCardEffects 資料型別、Resolver 或正式執行入口；一般 CardEffect Queue 不等於已有此操作。
- **狀態**：⬜ 前置已完成，下一主線待開始

### T-011：多步驟自訂目標選取

- **前置**：T-017。
- **目標**：支援卡片依序要求多次不同來源、數量與條件的目標選取。
- **既有基礎**：CardData.SubSelects 已是群組集合；ExistCard 可由 Presenter／AI 依 ID 回傳，GameplayManager 會將合法結果寫入出牌 Context，Effect 可透過群組 ID 讀取選中卡片。
- **剩餘缺口**：NewCard／NewPartialCard／NewEffect 仍是預留；還需定義完整多步驟順序、取消語意與其他選取類型的候選及結果契約。
- **依據**：[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs)、[GameAction](../Assets/Scripts/GameModel/Action/GameAction.cs)、[GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs)。
- **開始前需決定**：
  - 每一步的識別方式、來源區域、數量、篩選條件與提示文字。
  - 玩家取消、中途無合法目標及選取不足時的處理方式。
  - 各步驟結果如何交給 Action 與 Effect 管線。
- **建議階段**：
  1. 釐清既有群組集合的順序及識別契約，補足其他選取來源。
  2. 沿用 Presenter 群組流程，補足結果傳遞至 Model／Effect 的消費端。
  3. 補 UI 取消／關閉流程與 EditMode 測試。
- **完成條件**：多步驟選取順序穩定、結果能依步驟識別取得，取消與場景生命週期可正確收斂。
- **狀態**：🟡 已有選取基礎；完整功能待開始

---

## 前置完成後開始

### T-012：卡片合成系統（自訂藥水）

- **目標**：讓玩家透過多輪選擇效果片段，組合或轉換成新的卡片結果。
- **前置**：T-010 卡片變身、T-011 多步驟目標選取。
- **開始前需決定**：採用動態效果組合，或使用預先定義的 CardData 組合表。
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
- **現況**：目前主動出牌只受能量與 `Sealed` 限制；主目標與子選取不足採盡量執行，不作為出牌門檻。T-022 的 `EffectPlayed` 沿用相同規則。
- **工作**：定義可序列化的出牌前置條件、失敗提示、玩家／敵人／間接出牌是否共用，以及條件應在哪個 Context 與時點評估。
- **完成條件**：企劃可在 CardData 配置必要條件；所有正式出牌入口使用同一合法性規則，失敗時不支付費用、不移入 `PlayingCard`、不產生出牌事件。
- **狀態**：⬜ 未來待排期。

### T-029：GameplayManager 職責拆分

- **範圍**：GameModel 內部重構；T-022 完成後另行評估排期，不插入目前主線。
- **現況**：GameplayManager 同時承擔回合、出牌、效果根入口、事件與勝負收斂，閱讀及修改成本逐漸增加。
- **工作**：優先評估抽離單張出牌流程與出牌鏈協調，釐清查詢／執行介面及事件擁有權；入口保留可直接閱讀的具名流程，避免只為消除少量重複而引入委派抽象。
- **完成條件**：責任與依賴清楚，既有 FIFO、Budget、選取作用域、事件順序與中止清理測試全部通過，遊戲行為不變。
- **狀態**：⬜ T-022 已完成，待獨立評估排期；本次只記錄，不實作。

## 未來可能方向（非待辦）

- Effect Queue 的效果取消／替代。
- 戰鬥重播與 AI Simulation。
- GameData 與 GameModel 的完整 Content Spec → Runtime Compiler 程序集拆分；T-018 只先統一內容目錄與驗證來源，不在同一任務內擴張為資料層重寫。

這些項目目前不排入工作順序；等實際需求或風險出現後，再建立新的 T 編號。
