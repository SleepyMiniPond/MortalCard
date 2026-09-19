# CardView 卡牌視圖

> 核對日期：2026-09-19

卡牌視圖負責手牌排列、拖曳、聚焦及詳情展示。顯示資料以 [CardInfo](../Assets/Scripts/GameModel/Info/CardInfo.cs) 為準，其中 Cost／Power 可缺值；CreatePreview 更新同一 Power 欄位，沒有獨立 PreviewPower 欄位，也不是完整模擬能力。

## 互動規則

[CardView](../Assets/Scripts/GameView/CardView/CardView.cs) 區分手牌互動、點擊選取與純展示。Callback 傳 Identity，需判斷規則時再查最新 CardInfo，避免形態變更後操作舊資料。

[AllyHandCardView](../Assets/Scripts/GameView/CardView/AllyHandCardView.cs) 管理弧形排列、聚焦及拖曳。拖曳中形態改變會重新驗證主選取與目前目標，失效時清除選取與指向線；放開時再次查詢。

[AiCardView](../Assets/Scripts/GameView/CardView/AiCardView.cs) 與 [EnemySelectedCardView](../Assets/Scripts/GameView/CardView/EnemySelectedCardView.cs) 顯示敵人已選卡片，沒有友軍手牌的拖曳流程。

## 詳情

[FocusCardDetailView](../Assets/Scripts/GameView/CardView/FocusCardDetailView.cs) 與 [SingleCardDetailPopupPanel](../Assets/Scripts/GameView/Panel/Popup/SingleCardDetailPopupPanel.cs) 顯示最新卡片、Buff 與關鍵字。提示內容由 [CardPropertyHint](../Assets/Scripts/GameView/CardView/CardPropertyHint.cs) 組合。

CardDetailInfoView 仍是空元件；CardStatusInfoView.cs 實際定義 CardBuffInfoView，負責提示文字，不能依檔名推論成另一套卡片狀態快照。

建立及回收見 [Factory](Factory.md)，形態互動契約見 [CardTransformation](CardTransformation.md)，選取能力限制見 [Popup](GameView_Popup.md)。
