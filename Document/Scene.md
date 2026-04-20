# Scene 場景管理

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

Scene 系統管理 Unity 場景的生命週期和切換。透過 Main.cs 的無限迴圈驅動遊戲流程，每個場景對應一個明確的遊戲階段。

## 遊戲主迴圈 — Main.cs

```
Main.StartGame()
  Loop(永遠):
    1. MenuScene       → 主選單（等待玩家開始）
    2. LevelMapScene   → 關卡選擇（等待玩家選關 / 離開）
       ├── 選擇關卡 → 進入 3
       └── 離開     → 回到 1
    3. GameplayScene   → 戰鬥（等待戰鬥結束）
       ├── 勝利 → 回到 2
       ├── 重試 → 回到 3（同關卡）
       ├── 重新開始 → 回到 2
       └── 退出 → 回到 1
```

Main 使用 `UniTask` 非同步執行整個流程，每個場景回傳一個結果物件決定下一步。

## 場景列表

### GameplayScene — 戰鬥場景

最複雜的場景，負責：
1. 取得 Scene 中的 GameplayView
2. 透過 BattleBuilder 建構所有依賴
3. 啟動 GameplayPresenter.Run()
4. 等待戰鬥結束，回傳 `GameplayResultCommand`

### LevelMapScene — 關卡選擇場景

1. 取得 Scene 中的 LevelMapView
2. 啟動 LevelMapPresenter.Run()
3. 等待玩家選擇，回傳 `LevelMapCommand`
   - Battle（含 StageID）→ 進入戰鬥
   - Leave → 回到主選單

### MenuScene — 主選單場景

最簡場景，等待玩家點擊開始遊戲。

### LoadingScene — 載入場景

預留的中間場景（最小實作）。

## SceneLoadManager — 場景載入管理

提供非同步的場景載入工具：
- 使用 `SceneManager.LoadSceneAsync()` 進行非同步載入
- 以 `UniTask` 包裝 Unity 的非同步操作
- 確保場景轉換的平順

## 設計特點

### 無限迴圈架構

Main.cs 的遊戲迴圈永不結束（`while(true)`），場景之間透過結果物件控制流向。這讓流程控制集中在一個位置，避免分散到各場景中。

### 場景獨立性

每個場景只負責：
1. 取得自身的 View 參照
2. 建構需要的 Presenter
3. 啟動並等待結果
4. 回傳結果物件

不涉及其他場景的邏輯。

## 相關文件

- [Presenter 協調層](Presenter.md) — 場景內的邏輯協調
- [GameView 視覺呈現層](GameView.md) — 場景中的視覺元件
- [SystemArchitecture 架構總覽](SystemArchitecture.md) — 全域架構
