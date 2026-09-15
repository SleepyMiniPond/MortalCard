# 專案待辦事項

> 最後更新：2026-09-16
> 狀態標記：⬜ 未開始 | 🔄 進行中 | ✅ 已完成
> 已完成任務與驗證紀錄請查看 [TODO_Archive.md](TODO_Archive.md)。

## 工作優先順序

```text
現在可開始
└─ T-017 CardTriggeredTiming 生命週期觸發管線
        ↓
    T-022 EffectPlayed 間接出牌
        ↓
    T-023 InvokeCardEffects 原地執行卡效
        ↓
    T-011 多步驟目標選取
        ↓
    T-012 卡片合成

獨立排期
├─ T-013 敵人動態增減
└─ T-014 Preview / Simulation
```

T-010、T-018、T-019 與 T-020 已完成並封存。通用資料表達與 Reaction Effect 執行能力已具備，下一步補齊卡片生命週期觸發管線，讓新增卡片能以資料資產完成，而不是持續為單一卡片增加專用程式。T-011 與 T-012 延後至此項完成後；T-013、T-014 影響面較廣，不與這條主線同時進行。

---

## 現在可開始

## 主線依序完成

### T-017：完成 CardTriggeredTiming 生命週期觸發管線

- **前置**：T-018、T-019、T-020 已完成。
- **目標**：讓 `CardData.TriggeredEffects` 與 `CardBuffData.Effects` 能在抽牌、打出、保留、丟棄、初始化等卡片生命週期中，依明確且唯一的時機進入 Effect Queue。
- **現況**：
  - `CardTriggeredTiming.FormChanged` 已由 T-010 階段 4 接入；目前形態 Queue 直接執行新 Effective Form 的 CardData Effects，並在形態狀態與最新 `CardInfo` 提交後進行。
  - T-017 工作包 2 已接入 `Initialize`；工作包 3 已接入由系統抽牌根源造成的 `Drawed`；工作包 4 已接入非系統抽牌根源造成的 `EffectDrawed`，並確認每張卡實際完成 `Deck → HandCard` 後逐張派送。
  - `Played`、`Preserved`、`Discarded`、`EffectDiscarded` 仍待後續工作包接入；`EffectPlayed` 契約已確認，完整間接出牌 Runtime 拆至 T-022。
  - `CardTriggeredEffectDispatch.CreateItems()` 已由 `Initialize`、`Drawed` 與 `EffectDrawed` 正式 Runtime Queue 呼叫，可快照 CardData 與目前有效 CardBuff，並固定依 CardData → CardBuff 順序執行；既有 FormChanged 直接派送仍維持原語意。
  - `CardData.TriggeredEffects` 與 `CardBuffData.Effects` 共用 `CardTriggeredTiming`，實作時必須同時處理卡片本體與目前有效的 CardBuff，避免兩套生命週期語意分離。
- **已確認契約與後續待決定事項**：
  - `Drawed` 是系統根源抽牌，`EffectDrawed` 是非系統根源抽牌；系統抽牌觸發的連鎖抽牌仍屬 `Drawed`，且只有 `Deck → HandCard` 算抽牌。
  - CardData Effect → CardBuff Effect 的順序、派送快照、Selected Card Context 與同一 EffectQueueRunner Budget 規則已由工作包 1～3 確認。
  - `Played`／`EffectPlayed` 的語意已確認；後續仍需逐包決定 `Preserved`、`Discarded`／`EffectDiscarded` 與各自狀態、Gameplay Event、畫面更新的相對順序。
- **建議階段**：拆成 8 個小工作包，先完成共用觸發契約與既有資產 migration，再依序完成 Initialize、一般抽牌、Effect 抽牌、現有主動出牌 `Played`、Preserved、Discarded／EffectDiscarded，最後做完整驗收與文件收斂；`EffectPlayed` 的新間接出牌能力另由 T-022 實作。
- **完成條件**：現有正式生命週期流程皆有明確且可測試的 Runtime 入口；CardData 與有效 CardBuff 依固定順序在同一 Queue Scope 執行，且一般流程與 Effect 造成的流程不會混用或重複觸發。尚無正式操作來源的 `EffectPlayed` 以已確認契約及 T-022 追蹤，不在 T-017 製造假入口。
- **狀態**：🔄 工作包 5 步驟 2 已補齊 CardData／CardBuff 的 `EffectRepeat` 資料到 Runtime 垂直切片並修正執行次數，完整 EditMode 628／628 passed，目前待使用者確認；下一步為步驟 3 接入 `Played`

### T-022：完成 `EffectPlayed` 間接出牌能力

