# MortalGame 專案架構分析基線

> 分析日期：2026-06-22  
> 分析範圍：`Assets/Scripts/`、場景建置設定、套件設定、既有 `Document/` 文件與 `TODO.md`  
> 判讀原則：以目前程式碼為準；既有文件視為設計意圖，不直接視為實作事實

## 一、結論摘要

MortalGame 是一個以 Unity 6 開發的回合制卡牌戰鬥原型。整體結構最接近 **MVP 式場景協調架構**，搭配 **Data → Instance → Entity** 的資料生命週期，以及 **Command → Action → Effect Command → Event** 的戰鬥處理鏈。

目前架構已具備可擴充卡牌遊戲所需的重要語彙：資料模板、戰鬥實體、目標解析、組合條件、效果解析器、命令處理器、Buff 觸發時機、反應 Session、事件式 View 更新及物件池。T-001 與 T-002 的改造也已讓效果與 Buff 管線比舊文件描述更完整。

不過，專案現在仍是「有清楚設計方向的單體原型」，尚未形成可由編譯器與測試保護的模組化架構。最主要的結構風險如下：

1. 243 個主程式腳本集中在單一 `Assembly-CSharp`，且幾乎全部位於全域命名空間。
2. 沒有專案自有自動化測試，核心效果、Buff、回合流程與 UI 事件契約缺少回歸保護。
3. `GameplayManager`、`GameplayView` 與手牌 View 已成為大型協調類別，新增功能容易擴大分支與耦合。
4. 文件把「設計意圖」寫成「已完成保證」：GameData 並非純資料層、Model 並非 Unity 無關、事件系統也尚非事件溯源。
5. 戰鬥生命週期、亂數決定性與事件資料重複仍有實際風險，且與 T-003、T-014、T-015 直接相關。

因此，下一階段不宜立刻全面重構。較穩健的路線是先補測試接縫與生命週期控制，再處理 Effect Queue、Preview／Simulation 與動態敵人等高影響功能。

## 二、專案實況快照

| 項目 | 現況 |
|------|------|
| Unity | 6000.0.3f1 |
| 核心腳本 | `Assets/Scripts/` 共 243 個 `.cs` |
| 最大模組 | GameModel 109、GameView 65、GameData 44、Presenter 10 |
| 場景 | Main、Menu、Gameplay、LevelMap |
| 專案程序集 | 無自有 asmdef，主要集中在 `Assembly-CSharp` |
| 命名空間 | 除 Record 相容性宣告外，主程式幾乎全為全域命名空間 |
| 自動化測試 | 未發現專案自有 EditMode／PlayMode 測試 |
| 主要套件 | UniTask、UniRx、Odin Inspector、Optional、OneOf、URP、Input System |
| 資料來源 | ScriptableObject、Excel Importer 產生的資料、執行時 Dictionary Library |
| 建置檢查 | `dotnet build MortalCard.sln --no-restore`：0 error、5 warnings |

建置警告中，3 個來自內嵌 UniRx 對 Unity 舊 API 的使用；2 個來自 `GameplayManager.OneTurnStart` 與 `OnTurnEnd` 未被使用。

## 三、實際架構模型

### 3.1 巨觀分層

```text
Main / SceneLoadManager
        ↓ 場景生命週期
Scene 元件
        ↓ 建構與啟動
Presenter ─────────────→ GameView / UI
    ↓ 命令轉譯              ↑ 事件與 ViewModel
GameModel ─────────────→ IGameEvent
    ↕
GameData / Library / Instance
```

這不是嚴格的教科書 MVP，而是 **MVP 式的 Unity 單體架構**：

- Scene 是 Composition Root，負責尋找 Unity 元件並建立 Presenter。
- Presenter 同時持有 Model、View 與 UI 子 Presenter，是主要協調邊界。
- Model 不直接引用具體 View 類別，但事件中會攜帶 Entity 與 View 用 Info，且大量依賴 UnityEngine。
- View 接收事件並更新 `GameViewModel`，同時透過 UniRx 讓子 View 響應狀態。
- GameData 名義上是底層資料，但部分 Data 直接建立 Entity，形成 Data ↔ Model 的實作耦合。

