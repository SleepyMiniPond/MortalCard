# AI 工作記憶

> 最後更新：2026-06-24

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
