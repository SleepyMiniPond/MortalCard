# Presenter 協調層

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Presenter 是 MVP 架構中的**協調樞紐**，是唯一同時接觸 GameModel 和 GameView 的層級。它負責：
1. 將玩家的 UI 操作轉譯為遊戲邏輯動作
2. 將遊戲邏輯產生的事件分發給 View 渲染
3. 建構戰鬥所需的所有依賴物件
4. 管理整個戰鬥的生命週期

## 子系統總覽

```
Presenter/
├── Gameplay/
│   ├── GameplayPresenter.cs     # 戰鬥主協調器
│   ├── BattleBuilder.cs         # 依賴建構器
│   ├── Context.cs               # 全域遊戲配置
│   ├── GameInfoModel.cs         # GameViewModel 實作
│   ├── GameStageSetting.cs      # 關卡配置 Record
│   ├── ScriptableDataLoader.cs  # 資料載入器
│   └── InterAction/
│       ├── GameAction.cs        # Presenter→Model 的動作
│       └── GameCommand.cs       # View→Presenter 的命令
└── LevelMap/
    ├── LevelMapPresenter.cs     # 關卡選擇協調器
    └── LevelMapView.cs          # 關卡選擇視圖
```

## GameplayPresenter — 戰鬥主協調器

整個戰鬥場景的大腦，協調 Model 與 View 的互動。

### 持有的依賴

| 依賴 | 型別 | 用途 |
|------|------|------|
| GameplayView | IGameplayView | 視覺呈現入口 |
| GameViewModel | IGameViewModel | 響應式狀態中心 |
| GameplayManager | IGameplayManager | 遊戲邏輯引擎 |
| UIPresenter | IUIPresenter | 面板事件處理 |
| SubSelectionPresenter | — | 子選取流程處理 |
| GameResultWin/LosePresenter | — | 勝負結果處理 |

### Run — 主執行流程

```
Run()
  1. 啟動 GameplayManager.StartBattle()（非同步遊戲迴圈）
  2. 同時啟動：
     a. _GameplayBattleActions()  ← 玩家動作處理迴圈
     b. _uiPresenter.Run()        ← UI 事件監聽
  3. 等待戰鬥結束
  4. 取消所有非同步任務
  5. 顯示勝/負結果面板
  6. 回傳 GameplayResultCommand
```

### 動作處理迴圈

```
_GameplayBattleActions()
  Loop:
    1. 等待 GameplayManager 進入可接受動作狀態
    2. 從 View 接收 GameCommand（UseCardCommand / TurnSubmitCommand）
    3. _PostProcessAction() 轉譯為 GameAction
       - UseCardCommand → 判斷是否需要子選取
         → 需要：啟動 SubSelectionPresenter 取得目標
         → 不需要：直接建立 UseCardAction
       - TurnSubmitCommand → TurnSubmitAction
    4. 將 GameAction 送入 GameplayManager 的佇列
    5. 等待 GameplayManager 處理完成
    6. 取得事件列表 → GameplayView.Render()
```

## 命令與動作

### GameCommand（View → Presenter）

玩家在 UI 上的操作被封裝為命令：
- `UseCardCommand`：點擊/拖曳卡牌（含可選的主目標）
- `TurnSubmitCommand`：點擊送出按鈕

### GameAction（Presenter → Model）

Presenter 將命令轉譯為遊戲邏輯動作：
- `UseCardAction`：打出卡牌（含主選取 + 子選取集合）
  - `MainSelectionAction`：主目標（角色/卡牌/無）
  - 子選取：`ExistCardSubSelectionAction`、`NewCardSubSelectionAction` 等
- `TurnSubmitAction`：結束回合

### 轉譯過程

```
UseCardCommand（卡牌 Guid + 可選主目標）
  ↓
檢查卡牌是否有子選取需求
  ├── 有：啟動 SubSelectionPresenter → 取得選取結果
  └── 無：直接建立
  ↓
UseCardAction（卡牌 Guid + 主選取 + 子選取字典）
```

## BattleBuilder — 依賴建構器

使用 Builder Pattern 建構戰鬥所需的所有運行時物件：

### ConstructGameContextManager()
建立 GameContextManager 並初始化所有 Library：
- CardLibrary、CardBuffLibrary
- PlayerBuffLibrary、CharacterBuffLibrary
- DispositionLibrary、LocalizeLibrary

### ConstructBattle()
建立 GameStageSetting（不可變的戰鬥配置）：
- StageID、RandomSeed
- AllyInstance、EnemyData

## Context — 全域遊戲配置

單例物件，持有從 ScriptableObject 載入的所有遊戲配置：
- 卡牌/Buff 資料表（ID → Data 字典）
- 好感度設定
- 敵人定義集合
- 玩家實例（AllyInstance）
- 本地化字典

## ScriptableDataLoader — 資料載入器

序列化引用所有 ScriptableObject 資產，提供屬性存取：
- 從 AllCardScriptable 取得所有卡牌資料
- 從 ExcelDatas 解析好感度設定
- 從 ExcelDatas 解析本地化字典

## GameStageSetting — 關卡配置

Record 類型的不可變戰鬥設定：
- StageID：關卡識別碼
- RandomSeed：隨機種子（確保可重現）
- AllyInstance：友軍玩家快照
- EnemyData：敵軍配置

## LevelMap 子系統

### LevelMapPresenter

簡單的狀態機：
- Walk（預設）→ Battle（選擇關卡）→ Leave（退出）
- 回傳 `LevelMapCommand` 供 Main 遊戲迴圈使用

### LevelMapView

最小的視覺元件，提供關卡按鈕點擊回調。

## 設計模式

| 模式 | 應用 |
|------|------|
| **MVP** | Presenter 作為 Model 與 View 的唯一橋樑 |
| **Builder** | BattleBuilder 建構複雜依賴 |
| **Command** | GameCommand / GameAction 封裝互動意圖 |
| **非同步事件迴圈** | UniTaskPresenter 驅動模態流程 |
| **狀態機** | LevelMapPresenter 管理關卡選擇流程 |

## 相關文件

- [GameModel 核心邏輯](GameModel.md) — Presenter 驅動的邏輯引擎
- [GameView 視覺呈現層](GameView.md) — Presenter 分發的渲染層
- [Scene 場景管理](Scene.md) — 載入 Presenter 的容器
- [SystemArchitecture 架構總覽](SystemArchitecture.md) — MVP 架構說明
