# GameView Info 資訊面板

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Info 面板負責**持續顯示遊戲狀態**——血量、能量、好感度、回合數等。這些是「永遠可見」的 UI 元件，設計上以**被動更新**為原則：透過 UniRx 訂閱 ViewModel 的狀態變化，自動刷新顯示。

## 元件列表

### TopBarInfoView — 回合數

- 顯示當前回合/回合數
- 簡單的文字更新

### HealthBarView — 血量狀態

- 顯示當前/最大血量（填充條）
- 顯示護甲值（護甲存在時切換顯示物件）
- 提供更新方法供遊戲事件驅動

### EnergyBarView — 能量狀態

- 顯示當前/最大能量（填充條）
- 與 HealthBarView 相同的更新模式

### DispositionView — 好感度狀態

- 以圖片填充表示好感度等級
- 訂閱 `ViewModel.ObservableDispositionInfo` 取得即時更新
- 懸停時顯示本地化的好感度名稱與效果說明
- 使用 `DispositionLibrary` 將數值映射為等級名稱

### AllyInfoView — 友軍資訊面板

聚合顯示友軍的所有狀態：
- 內含 HealthBarView、EnergyBarView、DispositionView
- 內含 PlayerBuffCollectionView（Buff 圖示集合）
- `Init()` 接收 ViewModel、LocalizeLibrary、DispositionLibrary、SimpleTitleInfoHintView
- 提供各種更新方法，由 GameplayView.Render() 在對應事件觸發時呼叫

### EnemyInfoView — 敵軍資訊面板

與 AllyInfoView 結構相同，但用於敵方玩家：
- 聚合 HealthBarView、EnergyBarView
- 包含 PlayerBuffCollectionView
- 無 DispositionView（敵人沒有好感度機制）

### GameKeyWordInfoView — 關鍵字說明

- 可回收的關鍵字資訊元件
- 顯示遊戲機制的本地化名稱與說明
- 用於提示框系統

## 更新模式

Info 面板的更新遵循兩種模式：

### 1. 響應式訂閱（持續更新）
```
ViewModel.ObservableXxx() → Subscribe → 自動更新 UI
```
適用：DispositionView、DeckCardView、GraveyardCardView

### 2. 事件驅動（離散更新）
```
GameplayView.Render() → 識別事件 → 呼叫 InfoView.Update()
```
適用：HealthBarView、EnergyBarView、TopBarInfoView

## 相關文件

- [GameView_Panel 面板系統](GameView_Panel.md) — Info 的父容器
- [GameView 視覺呈現層](GameView.md) — 事件分發源
- [BuffView Buff 視圖](BuffView.md) — Buff 集合的 View
