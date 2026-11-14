# GameData 資料資產製作規範

> 最後更新：2026-11-14 | v1.4

## 文件目的

本文件記錄 MortalGame 中 ScriptableObject 資料資產的基本製作與維護規則，適用於卡牌、Buff、牌組、玩家與敵人等遊戲資料。

這是一份持續維護的實務規範。初期只記錄已確認且能實際執行的原則；日後遇到新的資料問題，再將解法補充到此文件。

## 基本規則

- 遊戲資料資產原則上放在 `Assets/ScriptableObjects/` 對應分類目錄下。
- 卡牌、CardBuff、PlayerBuff、CharacterBuff 資產由 `GameContentCatalog` 統一收錄；新增、刪除或移動這些資產後，先執行 `MortalGame/遊戲內容/重新產生內容目錄`。
- `AllPlayerScriptable` 只負責 Ally／Enemy 配置，`ExcelDatas` 只負責外部表格資料，不要把它們當成 Card／Buff Catalog 使用。
- 資料 ID 必須保持唯一且不可為空；修改既有 ID 前，必須確認所有引用端都已同步更新。
- 新增 Effect 或 Command 型別時，必須確認對應 Resolver 或 Handler 已完成註冊。
- 新增 ScriptableObject 類型、ID 引用欄位、巢狀資料結構，或變更資產存放位置時，必須確認 `GameDataValidator` 的搜尋範圍與驗證規則仍然完整。
- 不確定欄位是否允許為空時，先查閱對應系統文件或實際建立流程，不自行假設預設行為。

## 多步驟選取製作

- 目前只配置 ExistCard 群組，依 SubSelects 清單順序排列；ID 在同張卡內保持非空且唯一，修改 ID 時同步調整相依集合、效果與專屬提示文字。
- 每步先決定候選與 SelectCount，再依 [數量規則](Target.md#數量與候選不足) 選擇 CountRule。必選不代表不能取消 UI；關閉任一步會取消整次出牌。
- 後一步可用 SubSelectedCardCollection 讀取前面群組；需要排除已選卡片時，使用 CombinedCardCollection 的 Difference 或明確條件。不要引用自身或後面的群組。
- UI 專屬提示使用 CardData ID 與群組 ID 組成文字 key，規則見 [GameData](GameData.md#子選取配置)。本地化表有通用驗證，不依賴資產上的 PromptKey。

例如第一步 `pick` 從卡片擁有者的手牌選一張，第二步 `confirm` 的候選用 SubSelectedCardCollection 引用 `pick`，需求設為 1、CountRule 設為 RequiredExact。效果再引用 `confirm`，即可確認兩步結果的傳遞；只確認第一步不應提前移牌或執行效果。

## 臨時功能驗收資產

任務進入最後驗收時，依功能評估是否適合用單一功能卡片或最小資產直接驗證。適合時由 AI 批次製作、接好可進入 Play 的配置，減少使用者手動製卡的時間；純內部邏輯或無法透過卡片觀察的功能，沿用合適的自動化或其他驗證方式。

- 每張卡聚焦一項功能或邊界，例如成功、取消、零數量、候選不足、前步相依、不同呼叫入口；數值以容易觀察為主，不考慮平衡及正式效果文案，只保留可辨識名稱與 Validator 必要欄位。
- 測試卡、牌組、必要的玩家／敵人配置、本地化副本及入口集中在獨立臨時資料夾，使用唯一 ID；正式資產不得反向引用測試內容。
- 優先以正式掃描範圍外的資料夾及獨立 Catalog 隔離；獨立 Catalog 可引用正式內容。這是臨時驗收用途，不改變正式資產的存放規範。自訂配置仍須另外執行相應 Validator，不因未被預設 Gate 掃描而省略驗證。
- 控制起手牌、能量、生命與敵方配置，讓目標功能容易觸發；若沿用亂數而無法保證固定結果，需說明可變部分，避免用固定張數或畫面數字作錯誤判定。
- 提供入口、操作、預期結果與異常判準，區分自動化已驗證、AI 基本 Play 檢查及仍需使用者確認的內容，並包含停止／離開期間的清理檢查。
- 驗收完成後清除臨時資產及其 .meta，保留正式修正與有價值的回歸測試；刪除前確認外部引用，透過 Unity 刪除並刷新、驗證。若曾加入正式 Catalog 或配置，先清除那些引用並重建目錄。
- 不以 .gitignore 代替清理，因為 Unity 仍會匯入被 Git 忽略的資產。若需要長期保留範例，應另行確認其正式用途與維護責任。

## 修改後檢查

完成資料資產的新增或修改後：

1. 若變更 Card／Buff 資產，先執行 `MortalGame/遊戲內容/重新產生內容目錄`。
2. 在 Unity 執行 `MortalGame → 驗證遊戲資料`，修正 Console 中的資料驗證錯誤。
3. 涉及資料結構或驗證規則變更時，執行 `ScriptableObjectDataValidationTests`。
4. 確認受影響的卡牌或 Buff 能在實際流程中正確載入與顯示；按下 Play 或建置時也會由 Gate 重複驗證。

## 維護原則

- 優先記錄真實發生過、容易重複出現的問題。
- 規則應說明「需要檢查什麼」，避免堆疊過多實作細節。
- 若規則已可由 Validator 自動檢查，文件仍保留簡短原則，詳細錯誤交由工具回報。
- 若資料架構發生大幅調整，應同步更新本文件與 [GameData.md](GameData.md)。

## 程式入口

[目錄編譯](../Assets/Scripts/Editor/GameContentCatalogCompiler.cs)、[資產搜尋範圍](../Assets/Scripts/Editor/ProjectAssetPaths.cs)、[Validator](../Assets/Scripts/Editor/GameDataValidator.cs)、[Play Mode Gate](../Assets/Scripts/Editor/GameDataPlayModeGate.cs)、[Build Gate](../Assets/Scripts/Editor/GameDataBuildPreprocessor.cs)。
