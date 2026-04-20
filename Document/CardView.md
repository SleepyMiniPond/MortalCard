# CardView 卡牌視圖

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

CardView 系統負責卡牌在 UI 上的**完整視覺體驗**，包括手牌的弧形排列、卡牌的拖曳打出、聚焦放大、以及卡牌詳情顯示。這是玩家與遊戲互動最密切的 View 子系統。

## 核心資料模型

### CardInfo（Record 類型）

每張卡牌的完整顯示資訊，由 GameViewModel 管理：
- 身份資訊：Identity、CardDataID
- 分類資訊：Type、Rarity、Themes
- 數值資訊：OriginalCost/Power（原始）、CurrentCost/Power（計算 Buff 後）
- 選取規則：MainSelectionInfo
- Buff 與屬性：BuffInfos、Properties、Keywords
- 預覽數值：PreviewPower（顯示打出後的預期效果）

提供 `GetTemplateValues()` 供本地化系統進行模板替換（如 `{cost}` → 實際費用）。

### SelectableInfo

卡牌目標選取的 UI 規則轉換：
- `MainSelectionInfo`：主選取（角色/卡牌/無）
- `SubSelectionInfo`：子選取群組（現有卡牌/新建卡牌/效果變體）
- `IsSelectable()`：判斷某個 TargetType 是否為合法選取目標

## CardView — 手牌卡牌

實作 `ICardView`、`ISelectableView`、`IDragableCardView`、`IRecyclable` 四個介面。

### 三種渲染狀態

| 狀態 | 用途 | 互動能力 |
|------|------|----------|
| RuntimeHandCardProperty | 手牌中的卡牌 | 懸停放大、拖曳打出 |
| CardClickableProperty | 可點擊的卡牌 | 點擊選取/取消選取 |
| CardSimpleProperty | 唯讀展示 | 無互動 |

### 拖曳系統

手牌卡牌支援完整的拖曳流程：
1. **BeginDrag**：開始拖曳，通知 HandCardView
2. **Drag**：跟隨滑鼠移動，顯示目標箭頭（若需要選取目標）
3. **EndDrag**：放開時判斷目標是否合法，回報給 Presenter

### 聚焦內容

懸停卡牌時，展開顯示卡牌詳情（Buff 資訊、關鍵字說明）於專用面板。

## AllyHandCardView — 手牌管理器

管理友軍所有手牌 CardView 的容器，是 CardView 系統中最複雜的元件。

### 弧形排列演算法

手牌不是簡單的水平排列，而是呈現**弧形**分布：
- 根據卡牌數量計算每張卡的角度偏移與位置
- 中間的卡牌較高，兩側逐漸降低
- 每張卡牌有獨立的旋轉角度

### 聚焦系統

玩家懸停某張卡牌時：
- 該卡牌放大並提升至最上層
- 相鄰卡牌水平偏移讓出空間
- 顯示卡牌詳細資訊
- 使用 DOTween 動畫平滑過渡

### 拖曳管理

與卡牌拖曳協同：
- 計算拖曳偏移量
- 顯示/隱藏目標指示箭頭
- 管理拖曳狀態（開始/進行/結束）

### 事件處理

響應遊戲事件：
- `DrawCardEvent`：建立新 CardView
- `MoveCardEvent`：移除卡牌
- `DiscardHandCardEvent`：批次移除
- 每次變化後重新計算弧形排列

## AiCardView — 敵人卡牌

簡化版的卡牌顯示，用於展示 AI 選定的卡牌：
- 無拖曳或互動
- 水平排列（非弧形）
- 可配置的寬度和間距

## EnemySelectedCardView — 敵人手牌管理

管理敵人已選卡牌的顯示容器：
- 水平排列，動態間距
- 顯示剩餘牌組數量
- 響應 `EnemySelectCardEvent` / `EnemyUnselectedCardEvent`
- 透過 ViewModel 訂閱響應式更新

## 卡牌詳情視圖

### FocusCardDetailView
- 放大版卡牌詳情，包含 Buff 和關鍵字提示
- 與聚焦系統連動：懸停時顯示，移開時隱藏

### CardDetailInfoView
- 結構預留的擴展點（目前為空實作）

### CardStatusInfo / CardStatusInfoView
- 卡牌的狀態快照資訊
- 用於卡牌詳情面板中的單卡展示

### CardPropertyHint
- 動態網格顯示卡牌的 Buff 和關鍵字資訊
- 支援本地化文字渲染

## 相關文件

- [GameView 視覺呈現層](GameView.md) — CardView 的父系統
- [Card 卡牌系統](Card.md) — 卡牌的資料與實體
- [Factory 工廠系統](Factory.md) — CardViewFactory 物件池
- [GameView_Popup 彈窗面板](GameView_Popup.md) — 卡牌選取面板
