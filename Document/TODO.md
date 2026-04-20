# 專案待辦事項

> 最後更新：2026-04-21  
> 狀態標記：⬜ 未開始 | 🔄 進行中 | ✅ 已完成

---

## 🔴 高優先 — 架構改善

### T-001：消除 Switch Expression 雙重派發
- **問題**：`EffectDataResolver` 與 `EffectCommandExecutor` 各有 15~17 分支的 type-switch，新增效果至少要改 4 個檔案（Effect 定義、Resolver 分支、Command 定義、Executor 分支），違反 Open-Closed Principle
- **方向**：
  - 方案 A：讓 `ICardEffect` 自帶 `Resolve()` / `IEffectCommand` 自帶 `Execute()`，把邏輯內聚到效果本身
  - 方案 B：Strategy Dictionary（`Dictionary<Type, IEffectResolver>`）做中間層，保持 data class 純淨
  - 方案 C：Visitor Pattern 分離資料與行為但仍集中註冊
- **影響檔案**：`EffectDataResolver.cs`、`EffectCommandExecutor.cs`、`CardEffect.cs`、`EffectCommand.cs`
- **狀態**：⬜ 未開始

---

## 🟡 中優先 — 功能補完

### T-002：接通 CardBuff / CharacterBuff 觸發管線
- **問題**：`GameplayManager._TriggerTiming()` 中角色 Buff 和卡牌 Buff 的 foreach 迴圈是空殼，只有 PlayerBuff 真正接入反應管線
- **方向**：參照 PlayerBuff 的觸發邏輯，為 CardBuff 和 CharacterBuff 補上 `ConditionalEffect` 的解析與執行
- **注意**：CardBuff scope 較小（只影響單張牌），TriggerContext 需正確攜帶該 Card 作為 Source
- **影響檔案**：`GameplayManager.cs`（`_TriggerTiming` 方法）
- **狀態**：⬜ 未開始

### T-003：評估 Effect Queue 機制
- **問題**：目前效果是 `List<ICardEffect>` 線性展開成 `EffectCommandSet` 一次性執行，無法支援效果鏈（效果 A 結果影響效果 B）、效果中觸發新效果、效果取消/替代
- **方向**：引入 Effect Queue，讓效果執行中可以往佇列塞新效果；`GameContextManager` 的 stack-based scoping 已有雛形
- **前置**：先完成 T-001（消除 switch 派發後更容易引入 Queue）
- **狀態**：⬜ 未開始

---

## 🟣 新功能 — 卡牌系統擴展

### T-010：卡片變身（保留狀態）
- **需求**：卡片能夠變成另一張卡片，但保留既有狀態（如降費、已附加的 Buff）
- **設計思路**：
  - CardEntity 已有 `_mutationCardDataIds` 欄位，暗示變身機制的雛形已存在
  - 變身 = 切換 `_actingCardDataId`，但保留 `CardBuffManager` 和 `CardPropertyEntity` 的現有狀態
  - 需要定義哪些屬性跟著原卡、哪些跟著新卡（如：費用修改保留，基礎效果換新）
- **狀態**：⬜ 未開始

### T-011：多步驟自訂目標選取
- **需求**：卡片打出時顯示 UI 讓玩家依序選擇多次不同目標（例如先從手牌選 1 張，再從牌堆選 3 張）
- **設計思路**：
  - 現有 `SubSelectionPresenter` 已支援單次子選取，需擴展為**有序多步驟選取佇列**
  - 每一步定義：來源區域（手牌/牌堆/墓地/場上）、選取數量、篩選條件、顯示說明
  - `ISubSelectionGroup` 可以擴展為有序列表，依序彈出選取面板
  - 選取結果按步驟 ID 存入 `ISubSelectionAction` 字典
- **狀態**：⬜ 未開始

### T-012：卡片合成系統（自訂藥水）
- **需求**：類似爐石的自訂藥水 — 玩家先收集效果片段，打出合成卡時進行多次 N 選 1，組合成一張新卡
- **設計思路**：
  - 需要新的 CardProperty 或 CardBuff 類型來記錄「已收集的效果片段」
  - 合成打出時觸發特殊的 SubSelection 流程（多輪 N 選 1）
  - 選取完成後根據組合結果，動態建立新的 CardInstance（可能搭配 T-010 變身機制）
  - 或者：合成結果對應預定義的 CardData 組合表（較簡單但彈性低）
- **前置**：T-011（多步驟選取）、T-010（卡片變身）
- **狀態**：⬜ 未開始

### T-013：戰鬥中敵人動態增減
- **需求**：戰鬥中能夠新增敵人（增援）或移除敵人（逃跑）
- **設計思路**：
  - 目前 `PlayerEntity.Characters` 是初始化時建立的固定陣列
  - 需要改為動態集合（`List` 或 `ReactiveCollection`）
  - 新增敵人：運行時建立 CharacterEntity 插入集合，View 層需動態生成 CharacterView
  - 敵人逃跑：標記角色離場（非死亡），移出戰鬥計算，View 層播放離場動畫
  - 影響層面廣：目標解析（`ITargetCharacterCollectionValue`）、勝負判定、EnemyLogic、View 排版
- **狀態**：⬜ 未開始

---

## 建議執行順序

```
T-001（消除 Switch 派發）
  ↓
T-002（接通 Buff 管線）  +  T-010（卡片變身）
  ↓                            ↓
T-003（Effect Queue）     T-011（多步驟選取）
  ↓                            ↓
T-013（敵人動態增減）     T-012（卡片合成）
```

- T-001 是基礎設施改善，先做會讓後續所有功能的開發更輕鬆
- T-010 和 T-011 互相獨立，可以平行開發
- T-012 依賴 T-010 + T-011 的基礎
- T-013 相對獨立但影響面廣，建議架構穩定後再動
