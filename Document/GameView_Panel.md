# GameView 面板分工

> 核對日期：2026-09-19

Panel 只放 View，流程協調位於 Presenter/Gameplay。面板負責顯示與回報事件，Presenter 決定下一步、等待結果並處理取消。

| 類別 | 責任 | 文件與程式 |
|---|---|---|
| Info | 血量、能量、好感度、回合與 Buff 顯示 | [Info](GameView_Info.md)、[程式](../Assets/Scripts/GameView/Panel/Info/) |
| Popup | 卡片瀏覽、選取、詳情及勝負結果 | [Popup](GameView_Popup.md)、[程式](../Assets/Scripts/GameView/Panel/Popup/) |
| UI | 牌組、墓地與回合送出按鈕 | [UI](GameView_UI.md)、[程式](../Assets/Scripts/GameView/Panel/UI/) |

[UIPresenter](../Assets/Scripts/Presenter/Gameplay/UIPresenter.cs) 接收牌組、墓地及敵人選牌的瀏覽事件，再啟動對應流程。面板取消與訂閱清理遵循 [Presenter](Presenter.md) 的戰鬥生命週期。
