# 專案待辦事項

> 最後更新：2026-07-11  
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
- **完成內容**：
  - 新增 `EffectQueueRunner` 與 `EffectQueueItem`，將 CardEffect、PlayerBuffEffect、CharacterBuffEffect、CardBuffEffect 納入統一佇列執行流程
  - `EffectQueueItem.Execute()` 改為接收 `IEffectQueueContext`，允許效果執行期間插入後續效果
  - 新增 `EnqueueImmediate()`，用於保留原本遞迴觸發的深度優先順序，例如 buff 效果結束後立即處理對應的 `TriggerBuffEnd`
  - 新增最大處理數保護，超過上限時安全停駐並保留尚未執行的 pending queue，避免無限連鎖造成死循環
  - `_TriggerTiming()` 改為透過 `TriggerTimingQueueItem` 進入 Effect Queue，`TriggerBuffEnd` 這類遞迴 timing 也以 queue item 表示
  - 將 Buff timing 專屬 queue item 拆出至 `BuffTimingQueueItems.cs`，讓 `EffectQueueRunner` 保持通用佇列基礎設施，`GameplayManager` 保留 timing 掃描與組裝責任
  - 擴充 `EffectQueueRunnerTests`，覆蓋佇列順序、立即插入與 max count 安全停駐
- **驗證結果**：
  - `dotnet build MortalGame.EditModeTests.csproj`：0 error，2 warnings（既有 `GameplayManager.OneTurnStart` / `OnTurnEnd` 未使用）
  - Unity MCP `assets-refresh`：成功
  - Unity MCP `tests-run` EditMode：63 passed / 0 failed / 0 skipped
  - 測試中仍有 `NoOpCardBuffEffect` 未知 resolver warning，屬於既有測試用空效果案例
- **後續注意**：
  - 目前完成的是執行期 Effect Queue 的第一版架構；「效果取消 / 替代」尚未實作，建議等實際卡牌需求或 Preview / Simulation 管線明確後再擴充
  - 後續新增會連鎖觸發的效果時，應優先補 EditMode 測試確認 queue 順序與 selected context 邊界
- **狀態**：✅ 已完成（2026-06-29）

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
- **完成內容**：
  - 建立 `Main → SceneLoadManager → Scene → Presenter → UI / Model` 的 CancellationToken 傳遞鏈；各 Scene 連結自身銷毀 Token，場景載入與長時間等待皆可取消
  - `GameplayManager.StartBattle()`、玩家 Action 等待與相關長時間流程改為強制接收 Token，不保留可省略的 default token
  - GameplayPresenter 保存並監督 Battle、Gameplay Event、UI 三條並行工作；Battle 正常完成後取消其餘工作並等待完全收斂，輔助 loop 提前完成或失敗則向上回報
  - 玩家 pending queue 改存 `IGameCommand`，由單一 Gameplay Event loop Dequeue 後依序啟動處理，避免 UniTask 在入列時提前執行
  - UI、SubSelection、卡片詳情 Popup 與勝負面板完整傳遞 Token，並以 `finally` 保證關閉面板與釋放 UniRx 訂閱
  - 新增 `CharacterAnimationWorker`，以 `Dispose + Completion` 管理角色動畫事件佇列、進行中動畫與取消收斂，不再使用無主 `.Forget()`
  - 新增 `ICharacterEventAnimationPlayer` / `CharacterEventAnimationPlayer` 分離動畫排程與事件呈現；新增 `BaseAnimationEventView` 統一 PlayableDirector 的取消、停止與隱藏生命週期
  - GameplayView 管理 Ally / Enemy Character Worker；替換或戰鬥結束時 Dispose 並等待 Completion。多角色集合遷移已記錄於 T-013
- **驗證結果**：
  - Unity AssetDatabase refresh：成功，0 compile error
  - Unity EditMode Tests：111 tests，狀態 Passed，0 failed（新增 4 項生命週期測試）
  - `dotnet build MortalGame.Scene.csproj`：0 warning / 0 error
  - `dotnet build MortalGame.EditModeTests.csproj`：0 warning / 0 error
  - 七個 EventView Prefab 的 PlayableDirector 序列化引用保持有效
