# MortalCard／MortalGame

Unity 回合制卡牌遊戲。專案資料夾名為 MortalCard，程式命名空間與文件使用 MortalGame。

目前進度見 [待辦事項](Document/TODO.md)，系統細節由 [架構總覽](Document/SystemArchitecture.md) 與 [文件索引](Document/AI_Notes_Index.md) 進入。

想用網頁看進度，雙擊根目錄的 **啟動專案閱讀器.cmd**，會啟動本機唯讀服務並開啟瀏覽器。閱讀器直接讀取 Document 與 `.agents/working/` 的目前工作，不產生文件快照；需要已安裝 Node.js，不需安裝其他套件，詳見 [使用說明](Tools/ProjectBoard/README.md)。

## 開啟專案

1. 使用 Unity **6000.0.3f1** 開啟本目錄，等待套件還原與資產匯入；版本以 [ProjectVersion](ProjectSettings/ProjectVersion.txt) 為準。
2. 從 `Assets/Scenes/Main.unity` 啟動完整入口：選單 → 地圖 → 測試戰鬥。場景清單見 [EditorBuildSettings](ProjectSettings/EditorBuildSettings.asset)。
3. 修改 Card／Buff 資產後，執行 `MortalGame/遊戲內容/重新產生內容目錄`，再執行 `MortalGame → 驗證遊戲資料`。詳見 [資產製作規範](Document/GameData_Asset_Guidelines.md)。

目前戰鬥仍使用測試關卡、第一個敵人與即時產生的種子；勝利離場、同關重試及戰後寫回尚待補完，見 [場景限制](Document/Scene.md)。

## 開發導引

- 技術：C#、UniTask、UniRx、Option、Odin Inspector、URP；套件來源見 [manifest](Packages/manifest.json)。
- 分工：Model 管規則、View 管畫面、Presenter 協調，資料分為 Data → Instance → Entity；詳見 [系統架構](Document/SystemArchitecture.md)。
- 驗證：Unity Test Runner 的 EditMode／PlayMode；歷史通過紀錄與本次驗證須分開閱讀。
- 協作：開始修改前閱讀 [AGENTS.md](AGENTS.md) 與 [AI 工作指南](Document/AI_WorkOutline.md)。文件與註解使用台灣繁體中文。
