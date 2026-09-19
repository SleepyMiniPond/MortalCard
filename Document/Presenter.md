# Presenter 協調層

> 核對日期：2026-09-19

Presenter 將 UI 命令轉成 Model 動作，並將 Model 事件交給 View 呈現。View／Presenter 的共享契約位於 [Presentation Abstractions](../Assets/Scripts/Presentation/Abstractions/)，協調實作位於 [Presenter](../Assets/Scripts/Presenter/)，不混放在 Panel。

## 戰鬥與取消

[GameplayPresenter](../Assets/Scripts/Presenter/Gameplay/GameplayPresenter.cs) 同時監督戰鬥主流程、玩家命令／事件流程與 UI 面板流程。只有戰鬥主流程可以正常先完成；其餘工作提前結束或拋出例外須向上傳遞。

戰鬥結束後取消輔助工作並等待收斂，再處理結果面板；最後等待角色動畫清理。Scene Token 沿子流程傳遞，面板與訂閱透過清理區塊釋放，避免場景離開後仍操作畫面。

## 輸入與選取

[GameCommand](../Assets/Scripts/Presentation/Abstractions/GameCommand.cs) 描述 View 操作，[GameAction](../Assets/Scripts/GameModel/Action/GameAction.cs) 描述交給 Model 的動作。命令先入列，由單一流程依序處理，不在接收時提前啟動非同步選取。

[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs) 已逐群組處理 ExistCard 選取並以群組 ID 回傳結果；其他子選取類型仍是預留，完整多步驟契約見 T-011。卡片互動依 Identity 查最新 CardInfo，避免變形後使用舊規則。

## 建構與顯示狀態

- [ScriptableDataLoader](../Assets/Scripts/Presenter/Gameplay/ScriptableDataLoader.cs) 從 Catalog、玩家配置與 Excel 表讀取內容。
- [Context](../Assets/Scripts/Presenter/Gameplay/Context.cs) 保存載入資料，由 Main 建立及持有，並非單例服務。
- [BattleBuilder.cs](../Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs) 內的類別目前拼為 BattleBuidler，組裝 Library、Factory 與種子亂數。正式建構仍使用測試關卡 ID、第一個敵人與即時種子，尚無完整選關參數傳遞。
- [GameStageSetting](../Assets/Scripts/GameModel/GameStageSetting.cs) 位於 GameModel。
- [GameInfoModel.cs](../Assets/Scripts/Presenter/Gameplay/GameInfoModel.cs) 內的 GameViewModel 管理卡片與 Buff 等可觀察顯示資料，並非所有 UI 數值的唯一更新途徑。

## 結果與地圖限制

Lose 面板提供 Retry／Restart／Quit。Win 面板目前只有顯示與隱藏，Win Presenter 沒有正常完成事件，會持續等待至取消。地圖目前只有點擊開戰入口；完整場景結果處理見 [Scene](Scene.md)，缺口列於 [TODO](TODO.md)。
