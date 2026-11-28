# Instance 跨戰鬥狀態

> 核對日期：2026-11-28

Instance 是 Level／Run 期間可延續的 Domain 狀態，介於設計 Data 與戰鬥 Entity 之間。目前只有 Card 與 Ally 有此層；Enemy 與 Buff 直接由配置建立 Runtime 實體。

Record 提供值式更新語法，但不保證內含集合深度不可變，例如 AllyInstance.Deck 仍是 List。Instance 也不是已完成的磁碟存檔格式。

## 卡片身份與形態

[CardInstance](../Assets/Scripts/GameModel/Instance/CardInstance.cs) 保存原始 CardData ID、來源身份、附加屬性及可選持久形態。戰鬥 Entity 使用自己的 Identity，另保留 OriginCardInstanceGuid 連回來源。

[CardInstancePersistenceMapper](../Assets/Scripts/GameModel/Instance/CardInstancePersistenceMapper.cs) 只接受來源身份相符的 Entity／Instance：

- Persistent Self Form 可寫回；Base CardData ID 保持不變。
- BattleOnly、已還原或無 Self Form 會清除既有持久形態，不回溯歷史。
- External Override 永不寫回。
- Runtime 建立與 Clone 的卡片沒有來源 Instance，不收集為既有卡片更新。

## 勝利輸出

[CardInstanceChangeSet](../Assets/Scripts/GameModel/Instance/CardInstanceChangeSet.cs) 依開戰原牌組逐張輸出持久形態，包含未變形卡；None 代表清除既有持久形態，不代表略過更新。

GameplayManager 僅在勝利結束時建立 [AllyBattleCompletion](../Assets/Scripts/GameModel/Instance/AllyBattleCompletion.cs)，將來源玩家身份、剩餘血量、好感度與卡片形態變更放在同一份快照。輸出不持有戰鬥 Entity，也不包含能量、護盾、Buff、資源上限或牌組增刪。失敗的完成快照為 None；取消／例外不產生正常結果。

Presenter 等待勝利面板的繼續操作正常完成，才將快照隨場景結果向上傳遞；勝負與快照不一致會拒絕。

## 戰後套用與提交入口

AllyInstance.ApplyBattleCompletion 依來源玩家身份及完整原牌組身份集合核對快照，拒絕重複、未知或遺漏的卡片變更，再建立新的 AllyInstance。牌組依原順序保留成員、InstanceGuid、原 CardData ID 與附加屬性，只更新持久形態；血量與好感度採用快照，能量設為原 MaxEnergy，其餘配置沿用。

[Context](../Assets/Scripts/Presenter/Gameplay/Context.cs).CommitBattleCompletion 只在完整套用成功後替換 Ally 參考。套用過程不修改原實例與原牌組，資料錯誤不會造成部分提交；ResetRun 後的身份亦會拒絕上一輪快照。此入口不決定勝負、取消或場景去向，也不提供舊結果重播偵測。

GameFlow 在 Scene.Run 正常回傳並通過取消檢查後，僅於勝利分支呼叫提交入口，再推進關卡。下一戰讀取已提交的 AllyInstance；失敗／取消／例外不提交未完成結果，Retry 保留本關開戰前狀態。提交後的下一場景載入失敗不回滾既有結果。磁碟存檔仍未實作，見 [TODO](TODO.md)。

[AllyInstance](../Assets/Scripts/GameModel/Instance/AllyInstance.cs) 是玩家跨戰鬥狀態容器；後續存檔若需版本相容與外部輸入驗證，應另定義儲存邊界，不能將未存在的 SaveData 類別列為既有實作。

形態優先順序與 Clone 規則見 [CardTransformation](CardTransformation.md)。
