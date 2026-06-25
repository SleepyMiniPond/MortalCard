# 專案待辦事項

> 最後更新：2026-05-11  
> 狀態標記：⬜ 未開始 | 🔄 進行中 | ✅ 已完成

---

## 🔴 高優先 — 架構改善

### T-001：消除 Switch Expression 雙重派發
- **問題**：`EffectDataResolver` 與 `EffectCommandExecutor` 各有 15~17 分支的 type-switch，新增效果至少要改 4 個檔案（Effect 定義、Resolver 分支、Command 定義、Executor 分支），違反 Open-Closed Principle
- **方向**：
  - 方案 A：讓 `ICardEffect` 自帶 `Resolve()` / `IEffectCommand` 自帶 `Execute()`，把邏輯內聚到效果本身
  - 方案 B：Strategy Dictionary（`Dictionary<Type, IEffectResolver>`）做中間層，保持 data class 純淨
  - 方案 C：Visitor Pattern 分離資料與行為但仍集中註冊
- **影響檔案**：`EffectDataResolver.cs`、`EffectCommandExecutor.cs`、`CardEffect.cs`、`EffectCommand.cs`
- **狀態**：✅ 已完成（2026-05-11）
  - 方案 B 實施：`Resolvers/` 目錄（ICardEffectResolver × 13 + IPlayerBuffEffectResolver × 5）、`Handlers/` 目錄（IEffectCommandHandler × 11）
  - 新增效果只需加 Resolver/Handler class 並在 registry 新增一行，不需動 Resolver/Executor 本體

---

## 🟡 中優先 — 功能補完

### T-002：接通 CardBuff / CharacterBuff 觸發管線
- **問題**：`GameplayManager._TriggerTiming()` 中角色 Buff 和卡牌 Buff 的 foreach 迴圈是空殼，只有 PlayerBuff 真正接入反應管線
- **方向**：參照 PlayerBuff 的觸發邏輯，為 CardBuff 和 CharacterBuff 補上 `ConditionalEffect` 的解析與執行
- **注意**：CardBuff scope 較小（只影響單張牌），TriggerContext 需正確攜帶該 Card 作為 Source
- **影響檔案**：`GameplayManager.cs`（`_TriggerTiming` 方法）
- **狀態**：✅ 已完成（2026-05-15）
  - CharacterBuff 與 CardBuff 的觸發邏輯已完整實作，結構與 PlayerBuff 一致

### T-003：評估 Effect Queue 機制
- **問題**：目前效果是 `List<ICardEffect>` 線性展開成 `EffectCommandSet` 一次性執行，無法支援效果鏈（效果 A 結果影響效果 B）、效果中觸發新效果、效果取消/替代
- **方向**：引入 Effect Queue，讓效果執行中可以往佇列塞新效果；`GameContextManager` 的 stack-based scoping 已有雛形
- **前置**：先完成 T-001（消除 switch 派發後更容易引入 Queue）
- **狀態**：⬜ 未開始

### T-004：建立核心 EditMode 測試與測試資料建構器
- **問題**：核心 GameModel 與 Buff Timing 缺少自動化回歸保護，後續 T-003 Effect Queue、Buff 連鎖控制、Preview / Simulation 等工作容易在重構時產生行為回歸
- **方向**：建立 EditMode 測試基礎、測試資料建構器與可控的 `GameplayManager` 測試接縫，優先覆蓋 Buff timing 與 GameContext scope 行為
- **完成內容**：
  - 新增 `GameplayManager` 測試用 constructor overload，可注入受控 `GameStatus`，避免新增生硬的 `SetGameStatusForTesting` 測試介面
  - 新增 `InternalsVisibleTo("MortalGame.EditModeTests")`，讓 EditMode 測試能存取必要的 internal 測試接縫
  - 建立測試資料建構器：`GameContextTestBuilder`、`GameplayManagerTestBuilder`、`BuffTestBuilder`、`CardTestBuilder`、`OptionTestValue`
  - 新增 `BuffTimingPipelineTests`，覆蓋 PlayerBuff、CharacterBuff、CardBuff timing、條件判斷、Card selected context，以及 `TriggerBuffEnd` 後續觸發
  - 新增 `GameContextManagerTests`，覆蓋 selected player / character / card scope 的 push 與 restore 行為
  - 調整 `MortalGame.EditModeTests.asmdef`，讓測試 assembly 能正確參考 Optional 套件
