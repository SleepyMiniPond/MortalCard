# EventView 子系統 - 遊戲效果動畫展演機制

## 🎯 子系統定位與職責

**EventView 子系統是 GameView 中負責遊戲效果視覺化展演的專門機制**，將各種遊戲事件轉換為直觀的數值動畫與視覺回饋。此系統採用**模板化設計**與**Timeline 動畫**，為玩家提供即時且豐富的遊戲效果視覺體驗。

## 📊 EventView 系統架構設計

### 模板化動畫設計
**介面抽象層**：IAnimationNumberEventView 定義統一的動畫播放契約  
**事件特化層**：針對不同事件類型的專門 EventView 實現  
**Timeline 整合層**：利用 Unity Timeline 系統提供專業動畫效果  
**回收管理層**：實現 IRecyclable 介面支援物件池高效回收

### 核心介面設計

#### IAnimationNumberEventView 統一介面
**[IHealthEventView.cs](Assets/Scripts/GameView/EventView/IHealthEventView.cs)** 定義事件動畫的核心契約
- **非同步動畫**：基於 UniTask 的非阻塞動畫播放
- **統一契約**：所有事件動畫都遵循相同的播放介面
- **簡潔設計**：專注於動畫播放的核心功能

### 事件動畫分類系統

#### 戰鬥相關事件動畫

##### DamageEventView 傷害動畫
**[DamageEventView.cs](Assets/Scripts/GameView/EventView/DamageEventView.cs)** 傷害效果的視覺化展演
- **數值顯示**：透過 `TextMeshProUGUI` 顯示具體傷害數值
- **Timeline 動畫**：使用 `PlayableDirector` 播放專業動畫效果
- **父容器設定**：動態設定動畫的播放位置
- **生命週期管理**：動畫播放前激活，播放後隱藏

##### HealEventView 治療動畫
**[HealEventView.cs](Assets/Scripts/GameView/EventView/HealEventView.cs)** 治療效果的視覺化展演
- **治療數值**：顯示 `DeltaHp` 治療量
- **正向回饋**：通常使用綠色或溫暖色調的動畫效果
- **統一模板**：與傷害動畫使用相同的模板結構

##### ShieldEventView 護盾動畫
**[ShieldEventView.cs](Assets/Scripts/GameView/EventView/ShieldEventView.cs)** 護盾效果的視覺化展演
- **護盾數值**：顯示 `DeltaShield` 護盾增加量
- **防護主題**：通常使用藍色或銀色的動畫效果
- **模板一致性**：保持與其他事件動畫的設計一致性

#### 資源相關事件動畫

##### GainEnergyEventView 獲得能量動畫
**[GainEnergyEventView.cs](Assets/Scripts/GameView/EventView/GainEnergyEventView.cs)** 能量獲得的視覺化展演
- **能量數值**：顯示 `GainEnergyResult.DeltaEp` 能量變化量
- **資源回饋**：為玩家提供能量獲得的即時視覺確認
- **積極色調**：通常使用明亮或金色的動畫效果

##### LoseEnergyEventView 失去能量動畫
**[LoseEnergyEventView.cs](Assets/Scripts/GameView/EventView/LoseEnergyEventView.cs)** 能量消耗的視覺化展演
- **消耗提示**：清晰展示能量的消耗數值
- **負向回饋**：使用較暗或紅色調的視覺提示

#### 性情相關事件動畫

##### IncreaseDispositionEventView 性情提升動畫
**[IncreaseDispositionEventView.cs](Assets/Scripts/GameView/EventView/IncreaseDispositionEventView.cs)** 性情提升的視覺化展演
- **性情數值**：顯示 `DeltaDisposition` 性情變化量
- **正面情緒**：通常使用溫暖或明亮的色調表現
- **獨特機制**：專門針對 MortalGame 的性情系統設計

##### DecreaseDispositionEventView 性情降低動畫
**[DecreaseDispositionEventView.cs](Assets/Scripts/GameView/EventView/DecreaseDispositionEventView.cs)** 性情降低的視覺化展演
- **負面回饋**：清晰提示性情的下降
- **視覺區分**：與提升動畫形成明顯的視覺對比

### 動畫生命週期
```
1. 事件觸發
   ↓
2. SetEventInfo (設定數值與位置)
   ↓  
3. PlayAnimation (播放 Timeline 動畫)
   ↓
4. 動畫完成後自動隱藏
   ↓
5. 物件池回收
```
