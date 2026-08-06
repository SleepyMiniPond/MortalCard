# Instance 實例層

> 最後更新：2026-08-07 | 版本：v2.1

## 設計理念

Instance 層是三層資料架構的**中間橋樑**，代表 Level／Run 期間可跨戰鬥延續的 Domain 狀態。它的存在解決了一個核心問題：**Data 是不可變的設計模板，Entity 是戰鬥中的可變物件，那跨戰鬥狀態該存在哪裡？**

答案就是 Instance——使用 C# Record 類型確保不可變性，攜帶唯一 Guid 確保身份追蹤，同時只保存跨場景需要持久化的資訊。Instance 是 Domain Model；未來磁碟存檔會使用獨立 SaveData DTO 與它轉換，不直接把 Instance 當作檔案契約。

## CardInstance — 卡牌實例

### 職責
從 CardData（設計模板）到 CardEntity（戰鬥物件）的中間態，代表牌組中的一張具體卡牌。

### 設計要點
- **Record 類型**：不可變，天生適合序列化
- **InstanceGuid**：唯一識別碼，跨場景/存檔追蹤
- **CardDataId**：指向設計模板（而非直接引用物件）
- **AdditionPropertyDatas**：附加屬性（超出原始設計的額外修正）
- **PersistentFormState**：可選的跨戰鬥 Self Form，只保存 TransformKey 與形態 CardDataId

### 建構流程

```
CardData（設計師配置）
    ↓ CardInstance.Create(CardData)
CardInstance（持久化快照）
    ↓ CardEntity.CreateFromInstance(CardInstance, CardLibrary)
CardEntity（戰鬥實體）
    ↓ CardInstancePersistenceMapper.TryUpdate(CardEntity, CardInstance)
新的 CardInstance（保留或清除 Persistent Form）
```

### 設計意義

一個 CardData 可以產生多個 CardInstance（例如牌組中有 3 張「基本攻擊」），每個 Instance 有獨立 Guid。戰鬥開始時，每個 Instance 轉化為 CardEntity，在戰鬥中擁有獨立的 Buff 和狀態。

### CardForm 持久化邊界

- `CardDataId` 永遠代表 Base Form，不因變身而覆蓋。
- `PersistentFormState` 存在時，`CardEntity.CreateFromInstance()` 會原子建立 Persistent Self Form，並以該形態的 CardData Property 作為初始有效資料。
- `BattleOnly` Form 不寫回 CardInstance；若它取代舊 Persistent Form，寫回結果會清除舊狀態，不回看歷史形態。
- `CardInstancePersistenceMapper.TryUpdate()` 只接受 `OriginCardInstanceGuid` 相符的 Entity／Instance，並回傳新的 CardInstance Record。
- Runtime 建立的卡片與 Clone 沒有來源 Instance，因此不會被寫回。
- 正式磁碟存檔功能實作時，使用獨立 `CardInstanceSaveData` 處理版本相容、外部資料驗證與多型 Property 序列化。

## AllyInstance — 友軍實例

### 職責
持久化友軍玩家的完整狀態，用於跨場景保存進度。

### 包含資訊
- **Identity**：玩家識別碼
- **NameKey**：本地化名稱鍵
- **CurrentDisposition**：當前好感度
- **CurrentHealth / MaxHealth**：生命值狀態
- **CurrentEnergy / MaxEnergy**：能量狀態
- **Deck**：卡牌實例列表（`List<CardInstance>`）
- **HandCardMaxCount**：手牌上限

### 設計意義

AllyInstance 是「遊戲進度」的具體化。當玩家完成一場戰鬥後，AllyInstance 可以記錄戰後狀態（剩餘血量、牌組變化、好感度變動），並帶入下一場戰鬥。

## 三層架構中的位置

```
Data 層（設計時）      Instance 層（持久化）    Entity 層（戰鬥時）
───────────────     ─────────────────     ─────────────────
CardData            CardInstance           CardEntity
PlayerData          AllyInstance           AllyEntity
EnemyData           ─                      EnemyEntity
CardBuffData        ─                      CardBuffEntity
PlayerBuffData      ─                      PlayerBuffEntity
CharacterBuffData   ─                      CharacterBuffEntity
```

注意：目前只有 Card 和 Ally 有明確的 Instance 層。Buff 和 Enemy 直接從 Data 轉為 Entity，因為它們不需要跨場景持久化。這是一個務實的設計——不為不需要的功能建立抽象層。

## 與其他系統的關係

- **GameData**：提供 Data 模板，Instance 從中建構
- **Entity 系統**：從 Instance 建構運行時實體
- **BattleBuilder**：在戰鬥建構時將 AllyInstance 轉換為 AllyEntity
- **Scene/Presenter**：跨場景傳遞 Instance 作為遊戲進度

## 相關文件

- [SystemArchitecture 架構總覽](SystemArchitecture.md) — 三層架構說明
- [Card 卡牌系統](Card.md) — CardInstance 的使用
- [Player 玩家系統](Player.md) — AllyInstance 的使用
- [Entity 實體系統](Entity.md) — Entity 層的完整結構
