# CharacterView 角色視圖

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

CharacterView 負責角色在戰鬥場上的視覺呈現，核心功能是**播放戰鬥數字動畫**（傷害、治療、護甲變化等）。設計上使用非同步事件佇列，確保多個動畫按序播放、不會重疊。

## 架構設計

### BaseCharacterView — 抽象基類

所有角色視圖的基礎，實現動畫事件佇列機制：

**事件佇列處理**：
- 接收動畫事件（傷害、治療、護甲、能量、好感度）
- 放入佇列中
- 以最小時間間隔（`_minTimeInterval`）逐一播放
- 使用 `UniTaskVoid _Run()` 的持續運行迴圈

**動畫工廠集合**：每種事件類型都有對應的工廠，負責建立/回收動畫物件：
- DamageEventViewFactory
- HealEventViewFactory
- ShieldEventViewFactory
- GainEnergyEventViewFactory
- LoseEnergyEventViewFactory
- IncreaseDispositionEventViewFactory
- DecreaseDispositionEventViewFactory

### AllyCharacterView — 友軍角色

- 實作 `ISelectableView`：可作為效果目標被選取
- TargetType：`AllyCharacter`
- 持有主角色的 Guid（Identity）

### EnemyCharacterView — 敵軍角色

- 實作 `ISelectableView`：可作為效果目標被選取
- TargetType：`EnemyCharacter`
- 接收與友軍相同的動畫事件

## 動畫播放流程

```
遊戲事件產生（如 DamageEvent）
  ↓
GameplayView.Render() 將事件分發到對應 CharacterView
  ↓
CharacterView 將事件加入佇列
  ↓
_Run() 迴圈取出事件
  ↓
透過對應 Factory 建立 EventView
  ↓
EventView.PlayAnimation()（Timeline 動畫）
  ↓
動畫完成 → 回收 EventView 到物件池
```

## 相關文件

- [GameView 視覺呈現層](GameView.md) — CharacterView 的父系統
- [EventView 事件視圖](EventView.md) — 數字動畫的具體實作
- [Character 角色系統](Character.md) — 角色的邏輯層
- [Factory 工廠系統](Factory.md) — 事件動畫的工廠