因此，文件可繼續使用「MVP」作為溝通模型，但應標明它是方向而非強制邊界。

### 3.2 資料生命週期

```text
ScriptableObject / Excel
        ↓ ScriptableDataLoader
Context 中的 Dictionary Library
        ↓ BattleBuidler
CardData → CardInstance → CardEntity
PlayerData → AllyInstance → AllyEntity
EnemyData ───────────────→ EnemyEntity
BuffData ────────────────→ BuffEntity
```

三層資料架構在卡牌與友軍玩家上成立，但不是全域一致規則：Enemy 與 Buff 沒有 Instance 層。這是合理的 YAGNI 取捨，不應為了形式完整而補空泛抽象。

目前 `AllyInstance` 雖被描述為存檔快照，仍包含 `List<CardInstance>`，且沒有實際序列化、版本遷移或戰後回寫流程；應把它稱為「可持久化邊界的雛形」，而不是已完成的存檔系統。

### 3.3 戰鬥控制流

```text
Main._Gameloop
  → MenuScene.Run
  → LevelMapScene.Run
  → GameplayScene.Run
  → BattleBuidler 建立 Library、Context 與 GameStageSetting
  → GameplayPresenter.Run
      ├─ GameplayManager.StartBattle
      ├─ UI / Command 處理迴圈
      └─ IGameEvent 批次交給 GameplayView.Render
```

戰鬥狀態機由 `GameplayManager` 集中驅動：GameStart → TurnStart → DrawCard → EnemyPrepare → PlayerExecute → EnemyExecute → TurnEnd。玩家輸入經 `GameCommand` 轉成 `IGameAction`；Model 則把狀態異動轉成 `IGameEvent`，由 View 批次消費。

這個中心化流程容易追蹤，但 `GameplayManager` 已達約 693 行，同時負責狀態機、輸入佇列、建構戰鬥實體、卡牌打出、Buff 遞迴觸發、事件聚合與勝負判定，職責正在超出單一類別可安全承載的範圍。

### 3.4 效果與 Buff 管線

目前效果鏈可概括為：

```text
ICardEffect / IBuffEffect
  → EffectDataResolver registry
  → EffectCommandSet
  → EffectCommandExecutor registry
  → Entity / Manager 狀態變更
  → Result Action + IGameEvent
```

T-001 已將大型 type-switch 改為 Resolver／Handler registry，擴充點比舊架構更清楚。然而 registry 仍是集中式靜態註冊；新增效果雖不必修改執行器主體，仍須記得手動註冊。未知效果目前只記錄警告並回傳空命令，容易讓錯誤資料以「沒有作用」的形式進入遊戲。

T-002 已接通 PlayerBuff、CharacterBuff 與 CardBuff 的觸發巡覽，但 CardBuff Effect registry 目前仍為空。這表示基礎管線已接線，不代表已有可運作的 CardBuff 主動效果種類。

`_TriggerTiming()` 會在 Buff 完成後遞迴觸發 `TriggerBuffEnd`。此設計能表達連鎖反應，但目前沒有深度限制、循環偵測或 Effect Queue，因此互相觸發的 Buff 可能造成深層遞迴或無限鏈。這正是 T-003 應優先解決的核心原因。

### 3.5 View 與事件模型

`GameplayView.Render()` 是 View 事件分派中心，`GameViewModel` 則保存可觀察的 Card、Buff、區域及好感度資訊。這種「事件通知＋ReactiveProperty 狀態」的混合方式適合 Unity UI，但目前存在兩個真相來源：事件攜帶的快照，以及 GeneralUpdate 產生的完整狀態。

因此，現況應稱為 **事件驅動的 View 同步**，不宜稱為事件溯源：

- 事件沒有完整保存、版本、序號或重播機制。
- 部分事件直接攜帶可變 Entity。
- `GameHistory` 目前只是空殼。
- 無法只靠事件可靠重建 GameStatus。

T-015 對 CardInfo 重複建立與事件冗餘的判斷正確，且是建立 Preview／Simulation 前值得先整理的資料契約問題。

## 四、值得保留的設計

