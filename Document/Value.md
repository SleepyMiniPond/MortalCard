# Value 數值與集合查詢系統

> 最後更新：2026-09-06 | 版本：v1.0

## 設計目的

Value 系統以可序列化的積木描述整數與布林資料來源。`IIntegerValue.Eval` 回傳 `Option<int>`：可取得的數值（包括真正的 `0`）為 `Some`，找不到 Target、無法讀取能力或非法運算為 `None`。不得以 `0` 偽裝缺值。

## 整數來源

```
IIntegerValue
├── ConstInteger
├── TurnCountInteger
├── CardCollectionCountInteger
├── ArithmeticInteger
├── MinimumInteger／MaximumInteger
├── CardIntegerProperty
├── PlayerIntegerProperty
├── CharacterIntegerProperty
├── CardBuffIntegerProperty
├── PlayerBuffIntegerProperty
├── CharacterBuffIntegerProperty
├── PlayerBuffSessionInteger
└── ConditionalValue
```

- `TurnCountInteger` 於每回合 `BeforeTurnStart` 前即建立回合編號；第一回合讀值為 `1`。
- `CardCollectionCountInteger` 讀取集合數量；空集合是有效的 `Some(0)`。
- Card Value 可讀取有效 Power／Cost 與未套用修正的 Base Power／Cost。
- Player Value 可讀取 Max／Current Energy 與 Current Disposition；不具好感度能力的 Player 讀取 Disposition 時為 `None`。
- Character Value 可讀取 Current／Max Health 與 Current Shield。
- 三種 Buff Value 可讀取 Level；Buff 不存在與 Level 為 0 是不同狀態。

## 算術與範圍

`ArithmeticInteger` 支援 Add、Subtract、Multiply、Divide、Remainder。加、減、乘與特殊除法溢位使用 Saturating Arithmetic；除法採向下取整；除以零與餘數除以零回傳 `None`。`MinimumInteger`／`MaximumInteger` 至少需要一個子 Value，任一子 Value 為 `None` 時整體為 `None`。

`ConditionalValue` 依序檢查條件組合，回傳第一個符合 Pair 的 Value；沒有任何 Pair 符合時為 `None`。

Value 層只負責通用整數運算，不套用傷害、Buff 或資源領域限制。Effect Resolver、Entity 與 Manager 分別驗證效果語意與最終狀態不變量。

## 布林 Value

`IBooleanValue` 提供 `TrueValue` 與 `FalseValue`；布林條件可透過 `IsTrueCondition`、`IsFalseCondition`、`IsEqualCondition` 使用。

## 製作與驗證規則

- `None` 必須安全向上傳遞至 Condition、Session 與 Effect；Effect 數值為 `None` 時不建立 Command、Result、Event 或動畫資料。
- 最小／最大值不可為空，算術與比較列舉不可使用未定義值。
- Validator 可遞迴推導完全由常數組成的公式，並攔截常數零除與可確定的溢位；含動態 Value 的結果由 Runtime 邊界處理。
- 集合查詢與篩選規則見 [Target.md](Target.md)；條件組合與空集合語意見 [Condition.md](Condition.md)。
