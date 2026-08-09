# Card Transformation 卡片變身系統

> 最後更新：2026-08-10 | 版本：v1.2

## 文件定位

本文件描述已完成的 T-010 卡片形態架構，涵蓋 Self Transform、External Override、CardBuff Layer、區域反應邊界、View 互動安全，以及勝利時的批次 `CardInstanceChangeSet` 輸出。

## 形態層級

同一個 `CardEntity` 會保留三種形態來源：

1. **Base Form**：由原始 `CardInstance.CardDataId` 或 Runtime 建立時的 CardData ID 決定。
2. **Self Form**：由 Standard CardData 的 `CardTransformRule` 套用。
3. **External Override**：由外部 Effect 暫時強制套用 Override CardData。

Effective Form 的優先順序固定為：

```text
External Override > Self Form > Base Form
```

`CardEntity.CardDataId`、Type、Rarity、Theme、Cost、Power、選取規則、Effects 與 TriggeredEffects 都從目前 Effective Form 讀取。形態切換不會更換 `CardEntity.Identity`，也不會改變卡片所在區域。

## Self Transform

### 資料定義

只有 `StandardCardData` 能定義 `TransformRules`。每個 `CardTransformRule` 包含：

- `RuleId` 與 `TransformKey`。
- `Priority`。
- `GameTiming`。
- Conditions。
- Apply 或 Revert Operation。

Apply Operation 指定目標 Standard CardData 與 `CardFormPersistence`；Revert Operation 依 `TransformKey` 解除目前 Self Form。

同一 Timing 有多個規則成立時，Evaluator 先比較 Priority，Priority 相同時依資產陣列順序決定。一次 Timing 最多提交一個 Self Form Operation。

### 執行限制

- External Override 存在時不執行 Self Transform Rule。
- Apply 目標必須是 Standard CardData。
- 空白 Key、空白目標或不存在的 CardData 會被拒絕。
- 套用目前已生效的 CardData，或 Revert 不符合目前 `TransformKey` 的狀態，會回傳 No-op。

## External Override

External Override 由 `ApplyCardFormOverrideEffect` 建立，內容包含：

- Override Key 與目標 Override CardData。
- 套用來源 `IActionSource`。
- Release Rules。
- 專用 ReactionSessions。
- 對應的 CardBuff Override Layer Handle。

目前只保留一個 Override State。第二次套用不同 Override 時會直接取代第一個；新 Override 解除後不會回到被取代的舊 Override。同 Key 且同目標的重複套用為 No-op。

解除操作會核對 Override State Identity 與 Buff Layer Handle。已排隊的舊解除操作若遇到後來取代的新 State，會安全 No-op，不會誤刪新 Override。

`ObserveAction` 只更新 Override ReactionSession；只有明確的 Timing Dispatch 才會判斷 ReleaseRule 並排入解除操作。

## Property 與 CardBuff Layer

### Property

CardEntity 將 Property 區分為：

- **CardData Property**：隨 Effective Form 替換。
- **CardInstance Property**：Self Transform 期間保留。

External Override 存在時，CardInstance Property 暫停對外生效，只暴露 Override CardData Property；解除後再依恢復的 Effective Form 重建 CardData Property，並重新暴露原有 CardInstance Property。

### CardBuff

`CardBuffLayerManager` 管理 Base Layer 與可替換的 Override Layer：

- Self Transform 沿用 Base Layer。
- External Override 期間凍結 Base Layer，讀寫改由 Override Layer 處理。
- Override 期間新增的 CardBuff 只存在於 Override Layer。
- Override 被取代或解除時，舊 Override Layer 失效並移除其中 Buff。
- 已排隊但持有失效 LayerHandle 的 Buff Command 會安全 No-op。

PlayerBuff 不屬於 CardBuff Layer，因此仍可作用於 Override Form。

## Timing、事件與 Effect

`TimingDispatchPlanner` 依當次 Reaction Snapshot 建立一般 Reaction、Override Session／Release 與 Self Transform QueueItem。形態操作成功後：

1. 先原子更新 CardEntity 的形態狀態與 CardData Property。
2. 以新 Effective Form 建立最新 `CardInfo`。
3. 產生 `CardFormChangedEvent`，包含 Identity、前後 CardData ID、Key、Cause 與最新 CardInfo。
4. 若新 Effective Form 定義 `CardTriggeredTiming.FormChanged`，使用形態更新後的 Context 執行其 Effects。

沒有實際形態差異的操作不會產生 `CardFormChangedEvent`。

