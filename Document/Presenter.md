# Presenter 協調層

> 核對日期：2026-11-27

Presenter 將 UI 命令轉成 Model 動作，並將 Model 事件交給 View 呈現。View／Presenter 的共享契約位於 [Presentation Abstractions](../Assets/Scripts/Presentation/Abstractions/)，協調實作位於 [Presenter](../Assets/Scripts/Presenter/)，不混放在 Panel。

## 戰鬥與取消

[GameplayPresenter](../Assets/Scripts/Presenter/Gameplay/GameplayPresenter.cs) 同時監督戰鬥主流程、玩家命令／事件流程與 UI 面板流程。只有戰鬥主流程可以正常先完成；其餘工作提前結束或拋出例外須向上傳遞。

戰鬥結束後取消輔助工作並等待收斂，再處理結果面板；最後等待角色動畫清理。Scene Token 沿子流程傳遞，面板與訂閱透過清理區塊釋放，避免場景離開後仍操作畫面。

## 輸入與選取

[GameCommand](../Assets/Scripts/Presentation/Abstractions/GameCommand.cs) 描述 View 操作，[GameAction](../Assets/Scripts/GameModel/Action/GameAction.cs) 描述交給 Model 的動作。命令先入列，由單一流程依序處理，不在接收時提前啟動非同步選取。

指令在選取與正式提交後持續留在待完成佇列，阻擋重複輸入。提交前取消或拒絕由 Presenter 移除指令並恢復最新畫面；正式提交後，由對應卡片的 PlayerExecuteStartEvent.CompletedCardIdentity 移除指令並恢復互動。正式拒絕依序呈現 CardPlayRejectedEvent、等待警告關閉，再處理恢復事件；結束回合由 PlayerExecuteEndEvent 完成指令。場景取消或戰鬥結束不恢復操作。

[CardSelectionFlow](../Assets/Scripts/Presenter/Gameplay/CardSelectionFlow.cs) 依序準備選取步驟、累積 Draft，並在每步確認後重新驗證結果。[SubSelectionPresenter](../Assets/Scripts/Presenter/Gameplay/SubSelectionPresenter.cs) 控制單一步驟的選卡面板，透過 IUniTaskPresenter 處理選取、確認、取消、收合及卡片詳情；不保存多步驟草稿。支援 ExistCard、模板與效果選項，合成選項使用獨立預覽 Identity，模板基本費用／威力直接由配置呈現，避免尚無擁有者的預覽公式回傳缺值。

玩家指令作用域持有 CardSelectionSession，涵蓋面板選取及 Action 等待 Model 接收；完整提交會由共用 Context 建立入口結束 Session，取消或例外也會釋放。自動選取結果包含可釋放的 Session，由敵人、間接出牌或 Invoke 消費端持有；Queue 使用驗證完成的獨立 Context，不持有已結束的候選暫存。

GameplayPresenter 將失敗原因轉成已本地化的文字，交由 [WarningPopupPresenter](../Assets/Scripts/Presenter/Gameplay/WarningPopupPresenter.cs) 顯示。該 Presenter 搭配獨立 [WarningPopupPanel](../Assets/Scripts/GameView/Panel/Popup/WarningPopupPanel.cs)，不依賴選取規則，也不使用選卡面板顯示警告。Presenter 建立含訊息的唯讀顯示狀態，Open 回傳提供 CloseRequested 的 Session；各關閉按鈕在 View 合併為同一操作，Presenter 透過 IUniTaskPresenter 等待結束，再由 Session.Dispose 關窗、清空文字及解除綁定。

## Panel 的 State 與 Session 慣例

State＋Session 是專案大部分 Panel 的預設架構。新增 Panel 或調整既有 Panel 邊界時採用此模式；既有面板依功能需要逐步遷移，不要求一次重寫。以 [CardSelectionPanel](../Assets/Scripts/GameView/Panel/Popup/CardSelectionPanel.cs) 與 [AllCardDetailPanel](../Assets/Scripts/GameView/Panel/Popup/AllCardDetailPanel.cs) 為參考。

```csharp
IPanelSession Open(IReadOnlyReactiveProperty<PanelDisplayState> state);

public interface IPanelSession : IDisposable
{
    IObservable<Unit> CloseRequested { get; }
    IObservable<Guid> ItemClicked { get; }
}
```

此處名稱僅示意契約形狀，各面板定義自己的 State 與 Session，不要求建立共用泛型基底。