- **前置**：T-017 工作包 5 `Played`，建議於 T-017 完整驗收後開始。
- **目標**：讓 Effect 可將擁有者手牌中的卡片排入完整間接出牌流程，執行普通 Effects 與 `CardTriggeredTiming.EffectPlayed`。
- **已確認契約**：只允許手牌；不支付能量；受 `Sealed` 限制；經過 `PlayingCard`；套用普通 Effects 的完整 `EffectRepeat`，但 `EffectPlayed` 每次出牌只派送一次；共用主動出牌的墓地／排除／回收規則。
- **自動選取**：無玩家選取階段；共用 `SelectMainTarget` 與 `SelectSubTargets`，並抽出顯式「選取視角玩家」。`TargetLogicTag.ToAlly`／`ToEnemy` 以卡片擁有者為視角，不改寫代表目前行動流程的 `GameStatus.CurrentPlayer`。
- **執行模型**：同時只允許一個完整出牌流程；目前出牌中產生的間接出牌請求以 FIFO 延後到目前卡片完成離場後執行，整條出牌鏈需有共同 Budget 防止循環。
- **事件**：為 `UsedCardEvent` 增加出牌原因，區分主動出牌與 Effect 間接出牌。
- **失效規則**：卡片不在手牌、已被 `Sealed` 或無法建立合法自動目標時，不移入 `PlayingCard`、不產生出牌事件、不派送 `EffectPlayed`。
- **狀態**：⬜ 待 T-017 完成後開始

### T-023：完成 `InvokeCardEffects` 原地執行卡效能力

- **前置**：T-022 的共用自動選取入口；不依賴完整間接出牌 Queue。
- **目標**：執行位於牌堆／墓地的指定卡片普通 Effects，但不把卡片視為打出。
- **已確認契約**：卡片留在原區域；不進入 `PlayingCard`；不支付能量；不受 `Sealed` 限制；不套用 `EffectRepeat`；只執行一次普通 Effects；不產生 `UsedCardEvent`，也不派送 `Played`／`EffectPlayed`。
- **自動選取**：無玩家選取階段，沿用 T-022 的主目標與 SubSelection 自動選取能力；`ToAlly`／`ToEnemy` 同樣以卡片擁有者為視角。
- **Queue 規則**：直接進入既有 Effect Queue，連鎖 `InvokeCardEffects` 共用同一 Queue Budget。
- **命名**：程式與技術文件使用 `InvokeCardEffects`；題材化名稱只留給未來翻譯與顯示文字。
- **狀態**：⬜ 待 T-022 共用選取入口完成

### T-021：完成 `CardProperty.InitialPriority` 初始抽牌優先

- **前置／插入順序**：建議於 T-017 完成後排入；與 T-017 的 `CardTriggeredTiming.Initialize` 分開處理。
- **目標**：讓 `InitialPriorityPropertyData` 對應的 `CardProperty.InitialPriority` 真正影響戰鬥初始抽牌順序。
- **現況**：目前只有 `InitialPriorityPropertyData`、`InitialPriorityPropertyEntity` 與查詢／轉換測試；正式牌堆建立與抽牌流程尚未讀取此屬性來排序。
- **開始前需決定**：優先卡影響第一手或整個牌堆、同優先級的排序規則、洗牌與可重現亂數的互動，以及 `BeforeGameStart`／`CardTriggeredTiming.Initialize` 新增或修改優先卡時是否影響本場戰鬥。
- **範圍**：先將現有錯名 `CardProperty.Initialize` 更名為 `CardProperty.InitialPriority`（本輪已完成，保留底層數值 `1 << 4`）；再實作牌堆排序與抽牌行為，並補足 EditMode／整合測試。
- **狀態**：✅ 改名子步驟已完成；初始抽牌排序功能仍待規劃，本項目前不併入 T-017 工作包 2。

### T-011：多步驟自訂目標選取

- **前置**：T-017。
- **目標**：支援卡片依序要求多次不同來源、數量與條件的目標選取。
- **開始前需決定**：
  - 每一步的識別方式、來源區域、數量、篩選條件與提示文字。
  - 玩家取消、中途無合法目標及選取不足時的處理方式。
  - 各步驟結果如何交給 Action 與 Effect 管線。
- **建議階段**：
  1. 將單次 SubSelection 擴展為有序步驟資料。
  2. 讓 Presenter 依序執行並保存各步驟結果。
  3. 補 UI 取消／關閉流程與 EditMode 測試。
- **完成條件**：多步驟選取順序穩定、結果能依步驟識別取得，取消與場景生命週期可正確收斂。
- **狀態**：⬜ 未開始

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
- **既有基礎**：T-001 Resolver／Handler、T-003 Effect Queue、T-006 決定性亂數。
- **完成條件**：Preview 不污染正式狀態，且相同輸入能產生穩定、可供 UI 使用的預演資訊。
- **狀態**：⬜ 未開始

## 未來可能方向（非待辦）

- Effect Queue 的效果取消／替代。
- 戰鬥重播與 AI Simulation。
- GameData 與 GameModel 的完整 Content Spec → Runtime Compiler 程序集拆分；T-018 只先統一內容目錄與驗證來源，不在同一任務內擴張為資料層重寫。

這些項目目前不排入工作順序；等實際需求或風險出現後，再建立新的 T 編號。
