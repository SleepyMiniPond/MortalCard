# GameView 視覺呈現層

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

GameView 是 MVP 架構中的**View 層**，負責將 GameModel 的資料狀態轉換為玩家可見的視覺元素。設計核心原則：

1. **被動渲染**：View 不主動查詢 Model，而是**響應事件**（IGameEvent）和**訂閱狀態**（ReactiveProperty）
2. **零業務邏輯**：View 只關心「怎麼顯示」，不關心「為什麼顯示」
3. **工廠 + 物件池**：所有動態 UI 元件透過 PrefabFactory 管理，避免 GC 波動

## 子系統總覽

```
GameView/
├── GameplayView.cs          # 總調度器（20+ 事件類型分發）
├── PlaygroundView.cs        # 遊戲棋盤（預設選取目標）
├── ISelectableView.cs       # 可選取介面定義
├── ViewUtility.cs           # 動畫工具（PlayableDirector + UniTask）
├── BuffView/                # Buff 圖示顯示
├── CardView/                # 卡牌渲染與互動
├── CharacterView/           # 角色視覺與動畫
├── EventView/               # 戰鬥數字動畫
├── Factory/                 # 物件池工廠
└── Panel/                   # 面板系統
    ├── Info/                # 狀態資訊面板
    ├── Popup/               # 彈窗面板
    └── UI/                  # 工具按鈕
```

## GameplayView — 總調度器

GameplayView 是整個 View 層的**入口與樞紐**，實作三個關鍵介面：
- `IGameplayView`：完整的視覺操作契約
- `IAllCardDetailPanelView`：卡牌詳情面板入口
- `IInteractionButtonView`：互動按鈕入口

### Init — 初始化接線

`Init()` 方法將所有子元件與依賴連接：
- **ViewModel**：GameViewModel（響應式狀態源）
- **ActionReceiver**：玩家操作接收器（送往 Presenter）
- **StatusWatcher**：遊戲狀態觀察（當前行動玩家等）
- **LocalizeLibrary / DispositionLibrary**：本地化與好感度查詢

### Render — 事件分發引擎

`Render()` 是核心方法，接收 `IGameEvent` 陣列，逐一識別事件類型並委派給對應的處理邏輯：

| 事件類型 | 處理 |
|----------|------|
| DrawCardEvent | 在手牌區域建立新的 CardView |
| MoveCardEvent | 移動卡牌到其他區域 |
| DamageEvent | 在角色身上播放傷害數字動畫 |
| GetHealEvent | 播放治療動畫 |
| GetShieldEvent | 播放護甲動畫 |
| AddPlayerBuffEvent | 在 Buff 集合中添加圖示 |
| GeneralUpdateEvent | 批次更新所有 ViewModel 狀態 |
| ... | 20+ 種事件各有對應處理 |

### DisableAllInteraction — 安全鎖

在特定階段（如效果結算中）禁用所有玩家互動，防止不當操作。

## GameViewModel — 響應式狀態中心

GameViewModel 是 View 層的**單一真相來源**（Single Source of Truth），使用 UniRx `ReactiveProperty<T>` 管理所有可觀察狀態：

| 狀態 | 型別 | 用途 |
|------|------|------|
| CardInfo | Dict<Guid, ReactiveProperty<CardInfo>> | 每張卡牌的完整顯示資訊 |
| PlayerBuffInfo | Dict<Guid, ReactiveProperty<PlayerBuffInfo>> | 每個 Buff 的顯示狀態 |
| CharacterBuffInfo | Dict<Guid, ReactiveProperty<CharacterBuffInfo>> | 角色 Buff 的顯示狀態 |
| CardCollectionInfo | 巢狀 Dict[Faction][Type] | 各區域的卡牌集合資訊 |
| DispositionInfo | ReactiveProperty | 好感度資訊 |
| IsHandCardsEnabled | bool | 手牌是否可互動 |

View 元件透過 `Observable*()` 方法訂閱這些狀態，在資料變化時自動更新顯示。

## ISelectableView — 可選取介面

定義可被玩家點擊/拖曳選取的 UI 元件契約：
- `RectTransform`：位置資訊
- `TargetType`：目標類型（角色/卡牌）
- `TargetIdentity`：目標 Guid
- `OnSelect()` / `OnDeselect()`：選取/取消選取回調

## 子系統文件引用

| 子系統 | 文件 | 簡述 |
|--------|------|------|
| CardView | [CardView 卡牌視圖](CardView.md) | 手牌渲染、拖曳互動、弧形排列 |
| BuffView | [BuffView Buff 視圖](BuffView.md) | Buff 圖示、層數顯示、提示框 |
| CharacterView | [CharacterView 角色視圖](CharacterView.md) | 角色動畫、事件佇列處理 |
| EventView | [EventView 事件視圖](EventView.md) | 數字動畫（傷害/治療/護甲等） |
| Factory | [Factory 工廠系統](Factory.md) | PrefabFactory 物件池 |
| Panel/Info | [Info 資訊面板](GameView_Info.md) | 血條、能量條、好感度、回合數 |
| Panel/Popup | [Popup 彈窗面板](GameView_Popup.md) | 卡牌詳情、卡牌選取、勝負結果 |
| Panel/UI | [UI 工具元件](GameView_UI.md) | 牌組按鈕、墓地按鈕、送出按鈕 |

## 設計模式

| 模式 | 應用 |
|------|------|
| **Composite** | GameplayView 聚合所有子 View |
| **Observer** | ReactiveProperty 驅動自動更新 |
| **Strategy** | Render() 按事件類型分發處理 |
| **Factory + Pool** | 所有動態元件透過 PrefabFactory 建立/回收 |
| **MVP** | View 只負責顯示，邏輯在 Presenter/Model |

## 相關文件

- [Presenter 協調層](Presenter.md) — 連接 View 與 Model
- [GameModel 核心邏輯](GameModel.md) — 產生事件供 View 渲染
- [SystemArchitecture 架構總覽](SystemArchitecture.md) — View 在系統中的位置
