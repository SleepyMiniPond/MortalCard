# Buff 圖示呈現

> 核對日期：2026-09-19

目前 BuffView 子系統提供玩家 Buff 圖示。卡牌 Buff 透過卡片詳情提示呈現，CharacterBuff 有資料快照不代表已有同等圖示容器。

[PlayerBuffView](../Assets/Scripts/GameView/BuffView/PlayerBuffView.cs) 依 Identity 訂閱 ViewModel，顯示層數及本地化提示，回收時清理訂閱。[PlayerBuffCollectionView](../Assets/Scripts/GameView/BuffView/PlayerBuffCollectionView.cs) 管理圖示集合，由 Factory 建立與回收。

新增／移除事件改變圖示集合；層數或 Session 等資料更新由 ViewModel 訂閱反映。提示顯示不執行 Buff 邏輯。

相關：[GameView](GameView.md)、[CardView](CardView.md)、[Factory](Factory.md)、[Player](Player.md)。
