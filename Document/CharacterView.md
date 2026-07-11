# CharacterView 角色視圖

> 最後更新：2026-07-12 | 版本：v2.1

## 設計理念

CharacterView 負責角色在戰鬥場上的視覺呈現，核心功能是**播放戰鬥數字動畫**（傷害、治療、護甲變化等）。動畫排程、事件呈現與角色 View 本身分離，使角色移除時能取消並等待所有動畫資源完成清理。

## 架構設計

### BaseCharacterView — 抽象基類

所有角色視圖的基礎，保存動畫 Factory 與事件父節點，並組裝 CharacterAnimationWorker 與 CharacterEventAnimationPlayer。

### CharacterAnimationWorker — 排程與生命週期

- 接收動畫事件（傷害、治療、護甲、能量、好感度）並放入佇列
- 以最小時間間隔啟動動畫，並追蹤所有進行中的 UniTask
- `Dispose()` 發出取消；`Completion` 供外層等待 loop 與動畫完全收斂
- 角色動畫工作不使用無主的 `.Forget()`，非取消例外透過 Completion 傳回擁有者

### CharacterEventAnimationPlayer — 事件呈現

- 將動畫事件分派至對應 EventView Factory
- 設定顯示資料與角色事件父節點
- 以 `finally` 保證 EventView 回收到 PrefabFactory

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
CharacterView 將事件送入 CharacterAnimationWorker
  ↓
Worker 依最小間隔取出事件並追蹤動畫 Task
  ↓
CharacterEventAnimationPlayer 透過對應 Factory 建立 EventView
  ↓
EventView.PlayAnimation()（Timeline 動畫）
  ↓
動畫完成 → 回收 EventView 到物件池
```

目前 GameplayView 仍採單一 Ally／單一 Enemy CharacterView。Summon 回傳可 Dispose 且可等待 Completion 的生命週期 Handle；GameplayView 在替換角色或戰鬥結束時負責停止並等待 Worker。多角色支援需於 T-013 改為按角色 Identity 管理獨立 View 與生命週期集合。

## 相關文件

- [GameView 視覺呈現層](GameView.md) — CharacterView 的父系統
- [EventView 事件視圖](EventView.md) — 數字動畫的具體實作
- [Character 角色系統](Character.md) — 角色的邏輯層
- [Factory 工廠系統](Factory.md) — 事件動畫的工廠