1. **遊戲語彙清楚**：Action、Effect、Command、Result、Event、Timing、Source、Target 的概念能描述複雜卡牌互動。
2. **組合優於繼承**：Player、Character、Card 由多個 Manager 與 Property 組合，適合逐步擴充。
3. **Data／Instance／Entity 的務實分離**：至少在卡牌身份與戰鬥狀態上建立了正確邊界。
4. **Option 與 Record 的使用方向正確**：可選目標、不可變動作與事件比任意 null／可變 DTO 更安全。
5. **Presenter 作為 Unity 接線層**：大多數核心流程不必直接掛在 MonoBehaviour 上。
6. **效果解析與執行分離**：為 T-014 的 Preview／Simulation 保留了可利用的接縫。
7. **PrefabFactory 具備回收池**：動態 View 可重用，適合大量卡牌與戰鬥數字動畫。

## 五、主要風險與建議

### P0：先建立回歸保護

目前沒有專案自有測試，而下一批 TODO 都會改動核心管線。至少應先建立 EditMode 測試程序集，覆蓋：

- 傷害、治療、護甲、能量及卡牌區域移動。
- Resolver 與 Handler registry 的完整性。
- Player／Character／Card Buff 的 Timing、條件與生命週期。
- 一回合狀態機與勝負判定。
- 固定亂數種子下的抽牌與 AI 選牌結果。

### P0：修正非同步生命週期

`GameplayPresenter._GameplayBattleActions()` 接收取消來源卻未檢查 Token，且以 `.Forget()` 啟動無限迴圈。戰鬥結束後即使呼叫 `Cancel()`，該迴圈仍可能持續存取已卸載的 View。應讓所有場景級背景工作接受 `CancellationToken`，並在場景結束時可確定地停止及等待收尾。

### P0：建立決定性亂數

`GameStageSetting.RandomSeed` 已存在，但目前沒有用它初始化或注入亂數；洗牌與選取直接使用 `UnityEngine.Random`。因此相同 seed 無法重現戰局，會阻礙測試、戰鬥回放、AI Simulation 與問題重現。建議抽象 `IGameRandom`，由每場戰鬥持有獨立狀態。

### P1：控制大型協調類別

不建議一次把 `GameplayManager` 全面拆散。可依功能演進逐步抽出：

- Turn Flow：只負責階段轉換。
- Card Play Pipeline：只負責打牌與效果執行。
- Buff Trigger Scheduler：由 T-003 的 Queue 承接。
- Battle Event Buffer：集中事件排序、序號與批次消費。

`GameplayView` 也可將事件處理改成型別 Handler registry，避免與新增事件數量線性成長。

### P1：讓模組邊界可被編譯器驗證

短期先導入命名空間，降低全域型別碰撞；中期再以 asmdef 分離 Runtime、Editor 與 Tests。不要立刻依資料夾硬切 GameData／GameModel，因為目前 Data 直接建立 Entity，會形成程序集循環。應先決定工廠責任要留在 Data 還是移到組裝層，再切程序集。

### P1：收斂事件資料契約

優先完成 T-015：區分「動畫事件」與「狀態失效通知」，避免每個命名事件都攜帶完整快照。事件可攜帶 identity、delta 與必要動畫資訊；ViewModel 的最新狀態則由單一路徑更新。

### P2：補齊流程與資料錯誤處理

- `BattleBuidler` 固定使用 `StageTest` 與第一個敵人，LevelMap 尚未真正傳遞關卡選擇。
- `LoadingScene` 有載入 API，但不在 Build Settings，且目前沒有實際流程使用。
- 未知 Effect 只警告並忽略；開發環境宜在資料驗證階段直接失敗。
- Main、LevelMap 與戰鬥結果的 restart／retry 狀態以巢狀布林控制，後續增加流程時容易產生黏住的狀態，宜改成明確流程結果或狀態機。
- 專案名稱在 MortalCard 與 MortalGame 間不一致，應決定正式名稱以避免工具、文件與產品識別分裂。

## 六、與現有 TODO 的關係