- **驗證結果**：
  - `BuffTimingPipelineTests`：6 passed / 0 failed
  - `GameContextManagerTests`：4 passed / 0 failed
  - 完整 EditMode 測試：56 passed / 0 failed
  - 驗證使用 Unity `6000.0.3f1` batchmode，在臨時專案副本執行，避免與使用者開啟中的 Unity Editor 搶 project lock
- **注意事項**：
  - Unity log 仍有既有 NuGetForUnity duplicate / same filename 警告，非本次 T-004 引入
  - 後續若要進行 T-003，應優先沿用這批 builder 與 Buff timing 測試擴充案例
- **狀態**：✅ 已完成（2026-06-25）

### T-005：修正場景級 UniTask 取消與 Presenter 生命週期
- **問題**：場景與 Presenter 流程中仍可能存在 `.Forget()`、取消 token 傳遞不完整、切場景後非同步流程繼續更新 View / Model 的風險
- **方向**：建立場景級與 Presenter 級的取消邊界，讓 Menu、LevelMap、Gameplay 等場景流程在離開、重開或銷毀時能可靠取消尚未完成的 UniTask
- **設計思路**：
  - 盤點 `GameplayPresenter`、Scene `Run()` 流程與 UI command loop 中的 `.Forget()` 使用點
  - 將場景生命週期 `CancellationToken` 傳入 Presenter 與長時間等待流程
  - 對戰鬥結束、切場景、重新開始、離開戰鬥等路徑建立取消測試或最小驗證
  - 明確區分「背景 fire-and-forget」與「必須被場景生命週期管理」的非同步工作
- **影響檔案**：`GameplayPresenter.cs`、Scene 相關 `Run()` 流程、可能包含 UI command / action loop
- **狀態**：⬜ 未開始

### T-006：導入戰鬥專用決定性亂數服務
- **問題**：`GameStageSetting.RandomSeed` 已存在，但洗牌、抽取與其他隨機行為仍可能直接使用 `UnityEngine.Random`，導致相同 seed 無法重現同一場戰鬥
- **方向**：導入戰鬥專用亂數服務，讓每場戰鬥持有獨立且可注入的亂數狀態，支援測試、重播、AI simulation 與問題重現
- **設計思路**：
  - 定義 `IGameRandom` 或等價介面，封裝 range、shuffle、choice 等常用操作
  - 由 `GameStageSetting.RandomSeed` 初始化每場戰鬥的亂數實例
  - 將洗牌、抽牌順序、敵方隨機決策等戰鬥內隨機來源改由服務提供
  - 增加 EditMode 測試：相同 seed 產生相同結果，不同 seed 可產生不同結果
- **影響檔案**：`GameStageSetting`、戰鬥建立流程、卡牌抽洗流程、敵方邏輯中使用亂數的位置
- **狀態**：⬜ 未開始

### T-007：定義模組命名空間與 asmdef 遷移順序
- **問題**：專案已開始導入 Runtime / Editor / Tests 的 asmdef 基礎，但大量類別仍缺少穩定命名空間與清楚模組邊界；若直接硬切 GameData / GameModel 可能引發程序集循環
- **方向**：先定義命名空間與 asmdef 遷移順序，採漸進式整理 Runtime、Editor、Tests 邊界，避免一次性大搬家
- **設計思路**：
  - 先確認 Runtime、Editor、EditMode Tests、PlayMode Tests 的引用方向與允許依賴
  - 為核心區域規劃命名空間，例如 GameModel、GameData、GameView、Presenter、Scene
  - 暫緩硬切 GameData / GameModel assembly，先處理 Data 建立 Entity 造成的反向依賴
  - 建立小批次遷移準則：每次只移動一個清楚邊界，並以 EditMode 測試與 Unity compile 驗證
- **影響檔案**：各 `.asmdef`、Runtime / Editor / Tests 目錄下的 C# 命名空間與引用
- **狀態**：⬜ 未開始