- **Presenter 擁有狀態與規則**：建立 ReactiveProperty 或從 Model／ViewModel 衍生唯讀狀態，向 View 傳入 IReadOnlyReactiveProperty。選取合法性、操作結果與子流程協調留在 Presenter／Model；View 不回寫狀態。能由既有來源推導的值優先衍生，避免多份可寫狀態手動同步。
- **State 描述完整顯示資訊**：包含候選項目、選取身份、可見性、可操作性及結構化提示等面板所需資料，不攜帶 Button、Transform 或操作 callback。發布後不修改其中集合；文字樣式、固定文案或本地化 key 由 prefab／場景序列化配置，View 負責格式化與呈現。
- **View 綁定並更新畫面**：每次 Open 訂閱 State，依狀態更新元件；列表依身份保留、更新與回收項目，避免每次通知重建整份列表。View 不負責決定下一個流程或提交 Domain。
- **Session 封裝單次開窗操作**：Open 回傳面板專屬的 IDisposable 介面，提供 Presenter 所需的 IObservable 操作事件。按鈕以 CloseRequested／ConfirmRequested 等語意表達；同類按鈕可合併為帶列舉參數的事件，動態項目傳遞身份。Presenter 不取得 Unity Button，也不逐項注入 callback。
- **Presenter 持有 Session**：以 using 持有 Open 的回傳值，訂閱事件後交給既有 IUniTaskPresenter 協調操作、等待子面板與防重入；輸入訂閱隨 Run 結束釋放。結束時先釋放 Session，再釋放 Presenter 建立的狀態來源。資料同步隨開窗生命週期持續運作，不放進玩家操作的忙碌事件槽。
- **Session 集中 View 清理**：Dispose 結束本次事件、解除狀態綁定與內部訂閱、回收動態項目並關窗，且可重複呼叫。OnDestroy 轉交同一清理入口，Open 部分失敗先清理再拋出；同一 View 尚有未釋放 Session 時拒絕重複 Open，舊 Session 不影響新視窗。釋放不偽造 CloseRequested 或其他玩家操作，也不釋放外部傳入的 State。
- **取消沿非同步流程傳遞**：同步 Dispose 負責 View 資源，不取代 CancellationToken、例外傳遞或等待子流程清理。取消與例外不得被轉為預設玩家結果。共用執行器不提供開啟面板的 onStarted callback。

固定內容且沒有動態顯示資料的面板可使用 Open() 回傳 Session，不為套用慣例建立空 State；[勝利面板](../Assets/Scripts/GameView/Panel/Popup/GameResultWinPanel.cs) 與 [失敗面板](../Assets/Scripts/GameView/Panel/Popup/GameResultLosePanel.cs) 採此方式。已有其他生命週期需求的 View 應依責任判斷是否適用，不能只為統一形式引入多餘抽象。

### 單卡詳情

[SingleCardDetailPresenter](../Assets/Scripts/Presenter/Gameplay/SingleCardDetailPresenter.cs) 負責單卡詳情的資料來源與關閉流程。戰鬥卡以 Identity 取得目前可觀察資訊並衍生 SingleCardDetailDisplayState；沒有來源的獨立預覽卡使用傳入快照。State 只攜帶 CardInfo，不包含 View 元件或提示配置。

[SingleCardDetailPopupPanel](../Assets/Scripts/GameView/Panel/Popup/SingleCardDetailPopupPanel.cs) 訂閱 State，同時更新卡面、Buff 與關鍵字提示，將多個關閉按鈕合併為 Session.CloseRequested。詳情卡面使用 CardView.RenderSnapshot，避免在 State 之外再訂閱卡片資訊；其他 CardView 既有的自動訂閱方式不變。Session.Dispose 解除綁定、重設卡片並回收提示，取消與例外由 Presenter 傳遞。

SubSelectionPresenter 與 AllCardDetailPresenter 等待單卡 Presenter，保留子面板開啟期間的父操作防重入；資料更新與玩家操作等待分開管理。

### 手牌懸停詳情

[FocusCardDetailView](../Assets/Scripts/GameView/CardView/FocusCardDetailView.cs) 採用相同的唯讀 State 與單次顯示 Session，但沒有玩家按鈕，Open 直接回傳 IDisposable，不建立空的操作介面或等待型 Presenter。定位參考 RectTransform 作為 View 方法參數，留在視覺層，不放入純顯示 State。

[AllyHandCardView](../Assets/Scripts/GameView/CardView/AllyHandCardView.cs) 協調懸停開始與結束，持有單次 [HandCardFocusSession](../Assets/Scripts/GameView/CardView/HandCardFocusSession.cs)。Session 記住原卡片與位移識別，從卡片來源衍生 FocusCardDetailDisplayState，持有詳情 View Session 與衍生狀態。切換卡片、離開、開始拖曳、停用手牌操作及回收焦點卡時，外層只需釋放懸停 Session，由它依序解除詳情綁定、解除來源訂閱、還原原卡片與鄰卡位移；擁有者銷毀時也走相同清理。初始化部分失敗會釋放已建立的資源，再傳遞例外。

