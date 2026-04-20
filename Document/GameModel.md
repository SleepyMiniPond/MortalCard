# GameModel 核心遊戲邏輯層

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

GameModel 是整個遊戲的**大腦**，負責所有戰鬥規則的實現。它完全不知道 View 層的存在——所有狀態變更都透過產生 **不可變事件（IGameEvent）** 來通知外部世界。這個設計保證了邏輯層的純粹性與可測試性。

GameModel 的核心職責：
1. **驅動遊戲迴圈**：回合制狀態機（開始→抽牌→行動→結算→循環）
2. **管理遊戲實體**：Player、Character、Card 的完整生命週期
3. **執行效果管線**：Action → Effect → Event 三階處理
4. **維護上下文**：追蹤當前選取的目標與計算作用域

## 子系統總覽

```
GameModel/
├── GameplayManager.cs       # 遊戲迴圈主驅動器
├── GameContextManager.cs    # 上下文與作用域管理
├── GameModel.cs             # 遊戲模型介面定義
├── GameStatus.cs            # 遊戲狀態快照
├── GameEvent.cs             # 事件型別定義（20+ 種）
├── GameFormula.cs           # 數值計算公式中心
├── GameHistory.cs           # 歷史記錄（預留功能）
├── Action/                  # 動作系統
├── Condition/               # 條件系統
├── Effect/                  # 效果管線
├── EnemyLogic/              # 敵人 AI 邏輯
├── Entity/                  # 實體系統（最大子目錄）
├── Instance/                # 實例層
└── Target/                  # 目標系統
```

## GameplayManager — 遊戲迴圈

GameplayManager 是整個戰鬥的心臟，實現了一個非同步的回合制狀態機。

### 戰鬥流程

```
GameStart（遊戲開始）
  ├── 觸發 GameStart 時機 Buff
  └── 進入回合迴圈
      ↓
TurnStart（回合開始）
  ├── 觸發 TurnStart 時機 Buff
  ├── 更新所有實體（Buff 生命週期、Session 重置）
  └── 產生 RoundStartEvent
      ↓
DrawCard（抽牌階段）
  ├── 好感度系統計算額外抽牌數
  ├── 從牌組抽牌到手牌
  └── 產生 DrawCardEvent
      ↓
EnemyPrepare（敵人準備）
  ├── 敵人 AI 選牌（貪心策略）
  └── 產生 EnemySelectCardEvent
      ↓
PlayerExecute（玩家行動）★ 核心互動環節
  ├── 等待玩家從 UI 佇列輸入動作
  ├── UseCardAction → 執行卡牌效果管線
  ├── TurnSubmitAction → 結束玩家行動
  └── 每次動作後觸發 Buff 反應
      ↓
EnemyExecute（敵人行動）
  ├── 按序打出已選卡牌
  └── 同樣經過完整效果管線
      ↓
TurnEnd（回合結束）
  ├── 清理手牌（保留 Preserved 卡牌）
  ├── 觸發 TurnEnd 時機 Buff
  ├── 回收墓地中有 Recycle 屬性的卡牌
  ├── 回復能量
  └── 若有角色死亡 → GameEnd
```

### 事件聚合機制

GameplayManager 在整個回合中不斷累積事件到 `_gameEvents` 列表。外部透過 `PopAllEvents()` 批次取得所有待處理事件，然後交由 View 層逐一播放動畫。這種**批次推送**設計避免了事件丟失與順序錯亂。

### 玩家輸入機制

使用 `UniTaskAwaitableQueue` 實現非同步輸入等待。Presenter 將玩家操作（拖曳卡牌、點擊按鈕）轉為 `GameAction` 後送入佇列，GameplayManager 在 PlayerExecute 階段非同步等待與消費。

## GameContextManager — 上下文管理

### 設計動機

效果計算經常需要知道「當前是誰在觸發」、「目標是哪張卡」等上下文資訊。GameContextManager 使用**堆疊式作用域**來管理這些臨時狀態。

### 堆疊式作用域

```
SetClone()    → 推入一個複製的上下文（開始新的計算作用域）
SetSelected*() → 在當前作用域設定選取的 Player/Character/Card
Pop()         → 彈出作用域（恢復上層狀態）
```

這個設計解決了巢狀效果計算的問題——例如「打出卡牌 A 觸發 Buff B 的效果，Buff B 又需要讀取卡牌 A 的目標」。每層計算都有自己的作用域，不會互相干擾。

### 資料庫引用

GameContextManager 同時持有所有 Library（CardLibrary、各 BuffLibrary、DispositionLibrary、LocalizeLibrary）的引用，作為全域資料存取的統一入口。

## GameFormula — 數值計算中心

所有數值計算集中於此，確保修正邏輯的一致性：

