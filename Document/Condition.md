# Condition 條件系統

> 核對日期：2026-09-19

Condition 在 TriggerContext 中回傳布林結果，供效果、反應、形態及 Session 規則共用。條件讀取狀態，不修改狀態或消耗亂數；以可組合積木表達內容，不為單一卡牌建立專用條件。

## 組合與缺值

All／Any／Inverse 組合其他條件。依賴的單一 Target 或 Value 缺失時，該葉條件回傳 false；外層仍依邏輯運算，例如 Inverse 可反轉結果。空白必要條件清單及缺少子條件由 Validator 攔截。

GameTimingCondition 比較反應鏈最初的 ReactionOriginTiming，非 Timing 根源或 None 不匹配。

## 身份、狀態與集合

Card Identity 判斷同一戰鬥實體；Base CardData 判斷不受變形影響的原始資料；Card Form 及類型、主題、稀有度、屬性讀取有效形態。

玩家死亡依 MainCharacter；角色條件則查詢指定角色。Buff 條件可判斷 ID 與集合持有關係，0 層與不存在不同。

卡片集合 Contains 使用 Identity；Any／All 都要求至少一張卡，空集合皆為 false。同一卡的多個條件採 AND，避免把空集合誤判成符合全部條件。

## 出牌與 Session

CardPlay 條件讀取出牌位置與來源；CardPlayResult 條件讀取效果結果及目標。Session 條件讀取目前有效的記憶值，不負責觸發更新；有效期間見 [Session](Session.md)。

## 程式導引

[ModelCondition](../Assets/Scripts/GameModel/Condition/ModelCondition.cs) 提供組合及領域條件入口，[Condition 目錄](../Assets/Scripts/GameModel/Condition/) 提供各類值判斷。必要引用、集合與列舉語意由 [GameDataValidator](../Assets/Scripts/Editor/GameDataValidator.cs) 驗證。

資料來源與缺值傳遞見 [Target](Target.md)、[Value](Value.md)。