| TODO | 架構判讀 | 建議 |
|------|----------|------|
| T-001 | 已完成主要重構；registry 仍為手動集中註冊 | 補 registry 完整性測試與資料驗證即可 |
| T-002 | 觸發巡覽已完成；CardBuff 效果種類仍待定義 | 保留完成狀態，但在文件註明「管線完成」 |
| T-003 | 是連鎖 Buff、取消／替代與 Preview 的共同基礎 | 提升為下一個核心架構工作 |
| T-010 | CardEntity 已有變身雛形 | 先明確定義基礎值、暫時修正與 Buff 的保留規則 |
| T-011 | 現有 SubSelection 可作起點 | 先把選取流程建模成資料，再擴充 UI |
| T-012 | 明確依賴 T-010、T-011 | 執行順序合理 |
| T-013 | 會影響 Entity 集合、目標解析、勝負與 View | 應等測試接縫與事件契約穩定後再做 |
| T-014 | Resolver／Command 分離提供良好起點 | 先做純 Preview；完整 Simulation 需決定性亂數與可複製狀態 |
| T-015 | 問題判斷正確且影響 View 效能與一致性 | 建議移到 T-003／T-014 之前或同步進行 |

建議在 `TODO.md` 後續新增下列基礎項目；本次不直接修改，避免覆蓋目前工作區中的既有未提交內容：

- T-004：建立核心 EditMode 測試與測試資料建構器。
- T-005：修正場景級 UniTask 取消與 Presenter 生命週期。
- T-006：導入戰鬥專用決定性亂數服務。
- T-007：定義模組命名空間與 asmdef 遷移順序。
- T-008：建立 ScriptableObject 資料驗證與 Resolver／Handler 註冊檢查。

## 七、建議演進順序

```text
測試接縫 + 非同步生命週期 + 決定性亂數
                    ↓
        T-015 事件／ViewModel 契約
                    ↓
             T-003 Effect Queue
              ↙                 ↘
     T-010 / T-011          T-014 Preview
              ↘                 ↙
                  T-012
                    ↓
          T-013 動態戰鬥單位
```

這個順序的核心不是「先把架構變漂亮」，而是先建立能安全改動的保護網，再讓每次新功能自然抽出所需邊界。

## 八、文件校正清單

既有文件有良好的概念整理，但需要以下校正：

- `AI_Notes_Index.md` 記錄約 145 個腳本，實際已增至 243 個。
- `SystemArchitecture.md` 同時出現「六大核心系統」與舊索引的「五大系統」描述。
- 「GameData 是純資料、完全不含邏輯」與 Data 直接 `CreateEntity()` 的實作不符。
- 「GameModel 與 Unity 無關」不成立；Model 多處使用 UnityEngine 與 Unity Random。
- 「事件溯源」應改為「事件驅動 View 同步」。
- 「Instance 可直接作為存檔系統」應改為「存檔邊界雛形」。
- `Effect.md` 尚未完整反映 T-001 後的 Resolver／Handler registry。
- `GameModel.md` 尚未完整反映 T-002 後三類 Buff 的觸發管線。

## 九、精確 QA

**Q：目前是否需要全面重寫？**  
A：不需要。核心遊戲語彙與資料生命週期值得保留，風險主要集中在缺少測試、生命週期、亂數與單體邊界。採漸進式抽取比大爆炸重構更合適。

**Q：現在可以直接做 Effect Queue 嗎？**  
A：可以設計，但實作前至少要有 Resolver、Handler、Buff Timing 與一回合流程測試，否則很難判斷行為改變是新語義還是回歸。

**Q：是否應立即建立多個 asmdef？**  
A：不宜直接硬切。先處理 GameData 建立 Entity 的反向依賴，再以 Runtime／Editor／Tests 為第一階段切分，風險最低。

**Q：Preview 與 Simulation 哪一個先做？**  
A：先做 command-based Preview。完整 Simulation 需要可複製 GameState、決定性亂數、隔離事件與副作用，成本高一個層級。

**Q：這份分析是否等同 Claude `/init`？**  
A：不完全相同。`/init` 偏向建立 AI 的專案操作指引；本專案已有 `AGENTS.md`。本文件是以實作驗證設計、風險與演進順序的架構基線，範圍更接近 architecture audit。

