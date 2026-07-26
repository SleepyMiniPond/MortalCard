# AI 工作記憶

> 最後更新：2026-07-27

## 文件與草稿偏好

- 使用者偏好將「製作中的設計草稿、分析草稿、短期工作筆記」與正式專案文件分離。
- 臨時草稿請放在 `.agents/working/`，不要放進 `Document/`。
- `.agents/working/` 的內容不需要永久留存，也不應納入 git。
- `Document/` 只放正式、值得長期維護的專案文件。
- 若需要讓未來 AI 也知道長期協作偏好，優先更新本檔與 `AGENTS.md`。

## Git 工作偏好

- 不要主動 stage 或 commit 修改，除非使用者明確要求。
- 使用者會一邊檢查 change，一邊自行把看完的內容加入 stage。
- 若發現檔案已 staged，不要自動 unstage；視為使用者已檢查或正在整理。
- 完成工作時只回報變更內容、驗證結果與目前 git 狀態。

## 目前工作脈絡

- 專案已加入 asmdef 基礎：`MortalGame.Runtime`、`MortalGame.Editor`、EditMode Tests、PlayMode Tests。
- asmdef 後的建議下一步是先補核心 EditMode 測試基礎，而不是急著切更多程序集。
- 第一批測試應優先保護 Effect Resolver / Command Handler registry，以及後續 T-003、T-015 會碰到的核心契約。

## Unity 驗證偏好

- 本專案需要 Unity 驗證、AssetDatabase refresh、編譯檢查或 EditMode/PlayMode 測試時，優先使用 Unity MCP，連到使用者已開啟的 Unity Editor。
- 若原生 MCP tool 沒有直接暴露，改用 `unity-mcp-cli run-tool ...` 呼叫 MCP，例如 `unity-mcp-cli run-tool tests-run --input-file <json>`。
- 不要優先使用 Unity `-batchmode` 開第二個 Editor；本專案常態會有 Unity Editor 開著，batchmode 容易被 project lock 擋住。
- 只有在 MCP 不可用、使用者明確同意、或需要驗證 MCP 本身不可用時，才把 batchmode 當 fallback，且必須清楚回報原因。

## Runtime 與 Validator 程式風格

- Runtime 只處理遊戲機制中合理可能發生的失效，例如排隊中的後續 Effect 因前一個 Effect 已移除目標而找不到 Buff；這類情況應安靜 No-op／Rejected。
- Library、CardData、必要 ID、Operation、Condition 等企劃資產缺失，應由 Validator 在執行前排除；Runtime 核心邏輯不要重複堆疊 null 或缺少資料的防禦判斷。
- 若檢查源自同一語意有兩份輸入，優先改為單一資料來源，讓 Script 直接呈現遊戲流程。
- 玩家輸入、存檔與網路資料等未經專案 Validator 保證的外部邊界，仍需正常驗證。
