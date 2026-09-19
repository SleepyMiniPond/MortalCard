# GameModel 核心遊戲邏輯

> 核對日期：2026-09-19

GameModel 管理戰鬥規則與可變實體，不依賴 View。Model 聚合事件，由 Presenter 取出並交給畫面；目前不是可用事件還原整場戰鬥的事件溯源系統。

## 回合流程

[GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs) 的主要順序：

1. 建立角色與牌堆，固定初始卡片名單；執行 BeforeGameStart、初始卡片 Initialize、AfterGameStart。
2. 建立新回合編號，執行 BeforeTurnStart、回合事件、雙方能量恢復、AfterTurnStart。
3. 在抽牌前後時機之間處理雙方抽牌；每張卡完成移動與生命週期效果後才處理下一張。
4. 敵人準備選牌，接著等待玩家行動，再執行敵人已選卡牌。
5. BeforeTurnEnd 後依序處理 Ally、Enemy 清手；每方先完成移牌，再派送全部 Preserved、全部 Discarded，最後執行 AfterTurnEnd。

勝負在流程檢查點判定，玩家死亡依主角色，不必等所有助戰角色死亡。能量恢復在回合開始；Recycle 是本次卡片出牌離場後的回收，不是回合結束掃描整個墓地。

## 計算與狀態

- [GameContextManager](../Assets/Scripts/GameModel/GameContextManager.cs) 管理 Library、Factory、戰鬥亂數與堆疊式選取作用域。反應來源不覆寫玩家 Selected 目標。
- [GameStatus](../Assets/Scripts/GameModel/GameStatus.cs) 持有目前玩家與可變 Entity 引用，不是深度不可變快照。
- [GameFormula](../Assets/Scripts/GameModel/GameFormula.cs) 組合傷害、治療與卡牌修正；通用整數運算與狀態範圍分別由 Value、Entity／Manager 負責。
- [GameEvent](../Assets/Scripts/GameModel/GameEvent.cs) 描述結果與畫面更新；[GameHistory](../Assets/Scripts/GameModel/GameHistory.cs) 仍是預留結構。

## AI 與亂數

[UseCardLogic](../Assets/Scripts/GameModel/EnemyLogic/UseCardLogic.cs) 依可負擔費用選牌；[SelectTargetLogic](../Assets/Scripts/GameModel/EnemyLogic/SelectTargetLogic.cs) 負責目標。主目標 ToAlly／ToEnemy 依 CurrentPlayer 視角；ToRandom 分支實際取候選集合第一個，尚未隨機抽選。既有卡牌子選取才使用戰鬥亂數洗牌取樣，待修正項目見 [TODO](TODO.md)。

[GameRandom](../Assets/Scripts/GameModel/GameRandom.cs) 由戰鬥種子建立並注入。固定種子與相同操作可重現已接入的亂數流程，但不代表重播功能已完成。

## 子系統

- [Action](Action.md)：意圖、結果與反應上下文。
- [Effect](Effect.md)：解析、排程、狀態變更與事件。
- [Target](Target.md)、[Value](Value.md)、[Condition](Condition.md)：組合式查詢。
- [Entity](Entity.md)、[Instance](Instance.md)：戰鬥狀態與跨戰鬥狀態。
- [Session](Session.md)：反應規則的暫態記憶。