- **傷害計算**：`NormalDamagePoint()`、`PenetrateDamagePoint()`、`AdditionalDamagePoint()` — 各自套用不同的 Buff 加成邏輯
- **治療計算**：`HealPoint()` — 套用治療相關 Buff 修正
- **卡牌數值**：`CardPower()`、`CardCost()` — 計算卡牌在當前 Buff 影響下的實際數值

設計原則：所有原始數值從 CardData 或 Effect 取得，修正值從 PlayerBuff 和 CardBuff 的 Property 系統讀取，最終結果在 Formula 中合成。

## GameStatus — 狀態快照

極簡的資料容器，持有：
- 友軍（Ally）與敵軍（Enemy）的 PlayerEntity 引用
- 當前回合數
- 當前行動玩家（ReactiveProperty，供 View 訂閱）
- 對手玩家的推導（基於當前玩家反轉）

## 事件系統（GameEvent）

所有遊戲事件都是 **不可變 Record 類型**，確保事件一旦產生就不會被修改。事件分為以下類別：

### 遊戲流程事件
- `AllySummonEvent` / `EnemySummonEvent` — 角色登場
- `RoundStartEvent` — 回合開始
- `PlayerExecuteStartEvent` / `PlayerExecuteEndEvent` — 行動階段標記

### 卡牌操作事件
- `DrawCardEvent` / `MoveCardEvent` / `AddCardEvent` — 卡牌移動
- `UsedCardEvent` — 卡牌使用完畢
- `DiscardHandCardEvent` — 手牌丟棄
- `RecycleGraveyardToDeckEvent` / `RecycleGraveyardToHandCardEvent` — 卡牌回收
- `EnemySelectCardEvent` / `EnemyUnselectedCardEvent` — 敵人選牌

### 戰鬥數值事件
- `DamageEvent` / `GetHealEvent` / `GetShieldEvent` — 血量變化
- `GainEnergyEvent` / `LoseEnergyEvent` — 能量變化
- `IncreaseDispositionEvent` / `DecreaseDispositionEvent` — 好感度變化

### Buff 事件
- `AddPlayerBuffEvent` / `RemovePlayerBuffEvent` / `ModifyPlayerBuffLevelEvent`
- `AddCardBuffEvent` / `RemoveCardBuffEvent` / `ModifyCardBuffLevelEvent`

### 通用事件
- `GeneralUpdateEvent` — 批次更新事件，攜帶所有變化的實體資訊供 View 全面刷新

### IAnimationNumberEvent

標記介面，標示需要播放數字動畫的事件（如 DamageEvent 會在角色身上顯示傷害數字）。

## GameHistory — 歷史記錄

目前為最小實作，預留了回放/撤銷系統的架構空間：
- `GameHistory` — 根容器
- `TurnRecord` — 每回合的動作記錄
- `ActionRecord` — 每次動作的記錄

## 核心子系統引用

| 子系統 | 文件 | 職責 |
|--------|------|------|
| [Action 動作系統](Action.md) | `Action/` | 定義動作型別階層與觸發上下文 |
| [Condition 條件系統](Condition.md) | `Condition/` | 可組合的條件判斷邏輯 |
| [Effect 效果管線](Effect.md) | `Effect/` | 效果解析、命令執行、事件產生 |
| [Target 目標系統](Target.md) | `Target/` | 目標解析與選取規則 |
| [Entity 實體系統](Entity.md) | `Entity/` | 所有戰鬥實體的運行時管理 |
| [Instance 實例層](Instance.md) | `Instance/` | Data→Entity 的中介橋樑 |
| [Session 反應會話](Session.md) | `Entity/Session/` | Buff 動態狀態追蹤 |

## 敵人 AI 邏輯（EnemyLogic）

### UseCardLogic — 卡牌選取策略

採用**貪心演算法**：
1. 計算已選卡牌的總費用
2. 從手牌中篩選費用不超過剩餘能量的卡牌
3. 選取費用最高的卡牌
4. 重複直到無法再選

### SelectTargetLogic — 目標選取策略

根據 `TargetLogicTag` 分發：
- `ToEnemy`：優先選擇敵方角色/卡牌
- `ToAlly`：優先選擇己方
- `ToRandom`：隨機選擇

子目標選取使用隨機洗牌取前 N 張。

## 設計模式總結

| 模式 | 應用 |
|------|------|
| **狀態機** | GameplayManager 的回合流程控制 |
| **事件溯源** | 所有狀態變更產生不可變事件 |
| **觀察者/反應器** | Buff 系統在特定時機觸發反應 |
| **堆疊式作用域** | GameContextManager 管理巢狀計算上下文 |
| **非同步佇列** | 玩家輸入透過 UniTaskAwaitableQueue 傳遞 |
| **貪心演算法** | 敵人 AI 的卡牌選取策略 |
| **批次推送** | 事件聚合後一次性推送給 View |
