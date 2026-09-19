# 文件索引

> 核對日期：2026-09-19
> 索引描述文件範圍，不以日期勾選標記暗示功能已完成；工作狀態以 TODO 為準。

## 工作與維護

| 文件 | 範圍 |
|---|---|
| [AI_WorkOutline](AI_WorkOutline.md) | 工作流程與逐步續接 |
| [Coding_Standards](Coding_Standards.md) | 開發準則與責任邊界 |
| [Documentation_Guidelines](Documentation_Guidelines.md) | 文件蒸餾與核對原則 |
| [TODO](TODO.md) | 工作順序、既有基礎及剩餘缺口 |
| [TODO_Archive](TODO_Archive.md) | 各次任務的歷史完成與驗證紀錄 |
| [AI_Notes_Index](AI_Notes_Index.md) | 本索引 |

## 架構與資料

| 文件 | 範圍 |
|---|---|
| [SystemArchitecture](SystemArchitecture.md) | 架構、依賴及資料流 |
| [GameData](GameData.md) | Catalog、配置與內容載入 |
| [GameData_Asset_Guidelines](GameData_Asset_Guidelines.md) | 資料資產製作與驗證 |

## 戰鬥邏輯

| 文件 | 範圍 |
|---|---|
| [GameModel](GameModel.md) | 回合、事件與 AI 現況 |
| [Action](Action.md) | 意圖、結果與反應視角 |
| [Effect](Effect.md) | 效果解析、來源能力與 Queue 邊界 |
| [EffectQueue_VisualGuide](EffectQueue_VisualGuide.md) | 觸發循環圖解、工作展開與隊伍逐步變化 |
| [Target](Target.md) | 目標來源與集合查詢 |
| [Value](Value.md) | 整數、缺值及算術契約 |
| [Condition](Condition.md) | 條件組合與集合語意 |
| [Entity](Entity.md) | 實體責任與區域邊界 |
| [Card](Card.md) | 出牌、移牌與生命週期 |
| [CardTransformation](CardTransformation.md) | 形態、Clone、Override 與未接線邊界 |
| [CardBuff](CardBuff.md) | 卡片修正與有效 Layer |
| [Character](Character.md) | 生命、護盾及角色 Buff |
| [Player](Player.md) | 玩家資源與卡片管理 |
| [Session](Session.md) | 反應記憶的更新、重置與清除 |
| [Instance](Instance.md) | 跨戰鬥狀態與寫回限制 |

## 呈現與協調

| 文件 | 範圍 |
|---|---|
| [GameView](GameView.md) | 事件分發與畫面資料 |
| [CardView](CardView.md) | 手牌、拖曳、焦點與詳情 |
| [BuffView](BuffView.md) | 玩家 Buff 圖示 |
| [CharacterView](CharacterView.md) | 角色動畫排程與取消 |
| [EventView](EventView.md) | 數字動畫與回收 |
| [Factory](Factory.md) | View 物件池 |
| [GameView_Panel](GameView_Panel.md) | 面板責任分工 |
| [GameView_Info](GameView_Info.md) | 資源與狀態顯示 |
| [GameView_Popup](GameView_Popup.md) | 瀏覽、選取與結果面板 |
| [GameView_UI](GameView_UI.md) | 牌區與回合操作按鈕 |
| [Presenter](Presenter.md) | 命令轉譯、建構與非同步監督 |
| [Scene](Scene.md) | 場景生命週期及主迴圈限制 |

## 建議閱讀

先讀工作指南、編程規範、架構與 TODO，再依任務進入領域文件。跨層問題可按 GameData → Instance／Entity → Action／Effect → Presenter／GameView → Scene 追查。文件中的程式連結是深入入口，不另維護腳本或測試數量。
