# CardBuff 子系統 - 卡牌反應式狀態效果機制

## 🎯 子系統定位與職責

**CardBuff 子系統是 GameModel\Entity 中負責卡牌反應式狀態管理的增益機制**，讓卡牌能夠根據戰鬥中的各種觸發條件產生額外的狀態修飾或效果增強。提供完整的 Buff 生命週期管理，支援疊加、時效控制與條件觸發等複雜增益邏輯。

## 📊 CardBuff 系統架構設計

### Buff 實體管理設計
**Buff 實體層**：管理單個 Buff 的狀態、等級與觸發邏輯
**生命週期層**：控制 Buff 的持續時間與過期機制  
**屬性修飾層**：處理 Buff 對卡牌屬性的修改與增強
**管理協調層**：統籌卡牌上所有 Buff 的集合管理與更新

### 核心 Buff 實體

#### CardBuff 核心實體
**[CardBuffEntity.cs](Assets/Scripts/GameModel/Entity/CardBuff/CardBuffEntity.cs)** 戰鬥中單個卡牌增益的完整表示
- **資料連結**：透過 `CardBuffDataID` 連接到 GameData 中的 Buff 定義
- **身份標識**：每個 Buff 實例都有唯一的 `Identity` 進行追蹤
- **等級管理**：支援 Buff 的疊加與等級變化機制
- **施法者追蹤**：記錄 Buff 的施加來源（Caster）
- **條件效果**：整合條件判斷與觸發時機的效果系統
- **反應會話**：透過 `ReactionSessions` 支援複雜的觸發邏輯

#### Buff 管理器
**[CardBuffManager.cs](Assets/Scripts/GameModel/Entity/CardBuff/CardBuffManager.cs)** 統一管理卡牌上的所有 Buff
- **集合管理**：維護卡牌上所有 Buff 實體的集合狀態
- **Buff 操作**：提供 Buff 的添加、移除與查詢功能
- **生命週期更新**：統一處理所有 Buff 的時間更新與過期檢查
- **結果回饋**：返回詳細的 Buff 操作結果與狀態變化

## ⏰ 生命週期管理系統

### Buff 持續時間控制
**[CardBuffLifeTimeEntity.cs](Assets/Scripts/GameModel/Entity/CardBuff/CardBuffLifeTimeEntity.cs)** 提供多樣化的 Buff 持續機制

#### 持續類型分類
**永久 Buff**：`AlwaysLifeTimeCardBuffEntity` 永不過期的持續性增益
**回合 Buff**：`TurnLifeTimeCardBuffEntity` 基於回合數的時效性 Buff
**條件 Buff**：支援基於特定觸發條件的生命週期控制

### 生命週期更新機制
- **過期檢查**：`IsExpired()` 檢查 Buff 是否已到期
- **狀態更新**：`Update(TriggerContext)` 基於遊戲狀態更新生命週期
- **Clone 支援**：完整的生命週期實體複製機制

## 🔧 Buff 屬性修飾系統

### 卡牌屬性修改
**[CardBuffPropertyEntity.cs](Assets/Scripts/GameModel/Entity/CardBuff/CardBuffPropertyEntity.cs)** 管理 Buff 對卡牌屬性的修飾效果

#### 屬性修飾分類
**封印效果**：`SealedCardBuffPropertyEntity` 阻止卡牌的使用或觸發
**威力增強**：`PowerCardBuffPropertyEntity` 修改卡牌的攻擊力數值
**動態計算**：基於 `TriggerContext` 的屬性修飾動態評估

### 關鍵字系統
透過 `Keywords` 機制提供 Buff 效果的文字描述與識別標籤

## 💡 反應式觸發機制

### 條件觸發設計
CardBuff 與 Condition 系統深度整合，支援複雜的觸發條件判斷

### 時機控制系統
透過 `CardTriggeredTiming` 精確控制 Buff 效果的觸發時機

### 反應會話管理
`ReactionSessions` 提供複雜的反應邏輯與持續效果管理

## 🔄 Buff 操作流程

### Buff 添加流程
1. **資料檢索**：根據 `buffId` 從 CardBuffLibrary 獲取定義
2. **等級處理**：檢查是否已存在同類 Buff 並處理疊加邏輯
3. **實體創建**：創建新的 CardBuffEntity 實例
4. **生命週期初始化**：設定 Buff 的持續時間與過期條件
5. **結果回饋**：返回詳細的添加結果與狀態變化

### Buff 更新機制
- **批次更新**：CardBuffManager 統一處理所有 Buff 的生命週期更新
- **過期清理**：自動移除已過期的 Buff 實體
- **狀態同步**：確保 Buff 狀態與遊戲進程的同步性