### T-008：建立 ScriptableObject 資料驗證與 Resolver / Handler 註冊檢查
- **問題**：Effect、Buff、CardData 等 ScriptableObject 資料與 Resolver / Handler registry 的缺漏，可能要到實際遊戲流程才爆錯，缺少提早檢查機制
- **方向**：建立 EditMode 測試或 Editor validation，檢查資料引用、效果解析器與命令處理器註冊是否完整
- **設計思路**：
  - 檢查每個 `ICardEffect` / `IPlayerBuffEffect` / 其他效果資料是否有對應 resolver
  - 檢查每個 `IEffectCommand` 是否有對應 handler
  - 檢查卡牌、Buff、角色、敵人等資料引用的 ID 是否存在於對應 library
  - 將常見資料錯誤整理成可讀的錯誤訊息，方便設計資料調整時定位問題
  - 優先以 EditMode 測試保護 registry 完整性，再視需求補 Editor 選單驗證工具
- **影響檔案**：Resolver / Handler registry、ScriptableObject 資料載入與 library、EditMode Tests
- **狀態**：⬜ 未開始

---

## 🟣 新功能 — 卡牌系統擴展

### T-010：卡片變身（保留狀態）
- **需求**：卡片能夠變成另一張卡片，但保留既有狀態（如降費、已附加的 Buff）
- **設計思路**：
  - CardEntity 已有 `_mutationCardDataIds` 欄位，暗示變身機制的雛形已存在
  - 變身 = 切換 `_actingCardDataId`，但保留 `CardBuffManager` 和 `CardPropertyEntity` 的現有狀態
  - 需要定義哪些屬性跟著原卡、哪些跟著新卡（如：費用修改保留，基礎效果換新）
- **狀態**：⬜ 未開始

### T-011：多步驟自訂目標選取
- **需求**：卡片打出時顯示 UI 讓玩家依序選擇多次不同目標（例如先從手牌選 1 張，再從牌堆選 3 張）
- **設計思路**：
  - 現有 `SubSelectionPresenter` 已支援單次子選取，需擴展為**有序多步驟選取佇列**
  - 每一步定義：來源區域（手牌/牌堆/墓地/場上）、選取數量、篩選條件、顯示說明
  - `ISubSelectionGroup` 可以擴展為有序列表，依序彈出選取面板
  - 選取結果按步驟 ID 存入 `ISubSelectionAction` 字典
- **狀態**：⬜ 未開始

### T-012：卡片合成系統（自訂藥水）
- **需求**：類似爐石的自訂藥水 — 玩家先收集效果片段，打出合成卡時進行多次 N 選 1，組合成一張新卡
- **設計思路**：
  - 需要新的 CardProperty 或 CardBuff 類型來記錄「已收集的效果片段」
  - 合成打出時觸發特殊的 SubSelection 流程（多輪 N 選 1）
  - 選取完成後根據組合結果，動態建立新的 CardInstance（可能搭配 T-010 變身機制）
  - 或者：合成結果對應預定義的 CardData 組合表（較簡單但彈性低）
- **前置**：T-011（多步驟選取）、T-010（卡片變身）
- **狀態**：⬜ 未開始

### T-013：戰鬥中敵人動態增減
- **需求**：戰鬥中能夠新增敵人（增援）或移除敵人（逃跑）
- **設計思路**：
  - 目前 `PlayerEntity.Characters` 是初始化時建立的固定陣列
  - 需要改為動態集合（`List` 或 `ReactiveCollection`）
  - 新增敵人：運行時建立 CharacterEntity 插入集合，View 層需動態生成 CharacterView
  - 敵人逃跑：標記角色離場（非死亡），移出戰鬥計算，View 層播放離場動畫
  - 影響層面廣：目標解析（`ITargetCharacterCollectionValue`）、勝負判定、EnemyLogic、View 排版
- **狀態**：⬜ 未開始

### T-014：Preview / Simulation 預演管線
- **需求**：支援長按手牌或 hover 時預覽效果，例如預估傷害、預計命中目標、預期抽牌/生成結果；長期也可支援 AI 預演與除錯用途
- **設計思路**：
  - 保留既有 `Effect -> Command -> Result` 三段式架構，但正式區分三種用途：`Preview`、`Simulation`、`Execution`
  - `Preview`：只跑 Resolver，產出可供 UI 顯示的 Command/Preview 資訊，不真的修改遊戲狀態
  - `Simulation`：在隔離的模擬上下文中執行 Command，取得接近真實結果的 SimulatedResult，但不污染正式戰鬥狀態
  - `Execution`：維持現行正式套用流程，真正更新 Entity、Session、Event
  - 第一階段可先做 command-based preview（目標高亮、預估數值）；第二階段再補完整 simulation sandbox
