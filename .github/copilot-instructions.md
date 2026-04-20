# GitHub Copilot 指令 - MortalGame AI 文件系統

## 🌍 語言要求
**重要：本專案的所有文件必須使用台灣繁體中文撰寫**
- 所有新建立的文件必須使用繁體中文
- 所有注釋、說明、文檔都需要繁體中文
- 保持專業的技術文件寫作風格

---

## 🚨 關鍵：工作前必讀

### 必讀文件 - 必須先閱讀
開始任何分析或文件工作之前，您必須先閱讀核心文件：

**[AI_WorkOutline.md](../Document/AI_WorkOutline.md)** - AI 工作指南
   - 📋 專案架構概覽
   - 🎯 工作原則與流程  
   - 📝 筆記命名規範
   - ✅ 待辦追蹤系統

**[Documentation_Guidelines.md](../Document/Documentation_Guidelines.md)** - 文檔撰寫指導原則
   - 🎯 文檔定位與核心原則
   - ✅ 應該包含的內容指引
   - ❌ 應該避免的內容類型
   - 📋 品質檢查清單與準確性標準

**[Coding_Standards.md](../Document/Coding_Standards.md)** - 編程規範指南
   - 🛠️ 核心技術堆疊規範（UniTask、UniRx、Odin Inspector）
   - 🏗️ 架構設計原則與三層資料架構
   - 📝 命名規範與程式碼組織標準
   - 🎮 卡牌遊戲特化規範

**[SystemArchitecture.md](../Document/SystemArchitecture.md)** - 專案架構總覽
   - 🏗️ 五大核心系統架構關係
   - 🔗 系統間協作與資料流向
   - 📊 技術特色與設計原則
   - 🎯 專案整體複雜度評估

## 專案背景

### Unity 卡牌遊戲 - MortalGame
- **架構**：MVC 模式
- **技術堆疊**：Unity C#、Option 模式、UniRx、Odin Inspector、Record 類型
- **設計模式**：三層架構 (Data→Instance→Entity)

---

### 核心目錄結構
```
MortalGame/
├── Assets/Scripts/
│   ├── GameModel/          # 核心遊戲邏輯層
│   ├── GameView/           # 視覺呈現層
│   ├── UI/                 # 使用者介面
│   ├── GameData/           # 遊戲資料定義
│   ├── Scene/              # 場景管理
│   └── LevelMap/           # 關卡地圖系統
└── Document/               # AI 文件系統
    ├── AI_WorkOutline.md   # 🔴 必讀
    ├── AI_Notes_Index.md   # 🔴 必讀
    └── SystemArchitecture.md
```

### 主要技術特色
- **現代 C# 功能**：Record 類型、Option 模式、UniRx 響應式程式設計
- **三層設計**：ScriptableObject→執行時實例→戰鬥實體
- **編輯器整合**：Odin Inspector 提供設計師友善工作流程

---

**重要提醒**：這個 AI 文件系統是關鍵專案資產。每次貢獻都要維護其品質與完整性。兩個必讀檔案包含避免錯誤並確保高品質輸出的重要背景。

**系統版本**：v1.3
**最後更新**：2026-01-20
**維護狀態**：🟢 活躍