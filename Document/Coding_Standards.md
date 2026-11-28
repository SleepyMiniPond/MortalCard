# 編程規範

> 更新日期：2026-11-29
> 本文件是開發準則；不代表既有程式已全面符合。現況以對應程式及系統文件為準。

## 技術與命名

- Unity 非同步使用 UniTask，將取消 Token 沿必要工作傳遞，擁有者須監督並等待清理。
- 可觀察狀態與事件流使用 UniRx，訂閱生命週期隨擁有者結束；明確的建構／協調依賴仍可直接注入。
- 可選值優先使用 Option。外部輸入、Unity 序列化與 Validator 邊界仍需必要的 null／有效性檢查。
- 資料傳遞優先 Record；唯讀集合使用唯讀介面，但兩者皆不自動保證深度不可變。
- Inspector 配置使用 Odin 支援設計師工作流程，遊戲數值放在內容配置。
- 私有方法採 _PascalCase，私有欄位採 _camelCase，其餘依 C# 慣例。
- 共用與卡牌列舉分別參考 GameEnum、CardEnum，領域專用列舉置於所屬模組，不強制集中至單一檔案。
- 所有新文件、註解及說明使用台灣繁體中文。

## 職責分工

Model 管理規則，View 負責呈現與互動，Presenter 協調；共享契約放在 Presentation Abstractions。Data 保存配置，Property／LifeTime／Session 實體由 GameModel Factory 建立。

UI Panel 預設採用 State＋Session：Open 接收 IReadOnlyReactiveProperty 的顯示狀態，回傳包含操作 Observable 的 IDisposable Session，由 Presenter 持有並釋放。完整責任、清理規則、固定內容例外與既有面板遷移原則見 [Panel 慣例](Presenter.md#panel-的-state-與-session-慣例)。UI 開窗 Session 由 View 建立，與 GameModel Factory 的領域 Session 不同。

GameData 與 GameModel 目前仍同屬 Runtime，可執行的資料查詢仍有 Runtime 依賴，見 [架構總覽](SystemArchitecture.md)。資產製作遵循 [GameData 規範](GameData_Asset_Guidelines.md)。

## 數值責任

通用 Value／算術支援整數與缺值，不套用傷害或 Buff 領域限制。Resolver 判斷數值是否符合效果語意；Entity／Manager 維護最終 HP、護盾、能量及層數範圍。

Buff Level 不低於 0，存在 MaxLevel 時亦不超過上限；0 層 Buff 仍存在，不自動移除。非法效果不得以反向狀態變更或假的 Result／Event 表達。詳見 [Value](Value.md)。

## Validator 與 Runtime

| 情境 | 責任 |
|---|---|
| Library ID、Operation、Condition 等必要企劃資料缺失 | Validator 提前報錯，避免核心流程層層掩蓋資產錯誤 |
| 排隊後目標、來源區域或 Buff Layer 消失 | Runtime 正常失效，安全 No-op／Rejected，不產生假事件或動畫 |
| 存檔、網路、玩家輸入等外部資料 | 輸入邊界驗證，不能假設已受專案 Validator 保證 |
| 兩份相同語意輸入需要反覆核對 | 優先消除重複資料來源 |

驗證與 Runtime 不互相取代：前者排除製作錯誤，後者處理正常遊戲時序。

## 例外與診斷

- 新增或修改自訂錯誤時，必須先重用或定義語意明確的專門 Exception，再於使用端建立該型別。禁止在業務腳本直接使用 new Exception("訊息")、new InvalidOperationException("訊息") 或其他標準例外加上自訂字串／插值／串接文字。訊息由專門 Exception 組裝，使用端只傳必要的 Type、Guid、數值、路徑或驗證結果；一般標準參數、取消及釋放例外依下方規則保留。
- 專門 Exception 保存必要的唯讀診斷屬性，不接收已組裝好的任意訊息，不建立全域 ExceptionManager 或萬用參數字典；同一語意先找既有型別，避免為不同使用端重複定義。
- 相同錯誤語意共用型別，獨立的領域契約使用明確型別。按領域收納短例外，不要求一型別一檔案；Runtime 例外位於 Assets/Scripts/Exceptions、namespace MortalGame.Exceptions，Editor 專用例外位於 Assets/Scripts/Editor/Exceptions、namespace MortalGame.Editor.Exceptions，沿用各自既有 assembly。
- 基底反映原有語意：狀態契約用 InvalidOperationException，非法參數保留 ArgumentException／ArgumentOutOfRangeException 與 ParamName／ActualValue，不支援、尚未實作及 Build 中止各保留相應分類。一般 null／disposed／無自訂敘述的參數例外直接使用標準型別。
- GameEndException、CardPlayChainHaltedException 與取消有各自控制用途；整理訊息不得改變 throw／return、捕捉、取消或等待清理的順序。向上傳遞既有錯誤時保留原例外及 StackTrace，不重新包裝。
- 需要診斷資料的正常操作拒絕，以所屬領域的具名原因資料表達，必要 ID 隨原因保存，文字由原因定義集中產生；使用端不傳任意原因字串，也不依文字判斷流程。保留 Rejected／NoOp，不因集中診斷而改為拋出例外。
- Runtime Log 與手寫追蹤路徑由所屬領域的具名診斷方法集中組裝，呼叫端只傳必要資料；不使用全域字串表或任意訊息參數。搬移時保留 Log 等級、輸出次數與 fallback／執行順序，既有相容性訊息可維持原文字。
- Validator 回傳具名的 ValidationIssue，保存必要的 Context、ID、索引、列舉或 Type；完整診斷由各領域問題定義的 Message 組裝，依領域合併收納檔案。驗證規則只建立問題資料，Menu／Play Mode 的呈現集中於 ValidationDiagnosticReporter，Build 例外保存問題清單副本。保留批次收集、順序、既有依訊息去重與 Gate 的阻擋規則，不以逐筆 throw 或任意字串取代問題資料。
- Exception 訊息用於開發診斷；玩家提示與正常 No-op／Rejected 沿用所屬流程。驗證錯誤清單由例外保存副本，避免外部修改使診斷內容失去一致性。
- NUnit 的 Assert.Throws<T>／Is.TypeOf<T> 會精確比較型別，遷移時同步檢查專門型別與必要屬性，少量確認訊息包含識別即可。新增測試替身須核對 Validator 的型別掃描範圍，避免未註冊的測試命令阻擋 Play Mode。

## 變更驗證

修改 Assets/Scripts 下的 C# 後，依專案指令透過 Unity 工具刷新（新檔時）、重新編譯並修正至 0 error；warning 應回報。測試依行為風險選擇，避免只重述實作。不要主動 stage 或 commit。
