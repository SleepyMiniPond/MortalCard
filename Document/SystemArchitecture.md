# 專案系統架構總覽 - MortalGame

> 最後更新：2026-04-20 | 版本：v2.0

## 專案定位

MortalGame 是一款基於 Unity 的**武俠主題卡牌對戰遊戲**，玩家操控角色使用不同門派（唐門、峨嵋、嵩山、丐幫、滇蒼）的卡牌，與 AI 控制的敵人進行回合制戰鬥。遊戲包含卡牌打出、Buff 效果、血量護甲、能量管理、好感度等多層次的戰鬥機制。

## 核心架構哲學

### MVP + 三層資料架構

專案在巨觀層面採用 **MVP（Model-View-Presenter）** 模式，將遊戲分為「邏輯」、「呈現」、「協調」三大職責。在微觀的資料層面則採用 **Data → Instance → Entity** 三層架構，確保靜態設計資料、持久化實例、運行時戰鬥狀態三者之間有清晰的邊界。

```
┌─────────────────────────────────────────────────────────────────┐
│                        Scene 場景管理層                          │
│   控制場景生命週期與轉換，是最外層的遊戲流程容器                    │
├─────────────────────────────────────────────────────────────────┤
│                      Presenter 協調層                            │
│   連接 Model 與 View，處理玩家輸入轉譯與遊戲流程調度               │
├──────────────────────┬──────────────────────────────────────────┤
│   GameModel 邏輯層    │            GameView 視覺層                │
│   戰鬥規則與狀態機     │            UI 元件與動畫呈現               │
│   實體管理與事件產生    │            事件驅動的畫面更新               │
├──────────────────────┘──────────────────────────────────────────┤
│                       GameData 資料層                            │
│   ScriptableObject 封裝的靜態遊戲配置，供所有層級讀取              │
└─────────────────────────────────────────────────────────────────┘
```

### 六大核心系統

| 系統 | 職責 | 文件位置 |
|------|------|----------|
| **[GameData](GameData.md)** | 靜態資料定義：卡牌、角色、Buff、枚舉 | `Scripts/GameData/` |
| **[GameModel](GameModel.md)** | 核心戰鬥邏輯：狀態機、效果管線、實體管理 | `Scripts/GameModel/` |
| **[GameView](GameView.md)** | 視覺呈現：卡牌渲染、動畫、UI 元件 | `Scripts/GameView/` |
| **[Presenter](Presenter.md)** | 協調樞紐：輸入轉譯、依賴建構、流程調度 | `Scripts/Presenter/` |
| **[Scene](Scene.md)** | 場景管理：生命週期、轉換、載入 | `Scripts/Scene/` |
| **[UI](UI.md)** | 工具元件：DeckView、GraveyardView、SubmitView | `Scripts/GameView/Panel/UI/` |

## 系統間資料流

### 主要資料流向

```
GameData (靜態配置)
    ↓ 建構時讀取
Presenter/BattleBuilder (建構所有運行時物件)
    ↓ 注入依賴
GameModel/GameplayManager (驅動遊戲迴圈)
    ↓ 產生事件
Presenter/GameplayPresenter (轉譯事件)
    ↓ 分發渲染
GameView/GameplayView (視覺呈現)
```

### 玩家互動迴圈

```
玩家操作 UI (拖曳卡牌/點擊按鈕)
    ↓ GameCommand
GameplayPresenter (轉譯為 GameAction)
    ↓ UseCardAction / TurnSubmitAction
GameplayManager (執行效果管線)
    ↓ IGameEvent[]
GameplayView.Render() (逐事件播放動畫)
    ↓ 更新 GameViewModel
各 View 元件 (響應式更新)
```

## 三層資料架構詳解

這是本專案最核心的設計決策之一，目的是將「設計師配置的資料」、「遊戲存檔的快照」、「戰鬥中的可變狀態」三者完全分離。

### Data 層（設計時）
- **載體**：ScriptableObject + Odin Inspector
- **特性**：不可變的靜態設計資料，由設計師在 Unity 編輯器中配置
- **範例**：`CardData`（卡牌模板）、`PlayerBuffData`（Buff 模板）
- **關鍵設計**：Data 類別常包含 `CreateEntity()` 工廠方法，負責產生對應的 Entity

