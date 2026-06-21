# MortalGame asmdef 與測試邊界設計

> 設計日期：2026-06-22

## 目標

為現有 Unity 專案建立可被 Unity Test Framework 引用的程序集邊界，包含 Runtime、Editor、EditMode Tests 與 PlayMode Tests；本階段不拆分業務模組，也不新增測試程式。

## 架構

```text
Assets/
├── Scripts/
│   ├── MortalGame.Runtime.asmdef
│   └── Editor/
│       └── MortalGame.Editor.asmdef
└── Tests/
    ├── EditMode/
    │   └── MortalGame.EditModeTests.asmdef
    └── PlayMode/
        └── MortalGame.PlayModeTests.asmdef
```

### MortalGame.Runtime

- 涵蓋 `Assets/Scripts/` 下除 `Editor/` 外的所有現有程式。
- 暫時不拆分 GameData、GameModel、Presenter、GameView、UI 與 Scene。
- 原因是 GameData 目前會直接建立 GameModel Entity；過早拆分會產生程序集循環。
- 第三方程序集引用以實際 Unity 編譯需求為準，只加入現有程式確實使用的依賴。

### MortalGame.Editor

- 只包含 `Assets/Scripts/Editor/`。
- 僅限 Editor 平台。
- 引用 `MortalGame.Runtime`。

### MortalGame.EditModeTests

- 啟用 Unity Test Assemblies。
- 僅限 Editor 平台。
- 引用 `MortalGame.Runtime`。
- 後續放置數值、Entity、Effect、Buff 與回合邏輯等快速測試。

### MortalGame.PlayModeTests

- 啟用 Unity Test Assemblies。
- 引用 `MortalGame.Runtime`。
- 後續放置 MonoBehaviour、Prefab、UI、場景與 Presenter 整合測試。

## 範圍限制

- 不修改任何既有 `.cs` 檔案。
- 不重構命名空間或資料夾。
- 不新增測試案例。
- 不拆分 GameData 與 GameModel。
- 不處理既有未提交變更。

## 驗證方式

1. 新增四個 asmdef 後執行 Unity `Assets/Refresh`。
2. 觸發 Unity Script Recompile。
3. 若有第三方程序集缺失，只補充編譯錯誤明確指出的引用。
4. 重複編譯至 0 error。
5. warning 不阻擋完成，但必須回報。
6. 確認 Git 差異只包含本次規格與四個 asmdef，既有使用者變更維持原狀。

## 成功條件

- Unity 能辨識四個程序集。
- Runtime 與 Editor 程式編譯成功。
- EditMode 與 PlayMode 測試程序集可被 Test Runner 載入。
- Unity 編譯結果為 0 error。

