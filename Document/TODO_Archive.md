# 已完成任務封存

> 整理日期：2026-09-19
> 以下完成日期與驗證數字沿用各次任務紀錄，本輪未重新執行測試；目前行為以系統文件及程式為準，待辦見 [TODO](TODO.md)。
> 完成狀態限於各任務範圍，不代表所有預留列舉、流程入口或後續整合都已完成。

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

以戰鬥種子建立並注入 IGameRandom，洗牌與既有子選取使用同一服務。這不保證每個名為 Random 的選取分支已使用亂數；目前主目標 ToRandom 仍取第一個，於 T-022 共用選取工作處理。見 [GameModel](GameModel.md)。

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

## 現況導引

測試來源：[EditMode](../Assets/Tests/EditMode/)、[PlayMode](../Assets/Tests/PlayMode/)。T-007 當時「PlayMode 無案例」僅是歷史狀態，目前已有 CharacterAnimationWorkerTests。

歷史驗證中的工具、警告與測試數量不可當成最新結果；需要確認執行狀態時，依受影響範圍重新驗證。
