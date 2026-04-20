# Player 子系統 - 陣營資源與性情管理機制

## 🎯 子系統定位與職責

**Player 子系統是 GameModel\Entity 中負責陣營層級資源管理的核心機制**，與 Character 系統形成明確分工：**Player 管理陣營共享的戰略資源**（手牌、能量、牌庫等），**Character 管理個體單位狀態**。此專案的獨特之處在於引入了**性情機制**作為影響效果數值的特殊狀態系統。

## 📊 Player 系統架構設計

### 核心 Player 實體

#### 玩家核心實體
**[PlayerEntity.cs](Assets/Scripts/GameModel/Entity/Player/PlayerEntity.cs)** 戰鬥中陣營的完整表示
- **陣營標識**：透過 `Faction` 明確區分我方與敵方陣營
- **角色統籌**：管理陣營下的多個 `ICharacterEntity` 單位
- **資源整合**：整合能量管理、卡牌管理、Buff 管理等陣營資源
- **死亡判定**：當所有角色單位死亡時，陣營失敗
- **主角色系統**：透過 `MainCharacter` 設定陣營的核心角色單位

## ⚡ 能量管理系統

### 能量核心機制
**[EnergyManager.cs](Assets/Scripts/GameModel/Entity/Player/EnergyManager.cs)** 專門處理陣營的能量資源
- **能量狀態**：管理當前能量（Energy）與最大能量（MaxEnergy）
- **能量恢復**：`RecoverEnergy` 回合開始時的能量恢復機制
- **能量消耗**：`ConsumeEnergy` 使用卡牌時的能量消耗邏輯
- **能量增減**：`GainEnergy`/`LoseEnergy` 效果觸發的能量變化
- **溢出處理**：自動處理能量上限與溢出狀況

## 💭 性情管理系統（專案獨特機制）

### 性情核心機制
**[DispositionManager.cs](Assets/Scripts/GameModel/Entity/Player/DispositionManager.cs)** 管理影響效果數值的性情狀態
- **性情狀態**：管理當前性情（CurrentDisposition）與最大性情（MaxDisposition）
- **性情增加**：`IncreaseDisposition` 提升性情值，影響正面效果
- **性情減少**：`DecreaseDisposition` 降低性情值，可能削弱效果
- **數值影響**：性情高低直接影響各種效果的計算數值
- **上下限控制**：嚴格的數值範圍管理與溢出處理

## 🃏 卡牌管理系統

### 卡牌資源統籌
**[PlayerCardManager.cs](Assets/Scripts/GameModel/Entity/Player/PlayerCardManager.cs)** 統一管理陣營的所有卡牌資源
- **區域管理**：整合牌庫、手牌、墓地、排除、棄置等所有卡牌區域
- **卡牌流轉**：處理卡牌在不同區域間的轉移與狀態變化
- **卡牌操作**：提供抽牌、棄牌、消耗、創造等完整的卡牌操作
- **回合管理**：處理回合結束時的手牌清理與卡牌回收
- **使用追蹤**：透過 `PlayingCard` 追蹤正在使用的卡牌狀態

### 卡牌操作核心
- **卡牌使用**：`TryPlayCard` 處理卡牌使用的完整流程
- **卡牌創造**：`CreateNewCard` 支援效果創造的新卡牌
- **卡牌複製**：`CloneNewCard` 支援複製現有卡牌的機制
- **卡牌移動**：在不同區域間安全轉移卡牌

## 🛡️ PlayerBuff 增益系統

### PlayerBuff 核心實體
**[PlayerBuffEntity.cs](Assets/Scripts/GameModel/Entity/Player/PlayerBuff/PlayerBuffEntity.cs)** 陣營層級的增益效果管理
- **陣營增益**：影響整個陣營的持續性效果
- **等級疊加**：支援 Buff 的累積強化機制
- **反應會話**：複雜的觸發與反應邏輯支援

### PlayerBuff 管理器
**[PlayerBuffManager.cs](Assets/Scripts/GameModel/Entity/Player/PlayerBuff/PlayerBuffManager.cs)** 統一管理陣營的增益效果
- **Buff 生命週期**：完整的增益效果生命週期管理
- **觸發協調**：與其他系統的觸發事件協調
