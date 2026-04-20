# AI 筆記索引 — MortalGame 文件系統

> 最後更新：2026-04-20 | 版本：v2.0

## 文件總覽

本索引列出 `Document/` 資料夾中所有文件及其涵蓋範圍。

---

### 🔴 必讀文件

| 文件 | 說明 |
|------|------|
| [AI_WorkOutline.md](AI_WorkOutline.md) | AI 工作指南、原則與流程 |
| [AI_Notes_Index.md](AI_Notes_Index.md) | 本文件 — 文件索引 |
| [Coding_Standards.md](Coding_Standards.md) | 編程規範（命名、架構、技術堆疊） |
| [Documentation_Guidelines.md](Documentation_Guidelines.md) | 文件撰寫標準 |

---

### 🏗️ 架構層級

| 文件 | 說明 | 狀態 |
|------|------|------|
| [SystemArchitecture.md](SystemArchitecture.md) | 五大系統架構總覽、協作關係 | ✅ 2026-04-20 |

---

### 📦 GameData — 資料定義層

| 文件 | 說明 | 狀態 |
|------|------|------|
| [GameData.md](GameData.md) | 資料層總覽、ExcelDatas、Library、列舉 | ✅ 2026-04-20 |

---

### ⚙️ GameModel — 核心邏輯層

| 文件 | 說明 | 狀態 |
|------|------|------|
| [GameModel.md](GameModel.md) | 邏輯層總覽、狀態機、管理器 | ✅ 2026-04-20 |
| [Action.md](Action.md) | 三層動作管線（Intent→TargetIntent→Result） | ✅ 2026-04-20 |
| [Effect.md](Effect.md) | 效果管線（Resolver→Command→Executor） | ✅ 2026-04-20 |
| [Condition.md](Condition.md) | 組合式條件系統 | ✅ 2026-04-20 |
| [Target.md](Target.md) | 目標解析系統 | ✅ 2026-04-20 |
| [Entity.md](Entity.md) | 實體系統（組合式設計） | ✅ 2026-04-20 |
| [Card.md](Card.md) | 卡牌系統（三層流轉） | ✅ 2026-04-20 |
| [CardBuff.md](CardBuff.md) | 卡牌 Buff 系統 | ✅ 2026-04-20 |
| [Character.md](Character.md) | 角色系統（血量、護甲、CharacterBuff） | ✅ 2026-04-20 |
| [Player.md](Player.md) | 玩家系統（能量、牌組、好感度） | ✅ 2026-04-20 |
| [Session.md](Session.md) | 反應 Session 系統 | ✅ 2026-04-20 |
| [Instance.md](Instance.md) | Instance 層（CardInstance、AllyInstance） | ✅ 2026-04-20 |

---

### 🎨 GameView — 視覺呈現層

| 文件 | 說明 | 狀態 |
|------|------|------|
| [GameView.md](GameView.md) | 視覺層總覽、GameplayView、ViewModel | ✅ 2026-04-20 |
| [CardView.md](CardView.md) | 卡牌視圖（弧形排列、拖曳、聚焦） | ✅ 2026-04-20 |
| [BuffView.md](BuffView.md) | Buff 視圖（圖示、響應式更新） | ✅ 2026-04-20 |
| [CharacterView.md](CharacterView.md) | 角色視圖（動畫佇列） | ✅ 2026-04-20 |
| [EventView.md](EventView.md) | 事件視圖（數字動畫） | ✅ 2026-04-20 |
| [Factory.md](Factory.md) | 工廠系統（PrefabFactory 物件池） | ✅ 2026-04-20 |

---

### 📋 Panel — 面板子系統

| 文件 | 說明 | 狀態 |
|------|------|------|
| [GameView_Panel.md](GameView_Panel.md) | 面板系統總覽（Info/Popup/UI） | ✅ 2026-04-20 |
| [GameView_Info.md](GameView_Info.md) | 資訊面板（血條、能量、好感度） | ✅ 2026-04-20 |
| [GameView_Popup.md](GameView_Popup.md) | 彈窗面板（卡牌選取、結果面板） | ✅ 2026-04-20 |
| [GameView_UI.md](GameView_UI.md) | 工具元件（牌組/墓地按鈕、送出） | ✅ 2026-04-20 |

---

### 🎯 Presenter — 協調層

| 文件 | 說明 | 狀態 |
|------|------|------|
| [Presenter.md](Presenter.md) | 協調層總覽、BattleBuilder、Command/Action | ✅ 2026-04-20 |

---

### 🎬 Scene — 場景管理

| 文件 | 說明 | 狀態 |
|------|------|------|
| [Scene.md](Scene.md) | 場景管理、Main 遊戲迴圈 | ✅ 2026-04-20 |

---

## 建議閱讀順序

1. **AI_WorkOutline.md** — 了解工作原則
2. **Coding_Standards.md** — 了解技術規範
3. **SystemArchitecture.md** — 掌握全局架構
4. **GameData.md** → **Instance.md** → **Entity.md** — 理解三層資料架構
5. **GameModel.md** → **Action.md** → **Effect.md** — 理解邏輯管線
6. **GameView.md** → 各子視圖文件 — 理解視覺系統
7. **Presenter.md** → **Scene.md** — 理解協調與場景流程

---

## 文件系統統計

- **總文件數**：29 個
- **必讀文件**：4 個
- **系統文件**：25 個
- **全面重寫日期**：2026-04-20
- **涵蓋腳本數**：~145 個 .cs 檔案
