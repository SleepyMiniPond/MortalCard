# Factory 工廠系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Factory 系統為 GameView 層提供**物件池管理**，解決 Unity 遊戲中頻繁的 Prefab 實例化/銷毀導致的 GC 波動問題。所有動態 UI 元件（卡牌、Buff 圖示、數字動畫）都透過工廠建立和回收。

## PrefabFactory<T> — 泛型物件池

核心基礎類別，提供堆疊式的物件池管理：

### 運作機制

```
CreatePrefab()
  → 池中有閒置物件？ → Pop 並啟用
  → 池中沒有？ → Instantiate 新物件

RecyclePrefab(T)
  → 呼叫 Reset() 清理狀態
  → 重設父物件到池根節點
  → Push 回池中
```

### IRecyclable 契約

所有可回收的元件都實作 `IRecyclable` 介面，提供 `Reset()` 方法在回收前清理狀態。

## 特化工廠列表

### 卡牌工廠
| 工廠 | 產品 | 用途 |
|------|------|------|
| CardViewFactory | CardView | 友軍手牌卡牌 |
| AiCardViewFactory | AiCardView | 敵人選定卡牌 |

### Buff 工廠
| 工廠 | 產品 | 用途 |
|------|------|------|
| BuffViewFactory | PlayerBuffView | Buff 圖示 |

### 事件動畫工廠（8 個）
| 工廠 | 產品 | 用途 |
|------|------|------|
| DamageEventViewFactory | DamageEventView | 傷害數字 |
| HealEventViewFactory | HealEventView | 治療數字 |
| ShieldEventViewFactory | ShieldEventView | 護甲數字 |
| GainEnergyEventViewFactory | GainEnergyEventView | 能量獲得 |
| LoseEnergyEventViewFactory | LoseEnergyEventView | 能量消耗 |
| IncreaseDispositionEventViewFactory | IncreaseDispositionEventView | 好感度增加 |
| DecreaseDispositionEventViewFactory | DecreaseDispositionEventView | 好感度減少 |

### 資訊工廠
| 工廠 | 產品 | 用途 |
|------|------|------|
| CardPropertyInfoViewFactory | — | 卡牌屬性資訊 |
| GameKeyWordInfoViewFactory | — | 遊戲關鍵字資訊（最小實作） |

## 設計價值

1. **效能**：避免頻繁的 Instantiate/Destroy 操作
2. **記憶體穩定**：物件重用減少 GC 壓力
3. **統一管理**：所有動態元件的生命週期一致
4. **擴展便利**：新增 View 類型只需新增對應的 Factory 子類

## 相關文件

- [GameView 視覺呈現層](GameView.md) — Factory 的使用者
- [CardView 卡牌視圖](CardView.md) — CardViewFactory 的客戶
- [EventView 事件視圖](EventView.md) — EventViewFactory 的客戶
- [BuffView Buff 視圖](BuffView.md) — BuffViewFactory 的客戶