- **依賴/關聯**：
  - 與 T-001 高度相關，因為 preview / simulation 需要更乾淨的 dispatch 邊界
  - 若未來要做 AI 預演、連鎖效果預覽、複雜 UI 提示，會與 T-003（Effect Queue）互相影響
- **狀態**：⬜ 未開始

### T-015：CardInfo 氾濫 — 重複製造與事件冗餘
- **問題**：`CardInfo.Create()` 每次都執行 `GameFormula` 計算 + Buff 枚舉 + LINQ 合併，並非輕量操作，但目前存在多個重複製造的路徑：
  1. **Buff 事件與 GeneralUpdateEvent 雙重更新同一張卡**：`EffectCommandExecutor` 在執行 `AddCardBuffEffectCommand` / `RemoveCardBuffEffectCommand` / `ModifyCardBuffLevelEffectCommand` 時，同時發出 `AddCardBuffEvent(card.ToInfo())` 和 `GeneralUpdateEvent(card.ToInfo())`（透過 `UpdateReactorSessionAction` → `PlayerEntity.Update()` 產生），View 側兩者都流向 `_gameViewModel.UpdateCardInfo()`，同一張卡被重複更新
  2. **CardManagerInfo 內無謂計算 PlayingCard**：`DrawCardEvent`、`CreateCardEvent`、`UsedCardEvent`、`PlayerExecuteStartEvent` 等事件都攜帶 `CardManagerInfo`，其中包含 `PlayingCard.ToInfo()`，但 PlayingCard 在這些事件的當下實際上並未改變
  3. **CardBuff 事件攜帶完整 CardInfo 但不必要**：`AddCardBuffEvent` / `RemoveCardBuffEvent` / `ModifyCardBuffLevelEvent` 的目的只是通知某張卡的 Buff 列表有變，卻帶了包含 Cost/Power 計算的完整 CardInfo
- **方向**：
  - **核心原則**：命名事件只描述「發生了什麼」，不負責搬運完整資料快照；由 View 收到 Identity 後自行從 `GameInfoModel.ObservableCardInfo()` pull 最新值
  - `AddCardBuffEvent` / `RemoveCardBuffEvent` / `ModifyCardBuffLevelEvent` 改為只攜帶 `Faction` + `Guid Identity`，不帶 `CardInfo`
  - 評估 `CardManagerInfo` 的 `PlayingCard` 欄位是否真的需要 CardInfo，或改為 `Option<Guid>`
  - 釐清 `GeneralUpdateEvent` 和 CardBuff specific event 在 View 側的職責分工，避免同一更新走兩條路
- **影響檔案**：`GameEvent.cs`、`EffectCommandExecutor.cs`、`GameplayView.cs`、`GameInfoModel.cs`
- **狀態**：⬜ 未開始

---

## 建議執行順序

```
T-001（消除 Switch 派發）
  ↓
T-002（接通 Buff 管線）  +  T-010（卡片變身）
  ↓                            ↓
T-004（EditMode 測試基礎，已完成）
  ↓
T-005（生命週期） + T-006（決定性亂數） + T-008（資料驗證）
  ↓
T-003（Effect Queue）     T-011（多步驟選取）
  ↓             ↓              ↓
T-007（asmdef / 命名空間整理，分階段穿插）
  ↓
T-014（預演管線） T-013（敵人動態增減） T-012（卡片合成）
```

- T-001 是基礎設施改善，先做會讓後續所有功能的開發更輕鬆
- T-004 已完成，提供 Buff Timing 與 GameContext scope 的測試保護；後續 T-003 應沿用既有測試 builder 擴充案例
- T-005、T-006、T-008 是進入 T-003 前的安全網：分別處理非同步生命週期、可重現亂數、資料與 registry 驗證
- T-007 不一定要一次完成，建議配合後續重構分批整理，避免大規模命名空間與 asmdef 搬遷造成噪音
- T-010 和 T-011 互相獨立，可以平行開發
- T-012 依賴 T-010 + T-011 的基礎
- T-013 相對獨立但影響面廣，建議架構穩定後再動
- T-014 屬於中長期能力建設，先做輕量 preview 即可，完整 simulation 可等 T-001 / T-003 方向穩定後再投入
