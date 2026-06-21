# MortalGame asmdef 與測試邊界 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立 Runtime、Editor、EditMode Tests 與 PlayMode Tests 四個 Unity 程序集邊界，並以 Unity 正式編譯確認 0 error。

**Architecture:** 先以單一 `MortalGame.Runtime` 包住現有執行期程式，避免 GameData 與 GameModel 現有反向依賴形成循環。Editor 與兩種測試程序集只引用 Runtime；本次不修改 `.cs`、不拆分業務模組，也不新增測試案例。

**Tech Stack:** Unity 6000.0.3f1、Unity Assembly Definition、Unity Test Framework 1.4.4、UniTask、UniRx

---

### Task 1：確認基線與程序集依賴

**Files:**
- Verify: `Assets/Scripts/`
- Verify: `Assets/Scripts/Editor/`
- Verify: `Packages/manifest.json`

- [ ] **Step 1：確認四個目標 asmdef 尚不存在**

Run:

```powershell
@(
  'Assets/Scripts/MortalGame.Runtime.asmdef',
  'Assets/Scripts/Editor/MortalGame.Editor.asmdef',
  'Assets/Tests/EditMode/MortalGame.EditModeTests.asmdef',
  'Assets/Tests/PlayMode/MortalGame.PlayModeTests.asmdef'
) | ForEach-Object { "$_ = $(Test-Path $_)" }
```

Expected：四項皆為 `False`。

- [ ] **Step 2：執行修改前 Unity 編譯**

Run：Unity MCP `recompile_scripts(returnWithLogs: true)`。

Expected：0 error；warning 必須記錄。

### Task 2：建立 Runtime 與 Editor 程序集

**Files:**
- Create: `Assets/Scripts/MortalGame.Runtime.asmdef`
- Create: `Assets/Scripts/Editor/MortalGame.Editor.asmdef`

- [ ] **Step 1：建立 Runtime asmdef**

建立以下內容：

```json
{
  "name": "MortalGame.Runtime",
  "rootNamespace": "",
  "references": [
    "UniRx",
    "UniTask",
    "Unity.TextMeshPro",
    "UnityEngine.UI",
    "Unity.VisualScripting.Core"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 2：建立 Editor asmdef**

建立以下內容：

```json
{
  "name": "MortalGame.Editor",
  "rootNamespace": "",
  "references": [
    "MortalGame.Runtime"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

### Task 3：建立 EditMode 與 PlayMode 測試程序集

**Files:**
- Create: `Assets/Tests/EditMode/MortalGame.EditModeTests.asmdef`
- Create: `Assets/Tests/PlayMode/MortalGame.PlayModeTests.asmdef`

- [ ] **Step 1：建立 EditMode Tests asmdef**

建立以下內容：

```json
{
  "name": "MortalGame.EditModeTests",
  "rootNamespace": "",
  "references": [
    "MortalGame.Runtime"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": [
    "TestAssemblies"
  ]
}
```

- [ ] **Step 2：建立 PlayMode Tests asmdef**

建立以下內容：

```json
{
  "name": "MortalGame.PlayModeTests",
  "rootNamespace": "",
  "references": [
    "MortalGame.Runtime"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": [
    "TestAssemblies"
  ]
}
```

### Task 4：由 Unity 匯入並修正程序集引用

**Files:**
- Modify only if compiler requires: `Assets/Scripts/MortalGame.Runtime.asmdef`
- Modify only if compiler requires: `Assets/Scripts/Editor/MortalGame.Editor.asmdef`

- [ ] **Step 1：強制 AssetDatabase 更新**

Run：Unity MCP `execute_menu_item(menuPath: "Assets/Refresh")`。

Expected：Unity 完成四個新資產的匯入。

- [ ] **Step 2：觸發 Unity 重新編譯**

Run：Unity MCP `recompile_scripts(returnWithLogs: true, logsLimit: 200)`。

Expected：0 error。

- [ ] **Step 3：只依明確錯誤補充程序集引用**

若 Unity 回報 namespace 或 assembly 缺失，先確認提供該型別的 asmdef 名稱，再將其加入 `references`；不得移動 `.cs` 或順手重構程式。

- [ ] **Step 4：重複重新編譯直到 0 error**

Run：Unity MCP `recompile_scripts(returnWithLogs: true, logsLimit: 200)`。

Expected：0 error；warning 必須記錄。

### Task 5：驗證 Test Runner 與工作區差異

**Files:**
- Verify: `Assets/Tests/EditMode/MortalGame.EditModeTests.asmdef`
- Verify: `Assets/Tests/PlayMode/MortalGame.PlayModeTests.asmdef`

- [ ] **Step 1：查詢 EditMode Tests**

Run：Unity MCP `run_tests(testMode: "EditMode", returnOnlyFailures: false, returnWithLogs: true)`。

Expected：Test Runner 可啟動；因本階段沒有測試程式，允許回報 0 tests，但不可有程序集載入或編譯錯誤。

- [ ] **Step 2：檢查 JSON 與 Git 差異**

Run：

```powershell
Get-Content -Raw Assets/Scripts/MortalGame.Runtime.asmdef | ConvertFrom-Json | Out-Null
Get-Content -Raw Assets/Scripts/Editor/MortalGame.Editor.asmdef | ConvertFrom-Json | Out-Null
Get-Content -Raw Assets/Tests/EditMode/MortalGame.EditModeTests.asmdef | ConvertFrom-Json | Out-Null
Get-Content -Raw Assets/Tests/PlayMode/MortalGame.PlayModeTests.asmdef | ConvertFrom-Json | Out-Null
git diff --check
git status --short
```

Expected：四個 JSON 均可解析、`git diff --check` 無錯誤；既有使用者變更仍存在且未被納入本次修改。

