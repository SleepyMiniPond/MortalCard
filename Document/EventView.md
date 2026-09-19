# EventView 數字動畫

> 核對日期：2026-09-19

EventView 將傷害、治療、護盾、能量與好感度事件轉成數字動畫，不計算戰鬥結果。具體事件對應見 [CharacterEventAnimationPlayer](../Assets/Scripts/GameView/CharacterView/CharacterEventAnimationPlayer.cs) 及 [EventView 目錄](../Assets/Scripts/GameView/EventView/)。

## 資源生命週期

[BaseAnimationEventView](../Assets/Scripts/GameView/EventView/BaseAnimationEventView.cs) 使用 PlayableDirector 播放 Timeline，接收取消 Token；結束時停止播放並隱藏物件。外層播放者另保證回收到 Factory，兩者分別負責播放狀態及物件所有權。

事件進入角色動畫佇列後由 Worker 安排啟動，不保證 GameplayView 每分發一個事件就等動畫播完。參見 [CharacterView](CharacterView.md) 與 [Factory](Factory.md)。
