# 專案已完成任務封存

> 封存日期：2026-08-19
> 本文件保留已完成任務的設計、實作與驗證紀錄；目前工作請查看 [TODO.md](TODO.md)。
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
  - 可再擴充 localize key、MainTarget/SubSelection 完整性、LifeTimeData 空值與 AllCard/AllBuff 集合覆蓋率檢查；應依實際資料錯誤案例另行增加規則。
- **工具化完成內容**（2026-07-22）：
  - 將測試內的驗證規則抽出為共用 `GameDataValidator`，集中管理 Handler、Resolver 與跨 Library ID 引用檢查。
  - `ScriptableObjectDataValidationTests` 改為呼叫共用 Validator，避免測試與人工驗證各自維護一套規則。
  - 新增 Unity Editor 選單 `MortalGame/驗證遊戲資料`；成功時顯示確認訊息，失敗時將各項錯誤輸出至 Console。
  - 新增 `GameData_Asset_Guidelines.md`，記錄資料資產製作、Validator 維護與修改後檢查原則。
- **工具化驗證結果**：
  - Unity AssetDatabase refresh：0 compile error
  - `ScriptableObjectDataValidationTests`：3 passed / 0 failed / 0 skipped
- **影響檔案**：`GameDataValidator.cs`、`GameDataValidationMenu.cs`、`ScriptableObjectDataValidationTests.cs`、`GameData_Asset_Guidelines.md`
- **狀態**：✅ 已完成（2026-07-23）

---

## 🟣 後續架構討論：TriggerTiming / ReactorSession 時機一致化

### T-016：重構 TriggerTiming 與 UpdateReactorSessionAction 的時機模型
- **問題**：目前 `UpdateReactorSessionAction` 分散在 GameplayManager 主流程、EffectCommandHandler、Triggered*BuffEffectQueueItem，導致 timing session 更新順序不一致；特別是 `TriggerBuffEnd` 目前可能只觸發 Buff queue，未穩定更新 ReactionSession。
- **採用方案**：使用明確的 Before / After timing hook，並建立統一 Timing Pipeline，讓每個 timing pulse 固定先更新 ReactionSession，再執行對應 Buff trigger queue。
- **設計筆記**：`.agents/working/2026-06-30-trigger-timing-reactor-session-design.md`
- **完成內容**：
  - `TriggerTimingQueueItem` 統一執行「ReactionSession update → Buff 條件掃描 → Effect Queue」，包含遞迴 Buff timing。
  - 新增 Turn、DrawCard、Execute、PlayCard、TriggerBuffEffect、CharacterSummon、CharacterDeath 的 Before / After timing hooks。
  - 新增 `CardPlayIntent` / `CardPlayResult`，分離出牌 Action 與流程 hook 的 timing 語意。
  - GameplayManager 的 Turn、DrawCard、Player / Enemy Execute、PlayCard 流程改用新 hooks；`CardPlayResultAction` 會在 `BeforePlayCardEnd` 前更新 Session。
  - WholeTurn Session 改為 `BeforeTurnStart` reset、`AfterTurnEnd` clear；PlayCard Session 改為 `BeforePlayCardStart` reset、`AfterPlayCardEnd` clear。
  - PlayerBuff、CharacterBuff、CardBuff 的回合生命週期改由 `AfterTurnEnd` 扣除。
  - Buff effect 拆成同一 Effect Queue 內的 `BeforeTriggerBuffEffect → Effect → AfterTriggerBuffEffect`，保留深度優先順序、最大處理數保護及 Card / Character selected context。
  - `EnqueueImmediate` 新增 `IEnumerable<EffectQueueItem>` overload，呼叫端可依實際執行順序加入子流程；移除所有僅包裝 `Enqueue` 的 Effect 專用 helper。
  - 建立 `GameTimingMigrationTool`，支援 Dry Run、安全項目套用、衝突與人工確認報告、Undo 與資產儲存。
  - 完成 6 個 PlayerBuff ScriptableObject、共 7 個舊 timing 欄位遷移；ComboAttack Session 依實際 Action 語意修正為 `CardPlayResult`。
  - 舊 timing enum 成員已完全刪除；原始 2～10、14～15 數值保留為空洞且不重用，Migration Tool 仍可辨識未定義的舊序列化值。
  - EffectCommandHandler 的 result action Session 更新維持原設計，避免 Damage、Heal、DrawCard 等結果記憶遺失。
