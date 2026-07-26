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
- **資料資產規範**：建立或修改 ScriptableObject 資產時，遵循 [GameData 資料資產製作規範](GameData_Asset_Guidelines.md)
- **編輯器友善**：重視設計師工作流程體驗
- **資源管理**：統一的資料載入與快取機制

## 📋 程式碼品質標準

### Validator 與 Runtime 責任邊界

- **遊戲機制允許的失效必須由 Runtime 表達**：若目標可能因同一批次中較早執行的 Effect 而消失，例如第一個 Effect 已移除 Buff，導致第二個 Effect 找不到原目標，應視為正常遊戲流程並安全地 No-op／Rejected。
- **正常失效不產生 Gameplay Event 或動畫**：需要時保留 Debug Trace，但不可將正常的時序失效誤報為資料錯誤。
- **資產製作錯誤必須由 Validator 事先排除**：缺少 Library 資料、CardData、必要 ID、Operation、Condition 或其他理應存在的企劃資料，應在進入遊戲前由 GameData Validator 明確報錯。
- **Runtime 信任已通過驗證的企劃資料**：核心流程不要重複加入逐層 null、缺少資料或未知型別的防禦分支，以免掩蓋資產錯誤並干擾主要業務邏輯。
- **優先消除重複資料來源**：若防禦檢查只是為了確認兩份相同語意的輸入一致，應重構成單一資料來源，而不是保留額外一致性判斷。
- **邊界資料仍需驗證**：玩家輸入、存檔、網路回應或其他未經專案 Validator 保證的外部資料，不適用「Runtime 直接信任」原則。

判斷準則：

| 情境 | 處理位置 | Runtime 行為 |
|------|----------|--------------|
| Effect 排隊後目標因較早 Effect 消失 | Runtime 遊戲機制 | 安靜 No-op／Rejected |
| Buff Layer 已被移除，舊 Command 才執行 | Runtime 遊戲機制 | 安靜 No-op／Rejected，保留 Debug Trace |
| CardData ID 不存在於 CardLibrary | GameData Validator | 阻止錯誤資料進入正式 Runtime |
| 必填 Operation／Condition／Target 遺漏 | GameData Validator | 明確列出資產錯誤 |
| 外部存檔或網路資料損壞 | 系統輸入邊界 | 驗證並回報錯誤 |

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
**版本**：v1.1
