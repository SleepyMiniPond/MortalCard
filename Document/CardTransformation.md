# 卡片形態系統

> 核對日期：2026-09-19

形態改變同一張卡的有效設計資料，不更換 Identity、區域或順序。有效形態優先順序固定為 External Override > Self Form > Base Form。

## Self Form

Base Form 來自原始 Instance 或 Runtime 建立時的 CardData。Self Transform 使用 Base Standard CardData 的規則，在指定 GameTiming 判斷條件；高 Priority 優先，相同時依配置順序，一次派送最多選一個操作。

Apply 目標須為 Standard CardData，Revert 只解除符合 TransformKey 的 Self Form；重複套用目前形態或無可還原狀態為 No-op。External Override 存在時暫停 Self Transform。

來源：[CardTransformRuleEvaluator](../Assets/Scripts/GameModel/Entity/Card/CardTransformRuleEvaluator.cs)、[CardEntity](../Assets/Scripts/GameModel/Entity/Card/CardEntity.cs)。

## External Override

Override 是單一可取代狀態，不是堆疊。不同的新 Override 取代舊狀態，解除後不恢復被取代的 Override；同 Key、同目標重複套用為 No-op。解除時核對狀態身份，舊排隊操作不會解除後來的新狀態。

Override 有自己的 Session 與解除規則。觀察 Action 只更新 Session，明確 Timing 派送才判斷解除。來源：[CardFormOverrideState](../Assets/Scripts/GameModel/Entity/Card/CardFormOverrideState.cs)、[TimingDispatchPlanner](../Assets/Scripts/GameModel/Effect/TimingDispatchPlanner.cs)。

## 屬性與 Buff

| 狀態 | CardData Property | Instance Property | CardBuff |
|---|---|---|---|
| Base／Self Form | 隨有效形態重建 | 保留並生效 | 使用 Base Layer |
| External Override | 使用 Override 資料 | 暫停對外生效 | 使用新的 Override Layer，凍結 Base |
| Override 解除 | 恢復目前 Self 或 Base | 恢復生效 | 丟棄 Override Layer，恢復 Base |

已失效 Layer 的排隊命令安全 No-op；PlayerBuff 不受此 Layer 切換限制。詳見 [CardBuff](CardBuff.md)。

## 事件與觸發的現況

形態事件提供最新 CardInfo，供畫面依 Identity 更新。事件存在不等於所有形態生命週期效果都已執行：

- Self Apply／Revert 與 Override 解除由 [CardFormQueueItems](../Assets/Scripts/GameModel/Effect/CardFormQueueItems.cs) 執行新有效 CardData 的 FormChanged 效果。
- [Override Apply Handler](../Assets/Scripts/GameModel/Effect/Handlers/ApplyCardFormOverrideEffectCommandHandler.cs) 在資料 ID 改變時產生 CardFormChangedEvent，目前沒有派送 FormChanged 效果。
- 上述路徑尚未使用共用 CardTriggeredEffectDispatch，因此未派送 CardBuff 的 FormChanged。

此不一致列於 T-025；不能將 T-010 完成紀錄解讀為上述後續整合已完成。

## Clone 與持久化

Clone 以來源當下有效 CardData 建立全新的 Base Form，不複製來源 Identity、Instance 關聯、Self／Override 狀態、Instance Property、Buff 或區域。

只有來源 Instance 相符的 Persistent Self Form 可收集為勝利 ChangeSet；BattleOnly 或已還原會清除持久形態，Override 不寫回。戰鬥外套用與磁碟存檔尚未接線，見 [Instance](Instance.md)。

## 畫面與驗證

卡片 View 依 Identity 查最新資料，拖曳中變形會重新驗證目標；詳情與焦點同步更新。見 [CardView](CardView.md)。

資料型別、目標、ReleaseRule 與 Session 引用由 [GameDataValidator](../Assets/Scripts/Editor/GameDataValidator.cs) 驗證。既有行為測試位於 [CardTransformation 測試](../Assets/Tests/EditMode/CardTransformation/)，歷史測試結果保留於 [TODO_Archive](TODO_Archive.md)，不代表本次重新執行。Gameplay Prefab 的完整互動驗收仍有待補足。