- **驗證結果**：
  - Unity AssetDatabase refresh：0 compile error。
  - Migration Dry Run：0 safe / 0 review，已無可識別的舊 timing 資料。
  - GameTiming 序列化與 Migration mapping 測試通過。
  - T-016 Timing Pipeline、Buff Timing、Effect Queue 與 ScriptableObject validation 測試通過。
- **狀態**：✅ 已完成（2026-07-12）

### T-015：CardInfo 氾濫 — 重複製造與事件冗餘
- **問題**：`CardInfo.Create()` 每次都執行 `GameFormula` 計算 + Buff 枚舉 + LINQ 合併，並非輕量操作，但目前存在多個重複製造的路徑：
  1. **Buff 事件與 GeneralUpdateEvent 重複製造同一張卡的快照**：`CardBuffEffectCommandHandler` 在執行 `AddCardBuffEffectCommand` / `RemoveCardBuffEffectCommand` / `ModifyCardBuffLevelEffectCommand` 時，同時產生 CardBuff specific event 的 `CardInfo`，以及 `UpdateReactorSessionAction` → `PlayerEntity.Update()` 所產生的 `GeneralUpdateEvent(CardInfo)`。目前 View 只消費後者，因此前者是未使用的完整快照。
  2. **CardManagerInfo 內無謂計算 PlayingCard**：`DrawCardEvent`、`CreateCardEvent`、`UsedCardEvent`、`PlayerExecuteStartEvent` 等事件都攜帶 `CardManagerInfo`，其中包含 `PlayingCard.ToInfo()`，但 PlayingCard 在這些事件的當下實際上並未改變
  3. **CardBuff 事件攜帶完整 CardInfo 但不必要**：`AddCardBuffEvent` / `RemoveCardBuffEvent` / `ModifyCardBuffLevelEvent` 的目的只是通知某張卡的 Buff 列表有變，卻帶了包含 Cost/Power 計算的完整 CardInfo
- **方向**：
  - **核心原則**：命名事件只描述「發生了什麼」，不負責搬運未被消費的完整資料快照；畫面狀態仍由既有 `GeneralUpdateEvent` 更新，若未來 specific event 需要畫面資料，再依 Identity 從 `GameInfoModel.ObservableCardInfo()` 取得最新值。
  - `AddCardBuffEvent` / `RemoveCardBuffEvent` / `ModifyCardBuffLevelEvent` 改為只攜帶 `Faction` + `Guid Identity`，不帶 `CardInfo`
  - 評估 `CardManagerInfo` 的 `PlayingCard` 欄位是否真的需要 CardInfo，或改為 `Option<Guid>`
  - 釐清 `GeneralUpdateEvent` 和 CardBuff specific event 在 View 側的職責分工，避免同一更新走兩條路
- **影響檔案**：`GameEvent.cs`、`PlayerCardManager.cs`、`GameplayManager.cs`、`MoveCardEffectCommandHandler.cs`、`GameplayView.cs`、`AllyHandCardView.cs`、`EnemySelectedCardView.cs`、`GameInfoModel.cs`
- **完成內容**（2026-07-20～2026-07-22）：
  - `AddCardBuffEvent`、`RemoveCardBuffEvent`、`ModifyCardBuffLevelEvent` 改為只攜帶 `Faction + CardIdentity`，不再額外呼叫 `CardInfo.Create()`。
  - 確認目前 `GameplayView` 未直接消費 CardBuff specific event；卡片完整快照仍由同批 `GeneralUpdateEvent` 單一路徑更新，避免為 Identity 事件新增第二次 ViewModel 更新。
  - `CardManagerInfo.PlayingCard` 沒有任何消費者，已移除該欄位及其 `CardInfo` 建立成本；`ToInfo()` 也不再需要 `IGameplayModel`。
  - 修正手牌生命週期 CardBuff 僅標記過期但未移除的問題；單張 `CardInfo` 更新時也會同步替換各卡片區域集合內的舊快照，避免棄牌堆詳情顯示過期 Buff。
  - `UsedCardEvent` 與 `MoveCardEvent` 改為只攜帶卡片 Identity；移除出牌與移牌事件建立完整 `CardInfo` 的成本。
  - `DiscardHandCardEvent` 的棄牌與排除牌資料改為 Identity 集合；回合結束時不再為整批手牌建立完整 `CardInfo`。
  - 保留 `DrawCardEvent`、`AddCardEvent`、`EnemySelectCardEvent` 的完整 `CardInfo`，因為接收端仍需要它建立或更新卡片畫面。