## 區域邊界

`PlayerCardManager.ReactionCards()` 是一般卡片 Update、Buff Timing、Self Transform 與 Override Release 的有效集合，包含：

- Deck。
- HandCard。
- Graveyard。
- ExclusionZone。
- PlayingCard。

DisposeZone 保留在全域查找與 CardManager 快照中，但不參與一般 Reaction Update 或 Form Rule。這可避免已銷毀卡片繼續更新 Buff、Session 或形態。

## CardInstance 持久形態

`CardInstance` 保留原始 `CardDataId`，並以可選的 `PersistentCardFormState` 記錄跨戰鬥 Self Form。

`CardEntity.CreateFromInstance()` 會以原始 CardData 建立 Base Form，再還原 Persistent Self Form。`CardInstancePersistenceMapper.TryUpdate()` 只有在 `OriginCardInstanceGuid` 與來源 Instance 相符時才寫回：

- `Persistent` Self Form 會寫入 `PersistentFormState`。
- `BattleOnly`、已 Revert 或沒有 Self Form 時會清除 `PersistentFormState`。
- External Override 永遠不寫回。

目前已完成單卡 Mapper、round-trip 與批次 `CardInstanceChangeSet` 收集。只有勝利會輸出 ChangeSet；失敗、Retry、Restart、Quit 與生命週期取消都不輸出，也不修改卡片狀態。

## Clone

`Clone()` 以來源卡片當下的 Effective CardData 建立新的 Base Form，但不複製：

- 原卡 Identity 與 `OriginCardInstanceGuid`。
- Self Form 或 Override State。
- CardInstance Property。
- CardBuff Layer 內容。
- 卡片區域或 Dispose 狀態。

因此 Clone 是以當下形態為基底的獨立 Runtime Card。

## View 更新與互動安全

`GameplayView` 收到 `CardFormChangedEvent` 後，會以事件內的最新 `CardInfo` 更新 `GameViewModel`。View 以卡片 Identity 訂閱資料，不另外把舊 `CardInfo` 當作互動狀態：

- `CardView` 的 pointer、drag、click callback 只傳 Identity。
- `IGameViewModel.GetCardInfoOrNone()` 提供需要執行規則時的即時查詢。
- `AllyHandCardView` 的 Focus 與 Drag 狀態只保存 Identity。
- 拖曳中的卡片只訂閱目前拖曳 Identity；形態更新後立即重新驗證 `MainSelectable` 與合法目標。
- 舊目標失效時會 Deselect、清除選取並隱藏指向線；放開時再次查詢最新 CardInfo。
- `FocusCardDetailView` 與 `SingleCardDetailPopupPanel` 會更新 Buff／Keyword 提示。
- `AiCardView`、一般卡片清單及非手牌詳情使用相同 Identity 更新原則。

單一可替換訂閱使用 `SerialDisposable`；同一 UI scope 內的多個事件訂閱使用區域性 `CompositeDisposable` 聚合。

## 資料驗證

GameData Validator 會檢查 Self Transform 目標存在且為 Standard CardData，並驗證 External Override 的目標存在且為 Override CardData。External Override 另會檢查 Target、OverrideKey、ReleaseRules、可 Dispatch 的 Timing、巢狀 Condition、ReactionSession，以及 ReleaseRule 所引用的 SessionKey。

## 完成狀態與驗證

目前完整 EditMode 222 項全數通過，其中 T-010 74 項。已自動化覆蓋形態操作、持久化、Clone、Override／Buff Layer、ReleaseRule、PlayingCard、DisposeZone、ChangeSet 收集與資料 Validator 邊界，並以 A → B → C → B 整合案例驗證跨層狀態一致性。

T-010 功能與自動化驗收已完成。已知邊界：

- 勝利 ChangeSet 的戰鬥外實際套用與存檔流程（不屬於 T-010 必要範圍）。
- 專案尚無 Gameplay Prefab 互動測試基礎；拖曳中變形與 Focus 中變形的程式接線已完成，建議在正式內容資產可用時補一次非阻塞的場景人工 smoke test，或未來建立 View 測試 Harness。

## 相關文件

- [Card 卡牌系統](Card.md)
- [CardBuff 卡牌 Buff 系統](CardBuff.md)
- [Instance 實例層](Instance.md)
- [Effect 效果管線](Effect.md)
- [Action 與觸發來源](Action.md)
- [Target 目標系統](Target.md)
- [T-010 完成紀錄](TODO_Archive.md)
