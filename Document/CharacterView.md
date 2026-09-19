# CharacterView 角色呈現

> 核對日期：2026-09-19

角色 View 接收戰鬥數值事件，將動畫排程與具體呈現分開，讓場景結束或替換角色時可取消並等待清理。

## 責任與順序

[BaseCharacterView](../Assets/Scripts/GameView/CharacterView/BaseCharacterView.cs) 組裝 [CharacterAnimationWorker](../Assets/Scripts/GameView/CharacterView/CharacterAnimationWorker.cs) 與 [CharacterEventAnimationPlayer](../Assets/Scripts/GameView/CharacterView/CharacterEventAnimationPlayer.cs)。

Worker 按最小時間間隔啟動事件動畫並追蹤進行中的工作，動畫可以重疊；Dispose 發出取消，Completion 表達完全收斂或例外。Player 將事件對應至 Factory／EventView，完成或取消後回收。

Ally／Enemy CharacterView 亦可作為選取目標。GameplayView 目前只管理一個友軍及一個敵軍 View，替換與結束時等待舊 Worker 清理；多角色動態集合仍屬 T-013。

動畫實作見 [EventView](EventView.md)，排程測試見 [CharacterAnimationWorkerTests](../Assets/Tests/PlayMode/CharacterAnimationWorkerTests.cs)。
