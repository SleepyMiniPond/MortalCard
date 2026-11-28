# 專案待辦事項

> 最後更新：2026-11-28
> 狀態標記：⬜ 待開始／待排期 | 🟡 已有部分基礎，尚未完成 | 🔄 進行中 | ✅ 已完成
> T-021 已於 2026-11-22 完成四項工作，經使用者最終確認後結案。
> 已完成任務與階段歷史見 [TODO_Archive](TODO_Archive.md)。

## 工作優先順序

```text
目前進行中：T-030 例外型別與訊息集中整理，先盤點與逐批遷移。T-024 第 1～4 個工作已確認完成，第 5 個整合回歸及兩關人工驗收待例外整理後續接。

獨立排期
├─ T-024 場景結果、重試與戰後資料銜接（進行中）
├─ T-030 例外型別與訊息集中整理（進行中，插在 T-024 整合驗收前）
├─ T-025 FormChanged 生命週期一致化
├─ T-026 Buff 資源上限接線
├─ T-027 減少好感度事件呈現
├─ T-028 可配置的卡片出牌條件
├─ T-029 GameplayManager 職責拆分
├─ T-013 敵人動態增減
└─ T-014 Preview / Simulation
```

T-010～T-012 與 T-017～T-023 已完成並封存。T-017 範圍內的卡片生命週期觸發已完成（FormChanged 專用路徑差異另列 T-025）。T-024 已啟動，其餘獨立項目待排期。

---

## 最近完成

T-021 初始抽牌優先的四項工作已完成，包含雙方單次穩定排序、View 同步、可配置的 Inspector 效果選單限制，以及臨時資料清理。範圍與最終驗證見 [T-021 封存紀錄](TODO_Archive.md#t-021初始抽牌優先與效果選單設定已完成)。

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
- **已提出的使用需求（2026-10-04）**：手牌顯示加成後數字，指向敵人時顯示預測傷害。T-011 設計需保留部分選取與完整選取的區別，以及不污染正式狀態的查詢方式；預覽功能仍歸本項，未納入 T-011 實作範圍。
- **建議階段**：先做 Resolver 層的輕量 Preview；完整 Simulation sandbox 等 AI 或除錯需求明確後再設計。
- **既有基礎**：T-001 Resolver／Handler、T-003 Effect Queue、T-006 決定性亂數；CardInfo.CreatePreview 只提供卡牌威力預覽，尚非效果結果預演或獨立狀態沙盒。
- **完成條件**：Preview 不污染正式狀態，且相同輸入能產生穩定、可供 UI 使用的預演資訊。
- **狀態**：⬜ 未開始

## 架構核對留下的工作（待排期）

以下是先前架構核對留下的缺口；本次文件整理保留既定排期，不自動插入主線。

### T-024：場景結果、重試與戰後資料銜接

- **範圍**：Scene／Presenter／戰鬥外狀態；獨立排期，先完成結果流程，再接續戰後資料套用。
- **現況**：GameFlow 已接入結果轉移、新一輪重設與同關原種子／配置保存；SceneLoadManager 只載入，GameFlow 呼叫 Scene.Run。臨時兩關與畫面標示已接入，第一勝回地圖、第二勝回選單。結果 UI 與手牌生命週期重構已確認；戰後快照、Domain 套用及正常勝利提交已接通，只有勝利操作與清理正常完成才延續持久形態、HP 與好感度，下一戰能量回滿。完整整合與兩關人工驗收仍留第 5 個工作。
- **工作**：定義 Win／Retry／Restart／Quit 的狀態轉移、保存同關重試設定、提供勝利完成操作，再定義並套用戰後 Domain 狀態。磁碟存檔另行規劃。
- **完成條件**：各結果能離開目前流程；Retry 後再 Quit 不再重試；同關設定可重現；只有應提交的結果寫回，失敗／取消不提交。
- **依據**：[Main](../Assets/Scripts/Scene/Main.cs)、[GameResultWinPresenter](../Assets/Scripts/Presenter/Gameplay/GameResultWinPresenter.cs)、[BattleBuilder](../Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs)、[Instance](Instance.md)。
- **已確認規則**：勝利回地圖、Retry 保留原局、Restart 新一輪回地圖、Quit 回選單。勝利延續卡片持久形態、血量與好感度，下一戰能量回滿；失敗／取消不提交。
- **狀態**：🔄 第 1～4 個工作已經使用者確認完成；第 5 個工作待 T-030 例外整理後續接。細部進度與驗證以目前工作索引指向的原工作計畫為準。

### T-030：例外型別與訊息集中整理

- **範圍**：Assets/Scripts 的自訂例外敘述與必要資料；依 UI、Domain／流程、Factory／Resolver、其他 Runtime、Editor 分批處理。
- **目標**：使用處只建立專門 Exception 並傳入必要型別、身份或數值；訊息格式集中於例外本身，保留結構化診斷欄位。
- **界線**：保留標準參數例外、取消與既有控制訊號的語意；不引入全域捕捉、玩家訊息本地化或改變 throw／return 的控制流。遷移時同步測試的精確型別斷言。
- **狀態**：🔄 靜態盤點與分步提議完成，待使用者確認；尚未修改程式。先於 T-024 第 5 個整合驗收處理，細部進度見目前工作索引指向的原計畫。

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
- **現況**：已有手牌、`Sealed`、主動出牌費用與選取有效性檢查；自動選取遇到必要主目標無候選時略過，ExistCard 子選取依 CountRule 處理不足，RequiredExact 不足會拒絕整次嘗試。尚無可配置的一般出牌前置條件。
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
