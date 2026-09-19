# UI 工具元件

> 核對日期：2026-09-19

DeckCardView 與 GraveyardCardView 訂閱友軍對應牌區，顯示數量並提供瀏覽入口；UIPresenter 接收互動後開啟卡片集合面板。

SubmitView 發出 TurnSubmitCommand，由 Presenter 轉交 Model 結束玩家行動。按鈕不直接執行回合規則。

程式入口：[DeckCardView](../Assets/Scripts/GameView/Panel/UI/DeckCardView.cs)、[GraveyardCardView](../Assets/Scripts/GameView/Panel/UI/GraveyardCardView.cs)、[SubmitView](../Assets/Scripts/GameView/Panel/UI/SubmitView.cs)、[UIPresenter](../Assets/Scripts/Presenter/Gameplay/UIPresenter.cs)。

相關：[Panel](GameView_Panel.md)、[Popup](GameView_Popup.md)。
