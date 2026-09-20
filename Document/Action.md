# Action 與反應上下文

> 核對日期：2026-09-19

Action 描述意圖、目標或已發生的結果，Effect Command 才是狀態變更指令。Action 多以 Record 表達，但其中可參考可變 Entity／屬性容器，不代表整張物件圖不可變。

## 意圖與結果

效果可經過 Intent → TargetIntent → Result，分別表達整體意圖、特定目標與實際結果。具體效果會使用其中適用的階段，不保證每個操作都產生完整三階 Action。

ObserveRootAction／ObserveDerivedAction 更新實體、壽命與 Session；可執行的 Buff 效果另由 Timing Planner 排入 Queue。不能把「觀察 Action」一律描述為立即執行 Buff 效果。來源見 [GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs)、[Action 定義](../Assets/Scripts/GameModel/Action/)、[TimingDispatchPlanner](../Assets/Scripts/GameModel/Effect/TimingDispatchPlanner.cs)。

## 三種視角

| 資料 | 語意 |
|---|---|
| Action.Source／Target | 發生何事、由誰發起、作用於誰 |
| TriggerContext.Triggered | 目前正在回應該事件的卡片、玩家或 Buff |
| GameContext.Selected* | 玩家或 AI 出牌操作中明確選擇的主目標與 ExistCard 群組結果 |

[TriggerContext](../Assets/Scripts/GameModel/Action/TriggerSource.cs) 沿衍生反應保留最初 ReactionOriginAction／Timing，避免後續效果失去根源。Buff Trigger 保存宿主，不能以 CurrentPlayer 或覆寫 Selected 目標代替。

每次出牌會建立完整且獨立的選取作用域；主目標與 ExistCard 群組結果在結算結束後一併還原，不沿用上一張牌的 Selected 欄位。群組結果以不可變卡片 Identity 集合保存，Effect 於執行時再解析仍存在的卡片。

## 出牌與卡片時機

[CardPlaySource](../Assets/Scripts/GameModel/Action/ActionSource.cs) 保存出牌卡、手牌位置與本次屬性修正；結果 Source 聚合普通效果及 Played 的 Result。

CardTriggeredTimingAction 表達某張卡的生命週期，其 GameTiming 維持 None，兩套時機不混用。共用派送已接入的時機見 [Card](Card.md)。FormChanged 仍使用 CardFormChangedAction 的 CardData 專用路徑；EffectPlayed 尚無正式出牌入口。

目標及值的查詢契約見 [Target](Target.md)、[Value](Value.md)、[Condition](Condition.md)，排程與副作用見 [Effect](Effect.md)。
