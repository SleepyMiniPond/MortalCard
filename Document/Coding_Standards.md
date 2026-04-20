# 編程規範指南 - MortalGame

## 🎯 核心技術堆疊

### 異步處理
- **UniTask**：所有異步操作必須使用 UniTask，避免使用原生 Task
- 統一異步程式碼風格，提升 Unity 環境下的效能

### 響應式程式設計  
- **UniRx**：玩家事件、系統間通訊、狀態變化監聽統一使用 UniRx
- 建立響應式資料流，減少直接耦合

### 不可變資料結構
- **Record 類型**：資料傳遞物件優先使用 Record
- **IReadOnlyList**：不會變更的集合欄位使用唯讀介面
- **Option 模式**：處理可選值與空值安全

### 編輯器工具整合
- **Odin Inspector**：所有 ScriptableObject 與 Inspector 顯示
- **Odin 特性規範**：
  - `[BoxGroup]` / `[TitleGroup]`：邏輯分組
  - `[ShowInInspector]`：顯示私有/唯讀欄位
  - `[TableColumnWidth]`：表格欄位寬度控制

## 🏗️ 架構設計原則

### 職責分離原則
- **資料與邏輯分離**：GameData 只負責資料定義
- **視圖與邏輯分離**：GameView 只負責視覺呈現
- **業務邏輯集中**：核心邏輯統一在 GameModel

## 📝 命名與組織規範

### 程式碼組織
- **枚舉集中**：所有枚舉定義在 `GameEnum.cs`
- **屬性分組**：使用 Odin Inspector 特性進行邏輯分組
- **介面先行**：行為定義使用介面（如 `ICardEffect`）

## 🎮 遊戲特化規範

### Unity 整合最佳實踐
- **ScriptableObject 封裝**：所有遊戲資料透過 SO 管理
- **編輯器友善**：重視設計師工作流程體驗
- **資源管理**：統一的資料載入與快取機制

## 📋 程式碼品質標準

### 必須使用
- ✅ UniTask 處理異步
- ✅ UniRx 處理事件流
- ✅ Record/IReadOnlyList 確保不可變性
- ✅ Odin Inspector 增強編輯器體驗
- ✅ Option 模式處理空值

### 禁止使用  
- ❌ 原生 C# Task（Unity 環境下）
- ❌ 直接的 null 檢查（使用 Option 代替）
- ❌ 硬編碼的遊戲數值
- ❌ 直接的系統間依賴（使用 UniRx 解耦）

---

**維護責任**：所有開發者  
**更新頻率**：發現新模式時即時更新  
**版本**：v1.0