- **已知限制**：勝利結果面板目前沒有正常關閉按鈕或完成事件，仍會等待 Scene 取消；屬既有功能缺口，不在 T-005 內擅自定義互動
- **狀態**：✅ 已完成（2026-07-12）

### T-006：導入戰鬥專用決定性亂數服務
- **問題**：`GameStageSetting.RandomSeed` 已存在，但洗牌、抽取與其他隨機行為仍可能直接使用 `UnityEngine.Random`，導致相同 seed 無法重現同一場戰鬥
- **方向**：導入戰鬥專用亂數服務，讓每場戰鬥持有獨立且可注入的亂數狀態，支援測試、重播、AI simulation 與問題重現
- **設計思路**：
  - 定義 `IGameRandom` 或等價介面，封裝 range、shuffle、choice 等常用操作
  - 由 `GameStageSetting.RandomSeed` 初始化每場戰鬥的亂數實例
  - 將洗牌、抽牌順序、敵方隨機決策等戰鬥內隨機來源改由服務提供
  - 增加 EditMode 測試：相同 seed 產生相同結果，不同 seed 可產生不同結果
- **影響檔案**：`GameStageSetting`、戰鬥建立流程、卡牌抽洗流程、敵方邏輯中使用亂數的位置
- **狀態**：✅ 已完成（2026-07-07）
- **完成摘要**：
  - 新增 `IGameRandom` / `GameRandom`，以 `System.Random` 提供戰鬥專用、可注入的決定性亂數
  - `GameContextManager` 必須由外部注入唯一 `IGameRandom`，移除 `gameRandom = null`、`SetGameRandom()` 與 fallback 建構路徑
  - `BattleBuilder` / `GameplayScene` 改由 `GameStageSetting.RandomSeed` 建立戰鬥 context random
  - `DeckEntity`、`PlayerCardManager`、`PlayerEntity` 改為強制傳入 `IGameRandom`，移除無參數或自動產生 random 的相容性入口
  - `Utility.Shuffle()` 僅保留 `Shuffle(IGameRandom random)`，`SelectTargetLogic` 的隨機 sub-selection 已改用 battle random
  - 移除 `GameStatus.RandomState` 對 `UnityEngine.Random.state` 的舊式全域亂數暴露
- **驗證結果**：
  - Unity AssetDatabase refresh：0 compile error
  - Unity EditMode tests：73 passed / 0 failed / 0 skipped
  - `dotnet build MortalGame.EditModeTests.csproj`：0 error

### T-007：完成模組命名空間、依賴反轉與 asmdef 遷移
- **問題**：專案已開始導入 Runtime / Editor / Tests 的 asmdef 基礎，但大量類別仍缺少穩定命名空間與清楚模組邊界；GameData 的 `CreateEntity()` 與 GameModel 形成雙向依賴，若直接硬切會引發程序集循環
- **方向**：採漸進式整理命名空間、Runtime / Editor / Tests 邊界，並將 Entity 建立責任由 GameData 移至 GameModel Factory / Builder 或等價單向設計；完成依賴反轉後，實際拆分 Runtime 子 assembly
- **設計思路**：
  - 先確認 Runtime、Editor、EditMode Tests、PlayMode Tests 的引用方向與允許依賴
  - 為核心區域規劃命名空間，例如 GameModel、GameData、GameView、Presenter、Scene
  - 先處理 Data 建立 Entity 造成的反向依賴，再拆分 GameData / GameModel assembly
  - 建立小批次遷移準則：每次只移動一個清楚邊界，並以 EditMode 測試與 Unity compile 驗證
