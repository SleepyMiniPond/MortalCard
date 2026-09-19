# Scene 場景與主流程

> 核對日期：2026-09-19

Scene 負責取得場景內 View、建立 Presenter、等待結果。場景載入與遊戲主迴圈集中在 [Main](../Assets/Scripts/Scene/Main.cs) 及 [SceneLoadManager](../Assets/Scripts/Scene/SceneLoadManager.cs)。

## 生命週期

Main 的銷毀 Token 傳至載入與 Scene.Run；各 Scene 再連結自身銷毀 Token。Menu 等待開始、LevelMap 等待選擇、Gameplay 等待戰鬥與結果，均受取消控制。下層取消不反向取消 Main。

入口：[MenuScene](../Assets/Scripts/Scene/MenuScene.cs)、[LevelMapScene](../Assets/Scripts/Scene/LevelMapScene.cs)、[GameplayScene](../Assets/Scripts/Scene/GameplayScene.cs)。LoadingScene 仍是最小預留元件。

## 現有流程與限制

目前可從 Menu 進入 LevelMap，再由地圖點擊進入 Gameplay。不能將程式描述為已完成「勝利回地圖、重試同關、退出回選單」的完整狀態機：

- [GameResultWinPresenter](../Assets/Scripts/Presenter/Gameplay/GameResultWinPresenter.cs) 沒有正常完成入口，勝利畫面持續等待至取消。
- Main 在沒有 restart／retry 旗標時離開內層迴圈，再進入外層 Menu；不是一律回 LevelMap。
- retry 在單次戰鬥重試迴圈內未逐次重設，曾選 Retry 後的其他結果可能繼續重試；restart 也需明確定義每次結果的狀態轉移。
- LevelMap 的 Fail／Finish 會直接結束主流程；目前 View 只有開戰按鈕，其他結果尚無完整互動入口。
- 戰鬥設定仍由 [BattleBuilder](../Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs) 產生測試關卡、第一個敵人與新種子，Retry 並未保存原戰鬥設定。
- Main 尚未套用勝利 CardInstanceChangeSet。

上述是程式現況，後續修正與行為決策列於 [TODO](TODO.md)；戰鬥內非同步協調見 [Presenter](Presenter.md)。
