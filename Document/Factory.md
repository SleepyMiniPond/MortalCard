# View 物件池

> 核對日期：2026-09-19

[PrefabFactory](../Assets/Scripts/GameView/Factory/PrefabFactory.cs) 重用卡牌、Buff 圖示與數字動畫物件，減少反覆建立與銷毀。各特化 Factory 指定產品，完整清單見 [Factory 目錄](../Assets/Scripts/GameView/Factory/)。

取得物件時優先從池取出，池空才 Instantiate。Factory 不保證取出即啟用，顯示狀態由使用端／View 控制。回收時呼叫 IRecyclable.Reset、移至回收節點，再放回池中。

Reset 契約要求元件清理自己的狀態，但是否解除訂閱或隱藏仍須查看各實作。動畫另外由播放端以 finally 保證回收，見 [EventView](EventView.md)；不能將物件池本身描述成完整生命週期保證。
