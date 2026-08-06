# 專案待辦事項

> 最後更新：2026-08-05
> 狀態標記：⬜ 未開始 | 🔄 進行中 | ✅ 已完成
> 已完成任務與驗證紀錄請查看 [TODO_Archive.md](TODO_Archive.md)。

## 工作優先順序

```text
現在可開始
├─ T-010 卡片變身
└─ T-011 多步驟目標選取

T-010 + T-011 完成
        ↓
T-012 卡片合成

獨立排期
├─ T-013 敵人動態增減
├─ T-014 Preview / Simulation
└─ T-017 CardTriggeredTiming 觸發管線
```

T-010 與 T-011 互相獨立，應依近期卡牌需求選擇其中一項開始。T-012 同時依賴兩者；T-013、T-014 影響面較廣，不與其他大型功能同時進行。T-017 可獨立處理，但開始前需先定義各卡片生命週期時機的精確順序。

---

## 現在可開始

### T-010：卡片變身（保留狀態）

- **目標**：讓戰鬥中的卡片切換成另一張 CardData，同時保留指定的執行期狀態。
- **現況分析**：[CardTransformation.md](CardTransformation.md)
- **已定案重點**：
  - Self Transform 由原始 CardData 的 TransformRule 控制；外部效果可透過獨立的 Form Override 暫時強制形態。
  - Effective Form 依 `External Override > Self Form > Base Form` 解析；第一版只允許一層 Override，並保留未來堆疊擴充空間。
  - Self Transform 保留 CardInstance Property 與 Base CardBuff；CardData 自帶 Property 隨形態替換。
  - External Override 暫停 CardInstance Property、凍結 Base CardBuff 與 Self TransformRule，並使用獨立的臨時 Buff Layer。
  - Override 期間取得的 CardBuff 在解除時全部移除；PlayerBuff 仍作用於 Override Form。
  - 規則以 Priority 優先，Priority 相同時依陣列順序決定。
  - `ObserveAction` 只更新 ReactionSession；只有明確的 Timing Dispatch 會產生一般 Buff Effect，並在一般 Reaction 後原子提交形態變更。
  - FormChanged 使用獨立 Timing；狀態與 CardInfo 提交後，由新 Effective Form 觸發，並重新驗證 View 的拖曳、聚焦與選取狀態。
  - Buff Command 由 Resolver 攜帶內部 LayerHandle；已失效 Layer 的排隊命令靜默 No-op 並保留診斷。
  - Self Form 只有 `BattleOnly`（本場限定）與 `Persistent`（永久保留）；勝利／失敗寫回，取消不寫回。
  - External Override 永遠清除且不持久化；第一版只需產生 CardInstanceChangeSet，不要求先完成完整戰鬥外系統。
  - Clone 只依當下 Effective CardData 建立獨立卡片，不複製 Instance Property、Buff 或形態狀態，也不自動加入 Dispose。
  - 一般與 Override 卡片資產分為 StandardCardDataScriptable／OverrideCardDataScriptable，並共用 CardDataScriptableBase。
  - 每個 EffectQueueRunner 擁有獨立的 Budget、CorrelationId 與 TriggerPath；同一 Runner 內的連鎖 Reaction 共用預算，超限時明確停止並輸出診斷。
- **目前進度**：
  - ✅ 階段 0：建立 T-010 Characterization Test 與測試 Builder。
  - ✅ 階段 1：完成 CardForm Model、CardData／Instance／Buff Property 分層、Clone 與持久化規則。
  - ✅ 階段 2：完成多型 CardTransformRule Operation、Evaluator 與 TransformRule Validator。
  - ✅ 階段 3：完成 Action Observation／Timing Dispatch 分流、Reaction Snapshot、Effect Queue Scope、ReactionSession 修正與 Validator。
  - ✅ 階段 4：完成 Self Transform Timing Dispatch、CardFormChanged Event、FormChanged Effect 與基本 CardInfo／View 更新。
  - ✅ 階段 5：已建立 Standard／Override CardData Scriptable 分型與共用 `CardData` 基底；只有 `StandardCardData` 擁有 `TransformRules`，Override 在型別與 Inspector 層級均無法定義 Self Transform。既有 Standard 資產已在保留 GUID 與內容的前提下完成型別遷移。