- **影響檔案**：各 `.asmdef`、Runtime / Editor / Tests 目錄下的 C# 命名空間與引用
- **完成內容**：
  - 完成 Runtime、UI、Presentation Abstractions、GameView、Presenter、Scene 的單向 assembly 邊界
  - 新增 `MortalGame.Presentation.Abstractions`，集中 `IGameCommand`、`IGameViewModel`、`IGameplayActionReciever`、`ISelectableView` 等 Presenter / View 溝通契約
  - 將 GameView Panel 目錄中的 Presenter 實作移至 `Assets/Scripts/Presenter/Gameplay/`，並保留 Unity 資產 GUID
  - 將 `Main.cs` 移入 Scene assembly，解除 Runtime 與 Scene / Presenter / GameView 間的反向依賴
  - GameView 不再引用 Presenter；Presenter 單向依賴 GameView 與 Presentation Abstractions
  - GameData 與 GameModel 維持同屬 `MortalGame.Runtime`。目前可執行資料型別直接依賴 runtime Entity / Context，若要再拆分必須另案導入 Content Spec → Runtime Compiler，避免本任務擴張為 ScriptableObject 與 Odin 多型資料重寫
- **驗證結果**：
  - Unity AssetDatabase refresh：0 compile error
  - Unity EditMode tests：107 passed / 0 failed / 0 skipped
  - PlayMode Tests 目前只有 asmdef，尚無可執行測試案例
  - `dotnet build MortalGame.Scene.csproj`：0 warning / 0 error
  - `dotnet build MortalGame.EditModeTests.csproj`：0 error；僅有既有 UniRx 過時 API 與未使用事件警告
  - Unity 掃描全部 Prefab / Scene：未發現 Missing Script
  - ScriptableObject、Prefab、Scene 資產未發現舊 `Assembly-CSharp` managed-reference 型別識別
- **狀態**：✅ 已完成（2026-07-11）

### T-008：建立 ScriptableObject 資料驗證與 Resolver / Handler 註冊檢查
- **問題**：Effect、Buff、CardData 等 ScriptableObject 資料與 Resolver / Handler registry 的缺漏，可能要到實際遊戲流程才爆錯，缺少提早檢查機制
- **方向**：建立 EditMode 測試或 Editor validation，檢查資料引用、效果解析器與命令處理器註冊是否完整
- **完成內容**：
  - 新增 `ScriptableObjectDataValidationTests`，以 EditMode 測試持續掃描 `Assets/ScriptableObjects` 下的實際資料資產
  - 以反射檢查所有具體 `IEffectCommand` 型別皆有 `IEffectCommandHandler` 註冊，避免新增 command 時漏接 handler
  - 檢查 Card / PlayerBuff / CharacterBuff / CardBuff 資料中實際使用到的 Effect 是否有對應 resolver
  - 檢查常見跨 library ID 引用：`AddPlayerBuffEffect.BuffId`、`RemovePlayerBuffEffect.BuffId`、`CreateCardEffect.CardDataIds`、`AddCardBuffData.CardBuffId`、`RemoveCardBuffEffect.BuffId`、Deck 卡牌引用與 Player/Enemy Deck 引用
  - 調整 `MortalGame.EditModeTests.asmdef`，補上 `Sirenix.Serialization.dll`，讓 EditMode 測試 assembly 能正確讀取 Odin `SerializedScriptableObject`
- **驗證結果**：
  - Unity MCP `assets-refresh`：成功，0 compile error
  - Unity MCP `tests-run` EditMode：67 passed / 0 failed / 0 skipped
  - `dotnet build MortalGame.EditModeTests.csproj`：0 warning / 0 error
- **後續注意**：
  - 目前先以 EditMode 測試作為持續保證；若後續設計資料調整頻繁，再抽出共用 `GameDataValidator` 並補 Unity Editor menu（例如 `MortalGame/Validate Game Data`）
  - 可再擴充 localize key、MainTarget/SubSelection 完整性、LifeTimeData 空值與 AllCard/AllBuff 集合覆蓋率檢查
- **影響檔案**：Resolver / Handler registry、ScriptableObject 資料載入與 library、EditMode Tests
- **狀態**：✅ 已完成第一階段（2026-06-30）

