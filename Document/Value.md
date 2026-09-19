# Value 數值契約

> 核對日期：2026-09-19

IIntegerValue 回傳 Option<int>。真正的 0 是 Some(0)，目標缺失、無該能力或非法運算為 None，不以 0 偽裝缺值。

## 來源與運算

數值可來自常數、回合、實體／Buff 屬性、Session 或集合數量，也可組合算術、最小／最大值及條件選值。第一回合在 BeforeTurnStart 前即為 1；空集合數量是有效的 0；不存在的 Buff 與 Level 為 0 不同。

加、減、乘與特殊除法溢位採飽和運算；除法向下取整；除以零及餘數除以零為 None。最小／最大值至少需要一個子值，任一缺值則整體缺值。ConditionalValue 採第一組符合條件的值，無符合項為 None。

Value 是通用運算層，不套用傷害或資源限制。Resolver 驗證效果語意，Entity／Manager 維護最終狀態範圍。

## 驗證與使用

Effect 數值為 None 時不建立 Command／Result／Event。Condition 的缺值依葉條件處理，Session 不以缺值覆寫為 0。Validator 檢查無效列舉、空最小／最大集合，並攔截可由常數確定的零除或溢位；動態結果仍由 Runtime 處理。

布林 Value 提供 true／false 資料，配合布林條件使用。

程式入口：[IntegerValue](../Assets/Scripts/GameModel/Target/IntegerValue.cs)、[GameplayIntegerMath](../Assets/Scripts/GameModel/GameplayIntegerMath.cs)、[BooleanValue](../Assets/Scripts/GameModel/Target/BooleanValue.cs)。目標與條件見 [Target](Target.md)、[Condition](Condition.md)。
