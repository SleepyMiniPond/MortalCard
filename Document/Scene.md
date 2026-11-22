# Scene 場景與主流程

> 核對日期：2026-11-23

Scene 負責取得場景內 View、建立 Presenter、等待結果。[Main](../Assets/Scripts/Scene/Main.cs) 啟動 [GameFlow](../Assets/Scripts/Presenter/Gameplay/GameFlow.cs)，由 GameFlow 依序載入場景、呼叫 Scene.Run 並決定結果去向。[SceneLoadManager](../Assets/Scripts/Scene/SceneLoadManager.cs) 只實作 ISceneLoader，載入後回傳場景入口，不執行場景互動。

[場景契約](../Assets/Scripts/Presenter/Gameplay/GameSceneContracts.cs) 分開載入介面與各場景的 Run 入口；流程測試可替換 loader 及場景入口，分別驗證載入、執行與結果處理的順序。

## 生命週期

Main 的銷毀 Token 傳至載入與 Scene.Run；各 Scene 再連結自身銷毀 Token。Menu 等待開始、LevelMap 等待選擇、Gameplay 等待戰鬥與結果，均受取消控制。下層取消不反向取消 Main。

入口：[MenuScene](../Assets/Scripts/Scene/MenuScene.cs)、[LevelMapScene](../Assets/Scripts/Scene/LevelMapScene.cs)、[GameplayScene](../Assets/Scripts/Scene/GameplayScene.cs)。LoadingScene 仍是最小預留元件。

## 結果流程

Menu 開始後建立新一輪玩家資料，再進入 LevelMap。流程透過 await 與結果分支前進，不以跨次保留的 retry／restart 旗標控制。

| 結果 | 流程 |
|---|---|
| GameplayWinResult | 臨時第一關勝利回 LevelMap，第二關勝利回 Menu；目前勝利面板仍缺正常完成入口 |
| Retry | 使用同關開戰設定與原種子，再建立一場戰鬥 |
| Restart | 重建初始玩家資料並回 LevelMap |
| Quit | 回 Menu；下次開始重新建立一輪 |
| 地圖 Finish／Fail | 回 Menu，不停止主流程 |
| 地圖 Restart | 重建初始玩家資料並回 LevelMap |

取消與例外向上傳遞；載入返回後先檢查取消，再執行 Scene.Run，結果返回後也先檢查取消再處理去向。已觀察到取消時不開始下一段互動或載入下一個流程場景；已開始的 Unity 非同步載入不保證能被中止。場景內清理由各 Scene／Presenter 負責。

## 同關設定與資料邊界

[TemporaryBattleRoute](../Assets/Scripts/Presenter/Gameplay/TemporaryBattleRoute.cs) 集中保存正式地圖完成前的兩關進度、關卡 ID、測試敵人及設定建立；兩關使用同一個敵人。第一關勝利後，下次地圖點擊開戰進第二關；第二關勝利結束本輪回選單。Retry 留在當關，Restart 重設玩家並回第一關；關卡編號暫時輸出至 Console，尚未顯示於地圖 UI。

GameFlow 只在一輪開始建立此臨時路線，並於勝利／Restart 通知推進或重設。正式地圖導入時，替換這個類別及 GameFlow 的路線接點即可，不需從 Model／View 清除兩關規則。

路線在每次從地圖開戰時建構一次設定，Retry 不重新產生設定。[BattleBuilder](../Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs) 只負責建立戰鬥 Context。GameplayScene 接收設定，每次建立新的 GameContextManager 與 GameRandom(原種子)，不沿用已耗用的亂數物件。

開戰設定保留玩家 Instance 狀態及獨立牌組清單；每次嘗試另外複製牌組與附加屬性集合，保留來源身份與持久形態。內容配置物件仍共享，不提供 Play 中外部修改資產後的完整重播保證，Runtime Guid 也會重新建立。

## 尚未完成

- [GameResultWinPresenter](../Assets/Scripts/Presenter/Gameplay/GameResultWinPresenter.cs) 沒有正常完成入口，勝利畫面持續等待至取消。
- LevelMap View 目前只有開戰按鈕，其他地圖結果尚無完整互動入口。
- GameFlow 尚未套用勝利 CardInstanceChangeSet，也未接入戰後血量／好感度延續與能量回滿。結果去向已接入不代表戰後資料提交已完成。

上述是程式現況，後續修正與行為決策列於 [TODO](TODO.md)；戰鬥內非同步協調見 [Presenter](Presenter.md)。