---

## 🟣 後續架構討論：TriggerTiming / ReactorSession 時機一致化

### T-016：重構 TriggerTiming 與 UpdateReactorSessionAction 的時機模型
- **問題**：目前 `UpdateReactorSessionAction` 分散在 GameplayManager 主流程、EffectCommandHandler、Triggered*BuffEffectQueueItem，導致 timing session 更新順序不一致；特別是 `TriggerBuffEnd` 目前可能只觸發 Buff queue，未穩定更新 ReactionSession。
- **結論草案**：採用明確的 Before / After timing hook，例如 `BeforeTurnEnd` / `AfterTurnEnd`、`BeforePlayCardEnd` / `AfterPlayCardEnd`，並建立統一 `RunTiming(GameTiming timing, IActionSource source)` pipeline，讓每個 timing pulse 固定先更新 ReactionSession，再執行對應 Buff trigger queue。
- **設計筆記**：`.agents/working/2026-06-30-trigger-timing-reactor-session-design.md`
- **實作建議**：
  - 先補 EditMode 測試鎖定 WholeTurn / PlayCard session 在 Before / After timing 的讀取與清理行為。
  - 擴充 `GameTiming`，逐步淘汰模糊的 `TurnEnd`、`PlayCardEnd`、`TriggerBuffStart`、`TriggerBuffEnd`。
  - 將 GameplayManager 中屬於 timing pulse 的 session update 收斂到 `RunTiming`。
  - 保留 EffectCommandHandler 對 result action 的 `UpdateReactorSessionAction`，避免 result 類事件失去記憶更新。
- **狀態**：待規劃 / 待實作

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
  - T-005 目前依照「單一 Ally／單一 Enemy CharacterView」架構，分別以 `SerialDisposable` 管理最新角色的動畫生命週期；實作多角色時，必須改為以角色 Identity 管理 `CharacterView + IDisposable` 的動態集合
  - 多角色 Summon 不可讓多條動畫 loop 共用同一個 CharacterView；應由 CharacterView Factory／物件池建立獨立 View，角色移除時按 Identity Dispose scope 並回收 View，戰鬥結束時再統一清理所有剩餘角色
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
T-003（Effect Queue，已完成）
  ↓
T-008（資料驗證，已完成第一階段） + T-006（決定性亂數，已完成） + T-005（生命週期）
  ↓
T-011（多步驟選取）
  ↓             ↓              ↓
T-007（asmdef / 命名空間整理，分階段穿插）
  ↓
T-014（預演管線） T-013（敵人動態增減） T-012（卡片合成）
```

- T-001 是基礎設施改善，先做會讓後續所有功能的開發更輕鬆
- T-004 已完成，提供 Buff Timing 與 GameContext scope 的測試保護；T-003 已沿用既有測試 builder 擴充 queue 行為案例
- T-003 已完成第一版 Effect Queue，後續若要支援效果取消 / 替代，應等具體卡牌需求或 Preview / Simulation 管線明確後再擴充
- T-008 已完成第一階段，以 EditMode 測試持續保護資料與 registry 驗證；Editor menu 可等設計資料調整頻繁時再補
- T-006 已完成決定性亂數基礎，後續重播、AI simulation 與問題重現可沿用 `IGameRandom`
- T-007 建議作為下一個優先項，先盤點 Runtime / Editor / Tests assembly 與命名空間邊界，再分階段整理，避免大規模搬遷造成噪音
- T-005 仍重要，但較偏場景生命週期與非同步穩定性；若目前沒有切場景或 `.Forget()` 相關 bug，可排在 T-007 之後
- T-010 和 T-011 互相獨立，可以平行開發
- T-012 依賴 T-010 + T-011 的基礎
- T-013 相對獨立但影響面廣，建議架構穩定後再動
- T-014 屬於中長期能力建設，先做輕量 preview 即可，完整 simulation 可等 T-001 / T-003 方向穩定後再投入
