# Presenter 協調層

> 核對日期：2026-10-15（本次更新選取與警告面板職責）

Presenter 將 UI 命令轉成 Model 動作，並將 Model 事件交給 View 呈現。View／Presenter 的共享契約位於 [Presentation Abstractions](../Assets/Scripts/Presentation/Abstractions/)，協調實作位於 [Presenter](../Assets/Scripts/Presenter/)，不混放在 Panel。

## 戰鬥與取消

[GameplayPresenter](../Assets/Scripts/Presenter/Gameplay/GameplayPresenter.cs) 同時監督戰鬥主流程、玩家命令／事件流程與 UI 面板流程。只有戰鬥主流程可以正常先完成；其餘工作提前結束或拋出例外須向上傳遞。

戰鬥結束後取消輔助工作並等待收斂，再處理結果面板；最後等待角色動畫清理。Scene Token 沿子流程傳遞，面板與訂閱透過清理區塊釋放，避免場景離開後仍操作畫面。

## 輸入與選取

[GameCommand](../Assets/Scripts/Presentation/Abstractions/GameCommand.cs) 描述 View 操作，[GameAction](../Assets/Scripts/GameModel/Action/GameAction.cs) 描述交給 Model 的動作。命令先入列，由單一流程依序處理，不在接收時提前啟動非同步選取。

[CardSelectionFlow](../Assets/Scripts/Presenter/Gameplay/CardSelectionFlow.cs) 依序查詢選取步驟、累積 Draft，並在每步確認後重新驗證結果。[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs) 只控制單一步驟的選卡面板，透過 IUniTaskPresenter 的按鈕事件處理選取、確認、取消、收合及卡片詳情；不持有 Model 或管理多步驟草稿。目前支援 ExistCard，其他類型見 T-011 後續範圍。

GameplayPresenter 將失敗原因轉成已本地化的文字，交由 [WarningPopupPresenter](../Assets/Scripts/Presenter/Gameplay/WarningPopupPresenter.cs) 顯示。該 Presenter 搭配獨立 [WarningPopupPanel](../Assets/Scripts/GameView/Panel/Popup/WarningPopupPanel.cs)，只接收文字並等待 CloseEvent，不依賴選取規則，也不使用選卡面板顯示警告。Panel 提供關閉按鈕與顯示方法；Presenter.Run 訂閱按鈕，透過 IUniTaskPresenter 管理事件及訂閱清理，Panel 不在 Awake 綁定 callback。

面板 Presenter 自己執行 Open → await IUniTaskPresenter.Run → finally Close。共用執行器處理事件與工作清理，不提供開啟面板的 onStarted callback。

固定面板按鈕由 View 提供 Button 參考，Presenter._SetupSubscriptions 訂閱點擊並送出對應 Event，訂閱隨 Run 結束釋放。選卡面板的關閉、確認與收合均沿用此方式；動態卡片的點擊／長按則由 SelectionProperty 隨渲染配置 callback，與卡片詳情面板的做法一致。

## 建構與顯示狀態

- [ScriptableDataLoader](../Assets/Scripts/Presenter/Gameplay/ScriptableDataLoader.cs) 從 Catalog、玩家配置與 Excel 表讀取內容。
- [Context](../Assets/Scripts/Presenter/Gameplay/Context.cs) 保存載入資料，由 Main 建立及持有，並非單例服務。
- [BattleBuilder.cs](../Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs) 內的類別目前拼為 BattleBuidler，組裝 Library、Factory 與種子亂數。正式建構仍使用測試關卡 ID、第一個敵人與即時種子，尚無完整選關參數傳遞。
- [GameStageSetting](../Assets/Scripts/GameModel/GameStageSetting.cs) 位於 GameModel。
- [GameInfoModel.cs](../Assets/Scripts/Presenter/Gameplay/GameInfoModel.cs) 內的 GameViewModel 管理卡片與 Buff 等可觀察顯示資料，並非所有 UI 數值的唯一更新途徑。

## 結果與地圖限制

Lose 面板提供 Retry／Restart／Quit。Win 面板目前只有顯示與隱藏，Win Presenter 沒有正常完成事件，會持續等待至取消。地圖目前只有點擊開戰入口；完整場景結果處理見 [Scene](Scene.md)，缺口列於 [TODO](TODO.md)。
