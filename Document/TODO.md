# 專案待辦事項

> 最後更新：2026-08-19
> 狀態標記：⬜ 未開始 | 🔄 進行中 | ✅ 已完成
> 已完成任務與驗證紀錄請查看 [TODO_Archive.md](TODO_Archive.md)。

## 工作優先順序

```text
現在可開始
└─ T-019 通用遊戲狀態查詢／Value／Condition
        ↓
    T-020 統一 Reaction Effect 執行能力
        ↓
    T-017 CardTriggeredTiming 生命週期觸發管線
        ↓
    T-011 多步驟目標選取
        ↓
    T-012 卡片合成

獨立排期
├─ T-013 敵人動態增減
└─ T-014 Preview / Simulation
```

T-010 與 T-018 已完成並封存。接下來先完成通用資料表達基礎，再補齊 Reaction Effect 與卡片生命週期，讓新增卡片能以資料資產完成，而不是持續為單一卡片增加專用程式。T-011 與 T-012 延後至這三項完成後；T-013、T-014 影響面較廣，不與這條主線同時進行。

---

## 現在可開始

### T-019：通用遊戲狀態查詢／Value／Condition

- **目標**：建立可組合、可序列化的 Target／Value／Condition 基礎，使企劃能直接用資料描述回合、區域、角色狀態與集合條件，不必為每張卡新增專用條件類別。
- **現況**：
  - 缺少回合數、奇偶數、目前流程階段與卡片所在區域等常用狀態。
  - 卡牌集合主要只有單張與手牌來源，缺少依玩家、區域、條件進行通用查詢與篩選。
  - 整數運算只有加法與乘法，缺少減法、除法、餘數、最小／最大值與集合計數。
  - 玩家／角色／卡牌條件只覆蓋少量欄位，難以描述生命、護盾、好感度、Effective Form 與 Buff 狀態。
- **設計原則**：優先建立可重用的狀態來源與組合器，不建立「定時炸彈條件」或「刀盾條件」等內容專用型別。
- **建議階段**：
  1. 定義 Gameplay、Player、Character、Card、CardZone 的查詢邊界與空值語意。
  2. 補齊回合、區域、生命／護盾、卡片形態、Buff、集合計數與必要算術 Value。
  3. 建立集合 Filter／Any／All／Count 與數值比較、集合包含、奇偶等可組合 Condition。
  4. 為多型巢狀資料補 Validator、序列化 Round Trip 與 Eval EditMode 測試。
- **完成條件**：至少能完全以資產描述「持有者回合結束且卡片在手牌」、「偶數／奇數回合開始」、「依玩家與區域篩選卡牌」等條件，且錯誤資料能在 Editor 階段被攔截。
- **狀態**：⬜ 未開始

---

## 主線依序完成

### T-020：統一 Reaction Effect 執行能力

- **目標**：讓 Card、PlayerBuff、CharacterBuff、CardBuff 的反應效果能重用核心遊戲操作與 Effect Queue，而不必為每種來源複製 Damage、Shield、DrawCard 等效果實作。
- **現況**：
  - `ICardEffect` 已有傷害、護盾、抽牌、移牌與 Buff 等主要操作。
  - `IPlayerBuffEffect` 與 `ICharacterBuffEffect` 只支援少量修正型效果。
  - `ICardBuffEffect` 沒有任何正式具體型別，Resolver Registry 也是空的；CardBuff 雖能收到 Timing，卻無法執行實際效果。
- **開始前需決定**：
  - 採用共用 Gameplay Effect Spec、Reaction 對 CardEffect 的安全轉接，或保留不同介面但共用 Resolver／Command 建構層。
  - 各來源的 Triggered Owner、Caster、Selected Target 與 Playing Card Context 如何明確對應。
  - 哪些核心操作允許由所有 Reaction 來源使用，哪些需要限制。
- **建議階段**：
  1. 定義 Reaction Effect 到核心 Effect Command 的共用執行契約。
  2. 先完成 CardBuff 的傷害、護盾、治療、能量、抽牌與 Buff 操作垂直切片。
  3. 將 PlayerBuff／CharacterBuff 可共用的操作遷移到相同模型，保留來源特有的效果修正能力。
  4. 補齊 Resolver／Handler 註冊檢查、Context、Queue 順序與失效目標測試。
- **完成條件**：CardBuff 能在任一支援的 `GameTiming` 執行核心遊戲操作；新增共用操作時不需要為四種來源複製四套 Resolver／Command 流程。
- **狀態**：⬜ 未開始

### T-017：完成 CardTriggeredTiming 生命週期觸發管線

- **前置**：T-018、T-019、T-020。
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