### Instance 層（持久化）
- **載體**：C# Record 類型
- **特性**：不可變的遊戲狀態快照，適合序列化與存檔
- **範例**：`CardInstance`（卡牌實例，含唯一 Guid）、`AllyInstance`（玩家存檔狀態）
- **關鍵設計**：Record 保證不可變性，Guid 確保跨場景的身份追蹤

### Entity 層（戰鬥時）
- **載體**：一般 C# 類別，含可變狀態
- **特性**：運行時的完整戰鬥實體，擁有 Manager 子系統與狀態變化能力
- **範例**：`CardEntity`（含 BuffManager、PropertyEntity）、`PlayerEntity`（含 EnergyManager、CardManager）
- **關鍵設計**：透過 Composition 模式組合多個 Manager，形成完整的實體行為

```
CardData (SO 模板)  →  CardInstance (Record 快照)  →  CardEntity (戰鬥實體)
  ├ 效果列表             ├ 唯一 Guid                   ├ BuffManager
  ├ 屬性定義             ├ 卡牌 Data ID                ├ PropertyEntity[]
  └ 目標規則             └ 附加屬性                     └ 可變狀態
```

## 核心技術棧

| 技術 | 用途 | 原則 |
|------|------|------|
| **UniTask** | 所有非同步操作 | 取代原生 Task，優化 Unity 環境效能 |
| **UniRx** | 響應式狀態管理 | `ReactiveProperty<T>` 驅動 View 自動更新 |
| **Record 類型** | 不可變資料物件 | 事件、動作、Instance 層均使用 Record |
| **Option 模式** | 空值安全 | 取代 null 檢查，提供型別安全的可選值 |
| **Odin Inspector** | 編輯器整合 | BoxGroup、TableColumnWidth 等特性增強設計師體驗 |

## 遊戲迴圈架構

Main.cs 作為遊戲入口，以無限迴圈管理場景轉換：

```
Main Loop:
  Menu 場景 → 等待玩家點擊
  ↓
  LevelMap 場景 → 選擇關卡
  ↓                        ↙ 失敗重試
  Gameplay 場景 → 戰鬥結果 → 回到 LevelMap 或 Menu
```

## 戰鬥系統核心管線

戰鬥邏輯的精華在於 **Action-Effect-Event 三階管線**：

1. **Action 階段**：宣告意圖（如「對目標造成傷害」），經過 Buff 反應修正
2. **Effect 階段**：解析目標、計算數值、執行狀態變更
3. **Event 階段**：產生不可變事件記錄，交由 View 層播放動畫

每個效果經過三層處理：**Intent（意圖宣告）→ TargetIntent（目標綁定）→ Result（結果確認）**，每層都觸發 Buff 反應系統，實現複雜的連鎖效果。

詳見：[Effect 效果管線](Effect.md)、[Action 動作系統](Action.md)

## 關鍵設計模式

| 模式 | 應用場景 |
|------|----------|
| **事件溯源** | 所有狀態變更產生不可變事件，GameplayManager 聚合後批次推送 |
| **組合模式** | 條件系統（All/Any/Inverse）、效果鏈、目標選取均可遞迴組合 |
| **工廠 + 物件池** | View 層所有 Prefab 透過 PrefabFactory 管理，避免 GC 波動 |
| **策略模式** | Buff 生命週期、條件判斷、目標解析均為可替換的策略實作 |
| **命令模式** | EffectCommand 封裝狀態變更，GameCommand 封裝玩家操作 |
| **Null 物件** | DummyCard、DummyCharacter、DummyPlayer 提供安全預設值 |
| **Builder** | BattleBuilder 負責建構戰鬥所需的所有依賴物件 |

## 系統間依賴關係

```
Scene ──依賴──→ Presenter ──依賴──→ GameModel ──依賴──→ GameData
                    │                                      ↑
                    └──依賴──→ GameView ──讀取──→ GameData（透過 ViewModel）
```

- **Scene** 擁有場景元件引用，委託 Presenter 執行邏輯
- **Presenter** 是唯一同時接觸 Model 和 View 的層級
- **GameModel** 不知道 View 的存在，僅產生事件
- **GameView** 透過 ViewModel（ReactiveProperty）接收狀態更新
- **GameData** 是純資料層，不依賴任何其他系統
