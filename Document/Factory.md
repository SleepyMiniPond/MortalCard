# Factory 子系統 - View 元件工廠與物件池

## 🎯 子系統定位與職責

**Factory 子系統是 GameView 中負責 View 元件創建與回收的工具層**，提供統一的物件池管理，優化 View 元件的記憶體使用與創建效能。

## 🏭 工廠系統架構

### PrefabFactory 基礎工廠
**[PrefabFactory.cs](Assets/Scripts/GameView/Factory/PrefabFactory.cs)** 提供泛型物件池實現
- **物件池管理**：`Stack<T>` 實現 View 元件的重用機制
- **創建邏輯**：優先從池中取用，不足時創建新實例
- **回收機制**：透過 `IRecyclable.Reset()` 重置後回收

### 專門工廠列表
- **CardViewFactory** - 卡牌 View 元件工廠
- **BuffViewFactory** - Buff View 元件工廠  
- **EventView 工廠群** - 各種事件動畫的專門工廠
  - DamageEventViewFactory、HealEventViewFactory
  - GainEnergyEventViewFactory、LoseEnergyEventViewFactory
  - IncreaseDispositionEventViewFactory、DecreaseDispositionEventViewFactory
- **資訊 View 工廠** - CardPropertyInfoViewFactory、GameKeyWordInfoViewFactory