- **階段 4 收尾**：合併 Self Transform 的 QueueItem 執行責任、以 `EffectResult.Empty` 統一空結果、將無狀態的 `CardTransformRuleEvaluator` 靜態化，並以 `TimingDispatchPlanner` 集中建立一般 Reaction 與 Self Transform QueueItem；`GameplayManager` 僅保留 Reaction Snapshot 建立與流程驅動。
- **階段 4 驗證**：Buff Timing Pipeline 7 項、Effect Queue Runner 11 項及 T-010 20 項 EditMode 測試全數通過，Unity 編譯為 0 error。
- **階段 5 驗證**：Unity 編譯 0 error、0 warning；T-010 定向測試涵蓋 Scriptable 分型、Library 共用／Standard 查詢邊界、既有變身流程與資產 round-trip。20 個 Standard 資產的 GUID、CardData ID 與內容摘要在遷移前後一致，GameData Validator 全數通過。
- **TriggeredCardEffect 資產遷移**：將舊 `CardData.TriggeredCardEffect` 提升為 Standard／Override 共用的頂層型別；遷移前確認 20 個既有集合皆為空，遷移後 GUID、CardData ID 與集合數量一致，且資產中已無舊型別名稱。另以 Unity Asset round-trip 測試驗證 Timing 與多型 Effect 可保存並重新載入。
- **下一步**：開始階段 6，將 CardForm 的儲存與讀取整合進 SaveData。
- **後續階段**：CardBuff Layer、External Override、全區域 View 整合、CardInstanceChangeSet 與完整資料驗證。
- **完成條件**：變身後資料與畫面一致，保留／替換狀態符合規則，重新載入 CardInfo 不會回到舊資料。
- **狀態**：🔄 進行中（階段 0～4 完成）

### T-011：多步驟自訂目標選取

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

### T-017：完成 CardTriggeredTiming 生命週期觸發管線

- **目標**：讓 `CardData.TriggeredEffects` 與 `CardBuffData.Effects` 能在抽牌、打出、保留、丟棄、初始化等卡片生命週期中，依明確且唯一的時機進入 Effect Queue。
- **現況**：
  - `CardTriggeredTiming.FormChanged` 已由 T-010 階段 4 接入，會在形態狀態與最新 `CardInfo` 提交後執行新 Effective Form 的 Effects。
  - 其餘 `Drawed`、`EffectDrawed`、`Played`、`EffectPlayed`、`Preserved`、`Discarded`、`EffectDiscarded`、`Initialize` 目前只有 enum、資料欄位、Entity 查詢與 Validator，尚未找到正式的 Runtime 觸發入口。
  - `CardData.TriggeredEffects` 與 `CardBuffData.Effects` 共用 `CardTriggeredTiming`，實作時必須同時處理卡片本體與目前有效的 CardBuff，避免兩套生命週期語意分離。
- **開始前需決定**：
  - 一般流程與 Effect 造成的流程如何區分，例如 `Drawed`／`EffectDrawed`、`Played`／`EffectPlayed`、`Discarded`／`EffectDiscarded`。
  - 每個 timing 位於卡片區域移動、狀態提交、Gameplay Event 與畫面更新之前或之後。
  - CardData Effect 與 CardBuff Effect 的固定順序、快照範圍、Selected Card Context 與同一 EffectQueueRunner Budget 規則。
  - `Initialize` 的觸發範圍，以及既有 `Drawed` 命名若調整時的序列化數值與資產遷移策略。
- **建議階段**：
  1. 盤點每個 enum 對應的 GameplayManager／CardManager 狀態轉換點，建立唯一的生命週期順序表。
  2. 建立共用 Card Trigger Dispatch Queue Item，同時快照並執行 CardData 與有效 CardBuff Effects。
  3. 分批接入抽牌、打出、保留、丟棄與初始化流程，補齊 Effect 造成流程的來源辨識。
  4. 新增 EditMode 測試與 Validator，確認不重複觸發、Context 正確且新增／移除的 Effect 不回頭參與同一次快照。
- **完成條件**：除 `None` 外，每個 `CardTriggeredTiming` 都有一個明確且可測試的 Runtime 入口；CardData 與有效 CardBuff 依固定順序在同一 Queue Scope 執行，且一般流程與 Effect 造成的流程不會混用或重複觸發。
- **狀態**：⬜ 未開始

---

## 未來可能方向（非待辦）

- Effect Queue 的效果取消／替代。
- 戰鬥重播與 AI Simulation。
- GameData 與 GameModel 的 Content Spec → Runtime Compiler 拆分。
- GameData Validator 的 localize key、Target、LifeTimeData 等延伸規則。

這些項目目前不排入工作順序；等實際需求或風險出現後，再建立新的 T 編號。
