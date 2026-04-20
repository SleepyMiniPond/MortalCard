# UI 系統 - 介面工具層架構總覽

## 🎯 系統定位與職責

**UI 是 MortalGame 的介面工具層**，提供各種輕量級的 UI 輔助元件與工具函數，支援視覺呈現的細節處理與用戶體驗優化。專注於可重用的介面解決方案。

## 🛠️ 工具元件分類

### 畫面適配工具
- **[AutoCanvasScaler](Assets/Scripts/UI/AutoCanvasScaler.cs)**：自動根據螢幕比例調整 Canvas 縮放策略，確保不同解析度下的視覺一致性

### 視覺呈現工具  
- **[CustomLineRenderer](Assets/Scripts/UI/CustomLineRenderer.cs)**：自定義線條繪製元件，支援實線、虛線、點線等多種樣式，用於連接線或路徑顯示
- **[IntColorMapping](Assets/Scripts/UI/IntColorMapping.cs)**：整數到顏色的映射工具，提供數值驅動的顏色配置系統

### 動態切換工具
- **[IntValueSwitch](Assets/Scripts/UI/IntValueSwitch.cs)**：基於整數值的動態切換基底類別，提供數值驅動的介面變化機制
- **[IntTextColorSwitch](Assets/Scripts/UI/IntTextColorSwitch.cs)**：繼承 IntValueSwitch，實現文字顏色的數值驅動切換

### 通用工具函數
- **[UiUtility](Assets/Scripts/UI/UiUtility.cs)**：包含 CanvasUtility 等靜態工具方法，提供座標轉換、位置計算等常用操作