Focus 只依 State 同步繪製卡面與提示，卡面使用 RenderSnapshot，不另外查詢或訂閱卡片資料。Session.Dispose 重設卡片、回收提示並隱藏視窗，OnDestroy 轉交同一入口。固定詳情卡清理時保留原本的本地位置與旋轉，避免物件池 Reset 的位置歸零破壞 prefab 排版，造成再次顯示下移。此模式由既有懸停互動協調端持有，維持 View 內的視覺互動責任。

### 手牌拖曳

[HandCardDragSession](../Assets/Scripts/GameView/CardView/HandCardDragSession.cs) 管理一次拖曳的起始位置、指標偏移、目前位置、卡片來源訂閱、候選目標高亮、箭頭及卡片還原。AllyHandCardView 持有 Session，轉交移動與放開事件；拖曳進度直接以 Session 的存在與方法呼叫表達，不透過 ReactiveProperty／Scan 比較前後值。

Update 更新指標位置，卡片來源通知則以目前位置刷新視覺提示。Release 使用該次放開事件的座標與最新卡片資訊，重新判斷命中並回傳可選的既有 UseCardCommand，隨後清理；外層才將命令交給既有 GameplayPresenter。命中與型別篩選只是視覺候選判斷，不取代 Model 的出牌合法性。

Dispose 只解除訂閱、高亮與箭頭並還原卡片，不產生命令。停用操作、重新綁定操作、回收拖曳卡及擁有者銷毀均釋放 Session；已結束操作範圍的 callback 不接收遲到的放開事件，避免取消後出牌。

### 手牌輸入

CardView.OpenHandInput 回傳 [IHandCardInputSession](../Assets/Scripts/Presentation/Abstractions/IHandCardInputSession.cs)，以 PointerEntered／PointerExited 回報卡片身份，以 DragStarted／Dragged／DragEnded 回報身份與指標座標。卡面資料更新與輸入綁定各有入口，啟用手牌操作不再透過帶 callback 的 Render 重新繪製卡片。

AllyHandCardView 持有各張卡的輸入 Session 與操作訂閱，將事件交給既有懸停／拖曳協調；PointerExited 只結束同身份焦點，避免舊卡的離開事件關閉新焦點。事件身份固定於本次綁定，CardView.Reset、切換顯示模式或銷毀均結束舊 Session，物件池重用後舊事件不能變成新卡操作。Dispose 完成事件與解除綁定，不偽造 Exit 或 EndDrag。

CardView 擁有自己的位移 Tween；銷毀元件或 GameObject 時同步終止 Tween，避免目標 RectTransform 銷毀後仍被動畫更新。Reset 與位置更新沿用既有的位移替換與還原規則。

OpenHandInput 只管理子 View 的輸入生命週期，因此不另外建立顯示 State；卡面仍沿用既有資訊觀察。卡片列表內部的點擊／長按模式仍使用 CardClickableProperty，尚未一併遷移。

## 建構與顯示狀態

- [ScriptableDataLoader](../Assets/Scripts/Presenter/Gameplay/ScriptableDataLoader.cs) 從 Catalog、玩家配置與 Excel 表讀取內容。
- [Context](../Assets/Scripts/Presenter/Gameplay/Context.cs) 保存載入資料，由 Main 建立及持有，並非單例服務。
- [BattleBuilder.cs](../Assets/Scripts/Presenter/Gameplay/BattleBuilder.cs) 內的類別目前拼為 BattleBuidler，依傳入種子組裝 Library、Factory 與本場亂數；GameFlow 傳遞 GameStageSetting。臨時兩關設定與種子產生集中於 TemporaryBattleRoute，正式地圖結構尚未設計。
- [GameStageSetting](../Assets/Scripts/GameModel/GameStageSetting.cs) 位於 GameModel。
- [GameInfoModel.cs](../Assets/Scripts/Presenter/Gameplay/GameInfoModel.cs) 內的 GameViewModel 管理卡片與 Buff 等可觀察顯示資料，並非所有 UI 數值的唯一更新途徑。

## 結果與地圖限制

Lose 面板透過 Session 提供 Retry／Restart／Quit 操作；Win 面板透過 Session 的 ContinueRequested 正常完成。Presenter 只有正常操作與角色清理完成後才回傳結果，取消／例外清理面板並向上傳遞。GameFlow 於正常勝利分支提交戰後 Domain 快照，再決定場景去向；Presenter 不直接寫回 Context。地圖目前只有點擊開戰入口；完整場景結果處理見 [Scene](Scene.md)，缺口列於 [TODO](TODO.md)。