- **狀態**：✅ 已完成（2026-07-22）

---

### T-010：卡片變身（保留狀態）

- **目標**：讓戰鬥中的卡片切換 CardData，同時依形態來源保留、暫停或替換指定執行期狀態。
- **完成內容**：
  - 建立 `Base Form < Self Form < External Override` 的 Effective Form 層級；Self Transform 與 Override 都不改變卡片 Identity、區域或順序。
  - Self Transform 由 Standard CardData 的 TransformRule 控制，支援 Priority、Apply／Revert、BattleOnly／Persistent 與 FormChanged Timing。
  - External Override 使用單一可取代 Slot、專用 ReactionSession／ReleaseRule 與 CardBuff Override Layer；第二個 Override 會擠掉第一個，解除後不恢復舊 Override。
  - CardBuff Command 攜帶 LayerHandle；失效 Layer 的排隊命令安全 No-op。Base Buff 在 Override 期間凍結，Override Buff 解除時丟棄，PlayerBuff 仍可作用於目前形態。
  - CardInfo、Presenter 與 View 改為 Identity-based 更新；拖曳、Focus、詳情與選取會重新查詢最新資料並驗證目標。
  - Standard／Override ScriptableObject 完成分型與既有資產遷移；GameData Validator 覆蓋目標型別、必填欄位、ReleaseRule、巢狀 Condition、ReactionSession 與 SessionKey。
  - Clone 只使用當下 Effective CardData 建立獨立 Base Form，不複製 Instance Property、Buff、Self／Override State，也不自動加入 Dispose。
  - 勝利時輸出 `CardInstanceChangeSet`；Lose、Retry、Restart、Quit 與取消不輸出且不修改卡片狀態。戰鬥外實際套用與磁碟存檔不屬於本任務範圍。
- **整合驗證**：A 經 Timing 變成 B、套用 C Override、由 PlayerBuff 在 C 期間加入 Override Buff、Clone C，再解除 C 回到 B；底層 Property／Buff、區域、順序與 CardInfo 均符合規格。
- **驗證結果**：
  - Unity 編譯：0 error。
  - GameData Validator 定向測試：11 項全數通過。
  - T-010 EditMode：74 項全數通過。
  - 完整 EditMode：222 項全數通過。
- **已知邊界**：專案尚無 Gameplay Prefab View 測試 Harness；拖曳中與 Focus 中變形的程式接線已完成，保留正式內容資產下的非阻塞人工 smoke test。
- **正式文件**：[CardTransformation.md](CardTransformation.md)
- **狀態**：✅ 已完成（2026-08-10）

---

### T-018：統一內容目錄與資料驗證

- **目標**：讓 Editor 驗證、Runtime Library 與實際可載入內容使用同一份權威資料來源，避免資產存在卻因手動清單不同步而無法在戰鬥中載入。
- **完成內容**：
  - 建立 `GameContentCatalog`，統一收錄 Card、Override Card、CardBuff、PlayerBuff 與 CharacterBuff 資產。
  - 建立 Editor 掃描、Catalog 編譯與手動選單；搜尋與輸出路徑集中在 Editor 專用的 `ProjectAssetPaths`，不洩漏至 Runtime。
  - Runtime `ScriptableDataLoader` 改由 Catalog 建立各 Library，移除四套舊 `All*Scriptable` 類別與對應資產。
  - Validator 補齊 Catalog 覆蓋率、重複 ID、巢狀必要引用、Target／Value／Condition、跨 Library 引用、Localization、LifeTime 與 Session 語意檢查。
  - 新增 Build Gate；建置前使用同一個 `GameDataValidator.ValidateAll()`，錯誤時停止建置並完整列出原因，不在建置期間偷偷修改 Catalog。
  - 新增 Play Mode Gate；日常按下 Play 時先執行相同驗證，錯誤時取消進入 Play Mode、輸出所有原因並顯示提示。
  - 新增 Catalog 編譯、Runtime 載入、完整內容驗證與 Build Gate EditMode 測試。
- **驗證結果**：
  - Unity 編譯：0 error。
  - 正式內容 `GameDataValidator.ValidateAll()`：通過，0 error。
  - 完整 EditMode：242 passed / 0 failed / 0 skipped。
  - Play Mode Gate smoke test：有效資料可正常進入 Play Mode，並已正常退出。
  - 測試保留 2 筆既有 `NoOpCardBuffEffect` 未知 Resolver warning，屬測試用空效果案例。
- **狀態**：✅ 已完成（2026-08-19）
