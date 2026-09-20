# 專案系統架構總覽

> 核對日期：2026-09-19

MortalGame 是 Unity 回合制卡牌遊戲，以 MVP 分工協調戰鬥規則、畫面與玩家輸入。Data → Instance → Entity 區分設計配置、跨戰鬥狀態與本場戰鬥狀態；目前 Card 與 Ally 有 Instance，其他實體不必經過完整三層。

## 職責與依賴

| 層級 | 責任 | 程式入口 |
|---|---|---|
| Scene | 場景載入、取消生命週期與場景結果 | [Main](../Assets/Scripts/Scene/Main.cs) |
| Presenter | 建構依賴、轉譯輸入、監督戰鬥與 UI 工作 | [GameplayPresenter](../Assets/Scripts/Presenter/Gameplay/GameplayPresenter.cs) |
| GameModel | 回合、效果、實體狀態與事件 | [GameplayManager](../Assets/Scripts/GameModel/GameplayManager.cs) |
| GameView | 消費事件、更新畫面、回報互動 | [GameplayView](../Assets/Scripts/GameView/GameplayView.cs) |
| Presentation Abstractions | View 與 Presenter 共用契約 | [PresentationContracts](../Assets/Scripts/Presentation/Abstractions/PresentationContracts.cs) |
| GameData | 卡牌、Buff 與玩家配置及內容查詢 | [GameContentCatalog](../Assets/Scripts/GameData/Scriptable/GameContentCatalog.cs) |

Scene 組裝 Presenter 與 View；Presenter 協調 Model 和 View，GameView 不引用 Presenter。GameData 與 GameModel 仍同屬 Runtime assembly，資料中的可執行 Target／Value／Condition 仍依賴 Runtime，不能描述成已完全分離的純資料程序集。Property、LifeTime 與 Session 的 Entity 建立責任已移至 [GameModel Factory](../Assets/Scripts/GameModel/Factory/)。

## 戰鬥資料流

內容 Catalog／玩家配置 → 戰鬥建構與亂數注入 → GameplayManager → Effect Queue 修改 Entity 並產生事件 → Presenter → GameplayView。

玩家輸入走反方向：View 發出 GameCommand，Presenter 補足選取並轉成 GameAction，Model 接收後執行。Selected 目標、正在反應的 Triggered 來源與 Action 作用目標各有不同語意，見 [Action](Action.md)。

事件用於畫面同步與結果描述；目前沒有完整的事件溯源、重播或模擬功能。Record 也不保證其參考的集合與 Entity 深度不可變。

## 狀態與生命週期

- [Instance](Instance.md) 保存跨戰鬥 Domain 狀態，目前並非磁碟存檔契約。
- [Card](Card.md) 定義卡片流轉與生命週期；[CardTransformation](CardTransformation.md) 定義形態優先順序與持久化邊界。
- [Effect](Effect.md) 統一效果解析及排程；正式出牌與效果根批次透過 CardPlayChain 協調 FIFO，跨 Runner 共用整條鏈的執行預算。
- [Presenter](Presenter.md) 監督非同步工作，取消後等待清理；[Scene](Scene.md) 記錄場景結果流程限制。
- UniTask、UniRx、Option 與 Odin 的使用原則見 [Coding_Standards](Coding_Standards.md)。

未完成能力與排期以 [TODO](TODO.md) 為準。
