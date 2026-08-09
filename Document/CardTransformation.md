# Card Transformation 卡片變身現況

> 最後更新：2026-07-24 | 版本：v1.0

## 文件定位

本文件記錄目前程式碼中已存在的卡片變身基礎、資料流與限制。T-010 尚未實作，因此本文件不把討論中的目標 API 或資料結構描述成現有功能。

尚未實作的 T-010 設計草稿依專案規範存放於 `.agents/working/`，待功能完成後再依實際程式更新本文件。

## 現有變身基礎

### CardEntity 的資料引用

目前 [CardEntity](../Assets/Scripts/GameModel/Entity/Card/CardEntity.cs) 同時保存：

- `_mainCardDataId`：建立 CardEntity 時傳入的原始 CardData ID。
- `_mutationCardDataIds`：預留給形態切換使用的字串 List。

CardEntity 以 `_mutationCardDataIds` 的第一筆作為目前生效的 CardData ID；List 為空時則回退至 `_mainCardDataId`。目前程式將這個結果稱為 Acting CardData。

下列資料已統一代理至 Acting CardData：

- CardData ID。
- 卡片類型、稀有度與主題。
- 基礎 Cost 與 Power。
- 主目標與子目標選取規則。
- 主動 Effects。
- TriggeredEffects。

這代表 Model 已具備「替換目前 CardData 後，大部分靜態卡片內容一起切換」的基礎。

### Identity 與執行期元件

CardEntity 的 Identity、OriginCardInstanceGuid、CardBuffLayerManager 與 CardManager 區域歸屬，不是 Acting CardData 的一部分。

因此，如果未來在同一個 CardEntity 上切換 Acting CardData，而非刪除並重建 CardEntity，既有架構可以保留：

- 戰鬥 Identity。
- CardInstance 來源關聯。
- CardBuffLayerManager 及其中的 Buff。
- CardManager 中的物件參考、所在區域與排列位置。

目前尚未存在公開的變身操作，因此上述內容是既有物件結構提供的能力，不代表完整變身流程已完成。

## CardProperty 現況

CardEntity 建立時，會將兩種來源的 Property 一次轉換並合併至 `_properties`：

1. 原始 CardData 的 `PropertyDatas`。
2. CardInstance 的 `AdditionPropertyDatas`。

合併後的集合不再保留來源資訊，而且不會隨 Acting CardData 動態重建。這造成目前變身基礎的主要缺口：

- 切換 Acting CardData 時，原始 CardData 的 Property 不會自動消失。
- 新形態 CardData 的 Property 不會自動加入。
- 系統無法只替換 CardData Property，同時保留 CardInstance Property。

CardBuff 提供的 Property 保存在各 CardBuffEntity 中，並未合併進 CardEntity 的 `_properties`。`CardInfo.Create()` 會在建立畫面資料時，再合併 CardEntity Property 與 CardBuff Property。

## CardInfo 與畫面資料

[CardInfo](../Assets/Scripts/GameModel/Info/CardInfo.cs) 會從 CardEntity 讀取：

- Acting CardData ID 與分類資料。
- Acting CardData 的基礎 Cost／Power，再經公式計算最終數值。
- 主目標資訊。
- CardBuff 資訊。
- CardEntity 與 CardBuff 提供的 Property／Keyword。

因此，只要 CardEntity 的 Acting CardData 與 Property 來源正確，重新建立 CardInfo 就能得到一致的新形態資料。

目前尚未存在專用的 CardTransformedEvent，也沒有已完成的 Presenter／View 變身更新流程。

## TriggeredEffects 現況

[CardData](../Assets/Scripts/GameData/Card/CardData.cs) 已有 `TriggeredCardEffect`，資料包含 `CardTriggeredTiming` 與 Effects。

CardEntity 的 `TriggeredEffects` 會讀取 Acting CardData，因此形態切換後，對外暴露的 TriggeredEffects 也會跟著切換。原始 CardData 的 TriggeredEffects 不會跨形態保留。

目前程式中尚未找到將 CardData TriggeredEffects 完整送入 Effect Queue 執行的流程。已接入全域 GameTiming Queue 的反應效果主要來自 PlayerBuff、CharacterBuff 與 CardBuff。

## CardInstance 現況

[CardInstance](../Assets/Scripts/GameModel/Instance/CardInstance.cs) 目前只保存：

- InstanceGuid。
- 原始 CardDataId。
- AdditionPropertyDatas。

它沒有目前形態或持久變身狀態欄位，因此戰鬥中的 Acting CardData 無法寫回並於下一場戰鬥恢復。

## Clone 現況

CardEntity 的 `Clone()` 目前以 `_mainCardDataId` 建立複製卡，且不複製 `_mutationCardDataIds`。

因此，若來源卡片未來透過既有預留欄位切換形態，Clone 仍會回到來源的原始 CardData，而不是複製當下形態。這與 T-010 討論後的 Clone 規格不同，實作時需要調整。

## 尚未具備的功能

目前變身只是一組預留欄位與 Acting CardData 代理，尚未具備：

- TransformRule 資料。
- Apply／Revert 領域操作。
- 規則 Timing、Condition、Priority 與順序判定。
- CardData Property 與 CardInstance Property 的來源分離。
- 專用變身事件與觸發時機。
- CardInstance 的跨戰鬥形態保存。
- Clone 當下形態的明確行為。
- 變身資料驗證與 EditMode 測試。

## 相關文件

- [Card 卡牌系統](Card.md)
- [CardBuff 卡牌 Buff 系統](CardBuff.md)
- [Instance 實例層](Instance.md)
- [Effect 效果管線](Effect.md)
- [T-010 待辦事項](TODO.md)
