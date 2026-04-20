# Instance 子系統 - 遊戲局級物件管理機制

## 🎯 子系統定位與職責

**Instance 子系統是 GameModel 中負責遊戲局級物件管理的持久化機制**，管理從出發點到終點整局遊戲過程中需要跨戰鬥保持的物件。與 Entity 子系統形成對比：Instance 處理**跨戰鬥的持久性物件**，而 Entity 處理**單場戰鬥內的臨時物件**。

## 📊 Instance 系統架構設計

### 持久性物件分層設計
**玩家層級**：整局遊戲的玩家狀態與進度資訊
**收藏層級**：玩家在整局遊戲中獲得並保持的資源（如卡牌收集）
**跨戰鬥狀態**：需要在多場戰鬥間保持的玩家屬性與配置

### 核心 Instance 類型

#### 盟友實例 (Ally Instance)
**[AllyInstance.cs](Assets/Scripts/GameModel/Instance/AllyInstance.cs)** 管理玩家角色的局級狀態
- **基礎屬性**：生命值、能量值、好感度等跨戰鬥保持的數值
- **收集資源**：牌組 `Deck` 作為玩家在整局遊戲中收集的卡牌集合
- **配置資訊**：手牌上限等影響戰鬥的基礎設定
- **身份標識**：透過 `Guid Identity` 確保實例的唯一性與追蹤

#### 卡牌實例 (Card Instance)  
**[CardInstance.cs](Assets/Scripts/GameModel/Instance/CardInstance.cs)** 管理玩家收集卡牌的個體狀態
- **靜態資料連接**：透過 `CardDataId` 連結到 GameData 中的卡牌定義
- **動態屬性擴展**：`AdditionPropertyDatas` 支援卡牌的個性化強化與修改
- **實例追蹤**：每張卡牌實例都有獨特的 `InstanceGuid`，支援精確管理
- **工廠創建**：提供 `Create` 方法從 CardData 生成新的卡牌實例

## 🔧 跨戰鬥管理機制

### 狀態持久化設計
Instance 物件使用 Record 類型確保資料不可變性，適合序列化與狀態管理

### 實例化追蹤系統
所有 Instance 物件都配備 Guid 標識，支援跨場景的物件追蹤與狀態同步

### 動態擴展支援
透過 `AdditionPropertyDatas` 等機制，支援遊戲過程中的物件屬性動態調整

## 💡 與 Entity 系統的分工

### Instance 特徵
- **生命週期**：整局遊戲的開始到結束
- **數量規模**：相對較少，主要是核心持久性資源
- **典型物件**：玩家角色、收集的卡牌、遊戲進度狀態

### Entity 特徵  
- **生命週期**：單場戰鬥的開始到結束
- **數量規模**：相對較多，包含所有戰鬥內臨時物件
- **典型物件**：戰鬥中的 Buff、臨時卡牌、戰鬥狀態
