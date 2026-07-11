# EventView 事件視圖

> 最後更新：2026-07-12 | 版本：v2.1

## 設計理念

EventView 系統負責戰鬥中的**數字動畫特效**——當角色受到傷害、獲得治療、獲得護甲等事件發生時，在角色身上顯示對應的數字動畫。所有 EventView 遵循統一的生命週期模式，且支援物件池回收。

## 統一架構

每個 EventView 都遵循相同的設計模式：
- 實作 `IAnimationNumberEventView`（非同步動畫契約）
- 繼承 `BaseAnimationEventView`，統一實作 `IRecyclable` 與動畫契約
- 使用 `PlayableDirector` 驅動 Timeline 動畫
- 接收 `CancellationToken`，取消時停止 Director 並隱藏 GameObject

### 生命週期

```
SetEventInfo(Event, Parent)  → 從事件資料設定顯示文字
PlayAnimation(Token)         → 啟用 GameObject → 播放 Timeline → 完成或取消後停止並停用
Reset()                      → 清理狀態，準備被物件池回收
```

`BaseAnimationEventView` 以 `finally` 保證 Director 與 GameObject 狀態復原；CharacterEventAnimationPlayer 另以外層 `finally` 保證 Prefab 回收。兩層分別負責 EventView 自身狀態與 Factory 所有權。

## 事件類型

| EventView | 對應事件 | 顯示內容 |
|-----------|----------|----------|
| DamageEventView | DamageEvent | 傷害數字 |
| HealEventView | GetHealEvent | 治療數字 |
| ShieldEventView | GetShieldEvent | 護甲增加數字 |
| GainEnergyEventView | GainEnergyEvent | 能量獲得數字 |
| LoseEnergyEventView | LoseEnergyEvent | 能量消耗數字 |
| IncreaseDispositionEventView | IncreaseDispositionEvent | 好感度增加 |
| DecreaseDispositionEventView | DecreaseDispositionEvent | 好感度減少 |

## IAnimationNumberEventView

定義可取消的數字動畫播放契約。不同事件視圖只需負責將事件資料轉成顯示內容，共用播放生命週期由 BaseAnimationEventView 統一處理。

## 與其他系統的關係

- **CharacterView**：EventView 在角色身上播放，由 CharacterView 的事件佇列管理
- **Factory**：每種 EventView 都有對應的工廠（如 DamageEventViewFactory）
- **GameEvent**：從 GameModel 產生的事件記錄提供動畫數據

## 相關文件

- [CharacterView 角色視圖](CharacterView.md) — 動畫播放的容器
- [Factory 工廠系統](Factory.md) — EventView 的物件池工廠
- [GameModel 核心邏輯](GameModel.md) — 事件的產生源
