# GameModel 核心遊戲邏輯

> 核對日期：2026-10-03

GameModel 管理戰鬥規則與可變實體，不依賴 View。Model 聚合事件，由 Presenter 取出並交給畫面；目前不是可用事件還原整場戰鬥的事件溯源系統。

## 回合流程

[GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs) 的主要順序：

1. 建立角色與牌堆，固定初始卡片名單；執行 BeforeGameStart、初始卡片 Initialize、AfterGameStart。
2. 建立新回合編號，執行 BeforeTurnStart、回合事件、雙方能量恢復、AfterTurnStart。
3. 在抽牌前後時機之間處理雙方抽牌；每張卡完成移動與生命週期效果後才處理下一張。
4. 敵人準備選牌，接著等待玩家行動，再執行敵人已選卡牌。
5. BeforeTurnEnd 後依序處理 Ally、Enemy 清手；每方先完成移牌，再派送全部 Preserved、全部 Discarded，最後執行 AfterTurnEnd。

勝負在流程檢查點判定，玩家死亡依主角色，不必等所有助戰角色死亡。能量恢復在回合開始；Recycle 是本次卡片出牌離場後的回收，不是回合結束掃描整個墓地。

## 單張出牌與出牌鏈

[CardPlayExecutor](../Assets/Scripts/GameModel/Effect/CardPlayExecutor.cs) 集中處理單張卡的合法性、付費、普通效果重複、出牌觸發、結果與離場清理。GameplayManager 保留選取作用域、成功通知、勝負檢查與出牌鏈協調；成功通知仍在本次選取作用域內發出。

出牌準備先確認卡片、重複次數及主動出牌費用，成功後才進入 PlayingCard 並付費。主流程明示開始／結束時機與出牌觸發；普通效果依已決定的次數執行，每次重複仍讀取當前形態的效果。中止路徑在 PlayingCard 作用域釋放後同步真實離場狀態，保留已發生的支付與效果，不補發成功結果。

Executor 將事件記入傳入的 CardPlayChain，效果及時機沿用 Model 的執行入口，共用原本的佇列與預算。FIFO 請求在目前卡片完成後由 GameplayManager 繼續處理；原地卡效 InvokeCardEffects 維持獨立的普通效果展開路徑。出牌與根批次的事件歸屬及順序見 [Effect](Effect.md)。

## 計算與狀態

- [GameContextManager](../Assets/Scripts/GameModel/GameContextManager.cs) 管理 Library、Factory、戰鬥亂數與堆疊式選取作用域。反應來源不覆寫玩家 Selected 目標。
- [GameStatus](../Assets/Scripts/GameModel/GameStatus.cs) 持有目前玩家與可變 Entity 引用，不是深度不可變快照。
- [GameFormula](../Assets/Scripts/GameModel/GameFormula.cs) 組合傷害、治療與卡牌修正；通用整數運算與狀態範圍分別由 Value、Entity／Manager 負責。
- [GameEvent](../Assets/Scripts/GameModel/GameEvent.cs) 描述結果與畫面更新；[GameHistory](../Assets/Scripts/GameModel/GameHistory.cs) 仍是預留結構。

## AI 與亂數

[UseCardLogic](../Assets/Scripts/GameModel/EnemyLogic/UseCardLogic.cs) 依可負擔費用選牌；[SelectTargetLogic](../Assets/Scripts/GameModel/EnemyLogic/SelectTargetLogic.cs) 負責目標。自動主目標使用顯式選取視角；候選範圍以 ToAlly／ToEnemy／Any 表達，First／Random 為獨立挑選策略。主目標 Context 建立後才計算 ExistCard 子選取，Random 與子選取共用戰鬥亂數。

[GameRandom](../Assets/Scripts/GameModel/GameRandom.cs) 由戰鬥種子建立並注入。固定種子與相同操作可重現已接入的亂數流程，但不代表重播功能已完成。

選取入口必須傳入 caster，主目標、ExistCard 候選與數量查詢沿用同一視角；正式出牌傳出牌玩家，Invoke 傳本次施放者，不以可省略的 null 猜測卡片持有者。原地卡效的施放者與略過條件見 [Effect](Effect.md#原地卡效-invokecardeffects)。

## 子系統

- [Action](Action.md)：意圖、結果與反應上下文。
- [Effect](Effect.md)：解析、排程、狀態變更與事件。
- [Target](Target.md)、[Value](Value.md)、[Condition](Condition.md)：組合式查詢。
- [Entity](Entity.md)、[Instance](Instance.md)：戰鬥狀態與跨戰鬥狀態。
- [Session](Session.md)：反應規則的暫態記憶。
