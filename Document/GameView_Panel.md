# GameView Panel 面板系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Panel 系統是 GameView 中的**面板與彈窗集合**，分為三個子目錄：Info（狀態資訊）、Popup（彈窗互動）、UI（工具按鈕）。這三者各有明確的職責邊界。

## 總覽

```
Panel/
├── UIPresenter.cs           # 面板事件協調器
├── Info/                    # 狀態顯示（被動更新）
│   ├── TopBarInfoView       # 回合數
│   ├── HealthBarView        # 血條
│   ├── EnergyBarView        # 能量條
│   ├── DispositionView      # 好感度
│   ├── AllyInfoView         # 友軍資訊面板
│   ├── EnemyInfoView        # 敵軍資訊面板
│   └── GameKeyWordInfoView  # 關鍵字說明
├── Popup/                   # 互動彈窗（主動觸發）
│   ├── CardSelectionPanel   # 卡牌多選面板
│   ├── AllCardDetailPanel   # 卡牌瀏覽面板
│   ├── AllCardDetailPresenter
│   ├── SingleCardDetailPopupPanel
│   ├── GameResultWinPanel   # 勝利面板
│   ├── GameResultLosePanel  # 失敗面板
│   ├── GameResultWinPresenter
│   ├── GameResultLosePresenter
│   ├── SubSelectionPresenter
│   ├── UniTaskPresenter     # 非同步事件迴圈
│   └── SimpleTitleInfoHintView # 提示框
└── UI/                      # 工具按鈕（簡單互動）
    ├── DeckCardView         # 牌組按鈕
    ├── GraveyardCardView    # 墓地按鈕
    └── SubmitView           # 送出按鈕
```

## UIPresenter — 面板事件協調

UIPresenter 監聽工具按鈕的點擊事件，啟動對應的面板流程：
- 點擊牌組按鈕 → 啟動 AllCardDetailPresenter 顯示牌組
- 點擊墓地按鈕 → 啟動 AllCardDetailPresenter 顯示墓地
- 點擊敵人選牌 → 啟動 AllCardDetailPresenter 顯示敵人已選卡牌

使用 `IUniTaskPresenter` 進行非同步事件處理。

## 詳細文件引用

- [Info 資訊面板](GameView_Info.md)
- [Popup 彈窗面板](GameView_Popup.md)
- [UI 工具元件](GameView_UI.md)

## 相關文件

- [GameView 視覺呈現層](GameView.md) — Panel 的父系統
- [Presenter 協調層](Presenter.md) — 面板事件的觸發源
