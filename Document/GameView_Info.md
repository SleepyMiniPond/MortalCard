# GameView\Info 系統 - 遊戲資訊顯示組件

## 🎯 系統定位
GameView\Info 系統負責提供玩家在戰鬥過程中所需的各類遊戲資訊顯示，包含玩家狀態、敵人狀態、遊戲回合、以及各種數值條與提示資訊。

## 🏗️ 核心設計架構

### 雙陣營資訊顯示架構
系統採用**雙陣營對稱設計**，為我方與敵方提供統一的資訊顯示模式：
- **AllyInfoView**：我方完整資訊面板
- **EnemyInfoView**：敵方完整資訊面板
- 兩者共享相同的子組件，確保資訊顯示的一致性

### 模組化組件設計
每個資訊類型都封裝為獨立可重用的組件：
- **HealthBarView**：生命值與護盾顯示
- **EnergyBarView**：能量值顯示  
- **DispositionView**：角色性格狀態顯示
- **TopBarInfoView**：回合資訊顯示
- **GameKeyWordInfoView**：遊戲關鍵字資訊提示

## 📊 資訊顯示組件詳析

### 生命值系統 - [HealthBarView](Assets/Scripts/GameView/Panel/Info/HealthBarView.cs)
**設計要點**：
- 雙數值顯示設計：同時顯示生命值與護盾值
- 視覺化比例表示：透過 fillAmount 顯示生命值比例
- 護盾狀態控制：護盾存在時動態顯示護盾相關物件

### 能量系統 - [EnergyBarView](Assets/Scripts/GameView/Panel/Info/EnergyBarView.cs)
**設計要點**：
- 當前值/最大值雙顯示模式
- 比例條視覺化回饋
- 即時更新機制，回應能量變化事件

### 性格系統 - [DispositionView](Assets/Scripts/GameView/Panel/Info/DispositionView.cs)
**設計要點**：
- **響應式更新**：透過 UniRx 訂閱性格變化
- **懸停提示功能**：滑鼠懸停顯示詳細說明
- **動態本地化**：根據性格數值查詢對應文字描述
- **視覺回饋**：fillAmount 反映性格強度

## 🔄 事件驅動更新機制

### 資料驅動渲染
所有資訊組件都採用**資料驅動渲染模式**：
- 接收標準化的資料結構
- 透過統一的 SetInfo/UpdateInfo 方法更新顯示
- 支援本地化文字與視覺樣式的動態切換

## 🔗 系統間協作關係

### 與 GameModel 的資料流
- 訂閱 **IGameViewModel** 取得即時遊戲狀態
- 回應各類遊戲事件（健康值變化、能量變化、Buff 變化）
- 透過 **Entity** 資料結構接收角色完整資訊

### 與其他 GameView 組件協作
- **BuffView** 系統：顯示角色身上的增益/減益效果
- **Factory** 系統：透過物件池管理資訊組件的建立與回收
- **UI 工具**：使用基礎 UI 組件實現數值條與文字顯示
