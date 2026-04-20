# CharacterView 子系統 - 角色視覺與動畫表現機制

## 🎯 子系統定位與職責

**CharacterView 子系統是 GameView 中負責角色視覺呈現的核心機制**，提供盟友與敵人角色的統一視覺管理與動畫表現。此系統採用**基類繼承設計**與**事件驅動動畫**，確保不同陣營角色都能獲得一致且豐富的視覺回饋體驗。

## 📊 CharacterView 系統架構設計

### 繼承層次設計
**基類抽象層**：BaseCharacterView 提供通用的角色視覺邏輯與動畫框架  
**陣營特化層**：AllyCharacterView/EnemyCharacterView 實現特定陣營的視覺差異  
**事件動畫層**：多種 EventView 處理各類角色狀態變化的動畫效果  
**選擇互動層**：ISelectableView 支援角色的目標選擇與互動回饋

### 核心角色視覺架構

#### BaseCharacterView 基類設計
**[BaseCharacterView.cs](Assets/Scripts/GameView/CharacterView/BaseCharacterView.cs)** 角色視覺的核心基類
- **事件工廠整合**：整合傷害、治療、護盾、能量、性情等所有事件動畫工廠
- **動畫佇列管理**：透過 `_animationEventBuffer` 實現事件動畫的有序播放
- **時間控制機制**：`_minTimeInterval` 控制動畫播放的時間間隔，避免視覺混亂
- **非同步動畫**：基於 UniTask 的非同步動畫播放系統

### 陣營特化視覺系統

#### AllyCharacterView 盟友角色視覺
**[AllyCharacterView.cs](Assets/Scripts/GameView/CharacterView/AllyCharacterView.cs)** 盟友角色的專門視覺管理
- **盟友召喚**：`SummonAlly` 處理盟友角色的召喚與初始化
- **選擇支援**：實現 `ISelectableView` 支援盟友角色的目標選擇
- **身份追蹤**：透過 `_playerIdentity` 追蹤對應的角色實體
- **目標類型**：`TargetType.AllyCharacter` 明確標識為盟友目標

#### EnemyCharacterView 敵人角色視覺  
**[EnemyCharacterView.cs](Assets/Scripts/GameView/CharacterView/EnemyCharacterView.cs)** 敵人角色的專門視覺管理
- **敵人召喚**：`SummonEnemy` 處理敵人角色的召喚與初始化
- **選擇支援**：實現 `ISelectableView` 支援敵人角色的目標選擇
- **身份追蹤**：透過 `_playerIdentity` 追蹤對應的角色實體
- **目標類型**：`TargetType.EnemyCharacter` 明確標識為敵人目標

## 🎬 動畫事件系統

### 事件佇列機制
- **有序播放**：確保多個角色事件按順序播放，避免視覺衝突
- **時間控制**：透過 `_minTimeInterval` 控制事件間的時間間隔
- **非阻塞更新**：使用 UniTask 確保動畫不阻塞主執行緒

### 動畫事件分發
BaseCharacterView 提供統一的事件接收介面：
- **`UpdateHealth`**：接收生命值變化事件（傷害、治療、護盾）
- **`UpdateEnergy`**：接收能量變化事件（獲得、失去能量）
- **`UpdateDisposition`**：接收性情變化事件（提升、降低性情）

### 模式匹配動畫執行
- **類型安全**：透過模式匹配確保事件類型的正確處理
- **工廠模式**：每種動畫都透過對應的工廠進行創建與回收
- **資源管理**：動畫播放完成後自動回收，避免記憶體洩漏

## 🎯 目標選擇與互動

### ISelectableView 介面實現
CharacterView 實現統一的選擇介面：
- **`RectTransform`**：提供選擇區域的視覺邊界
- **`TargetType`**：明確標識目標類型（盟友/敵人）
- **`TargetIdentity`**：提供目標的唯一識別
- **`OnSelect`/`OnDeselect`**：處理選擇狀態的視覺回饋

### 陣營目標分類
- **AllyCharacter**：盟友角色目標，通常用於友善效果
- **EnemyCharacter**：敵人角色目標，通常用於攻擊效果

## 🔄 生命週期管理

### 角色召喚流程
```
1. 接收召喚事件 (SummonAlly/SummonEnemy)
2. 設定角色身份 (_playerIdentity)
3. 啟動動畫佇列系統 (_Run)
4. 開始接收並播放角色事件動畫
```

### 動畫播放週期
```
1. 事件加入佇列 (UpdateHealth/UpdateEnergy/UpdateDisposition)
2. 時間間隔控制 (_minTimeInterval)
3. 事件出隊並播放動畫 (_PlayHealthEventAnimation)
4. 工廠模式回收資源
```

## 🌐 與其他系統協作

### 與 GameModel 系統協作
**事件來源**：接收 GameModel 中角色狀態變化的事件資料

### 與 EventView 系統協作
**動畫支援**：依賴各種 EventView 工廠提供具體的動畫效果

### 與 Target 系統協作
**目標選擇**：作為 ISelectableView 參與遊戲的目標選擇機制

### 與 Factory 系統協作
**資源管理**：利用工廠模式實現高效的動畫元件生命週期管理
