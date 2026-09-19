# Info 狀態面板

> 核對日期：2026-09-19

Info 面板顯示回合、血量、護盾、能量、好感度及玩家 Buff。AllyInfoView 組合友軍資訊，EnemyInfoView 沒有好感度面板。

更新有兩條途徑：血量、能量與回合由 GameplayView 分發事件更新；好感度等資料透過 ViewModel 訂閱。不能將所有 Info 元件描述為同一套響應式更新。

元件責任與細節見 [Info 目錄](../Assets/Scripts/GameView/Panel/Info/)；事件接線見 [GameplayView](../Assets/Scripts/GameView/GameplayView.cs)。好感度名稱及效果由 DispositionLibrary 與本地化資料提供，Buff 圖示見 [BuffView](BuffView.md)。
