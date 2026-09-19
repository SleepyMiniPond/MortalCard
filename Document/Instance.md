# Instance 跨戰鬥狀態

> 核對日期：2026-09-19

Instance 是 Level／Run 期間可延續的 Domain 狀態，介於設計 Data 與戰鬥 Entity 之間。目前只有 Card 與 Ally 有此層；Enemy 與 Buff 直接由配置建立 Runtime 實體。

Record 提供值式更新語法，但不保證內含集合深度不可變，例如 AllyInstance.Deck 仍是 List。Instance 也不是已完成的磁碟存檔格式。

## 卡片身份與形態

[CardInstance](../Assets/Scripts/GameModel/Instance/CardInstance.cs) 保存原始 CardData ID、來源身份、附加屬性及可選持久形態。戰鬥 Entity 使用自己的 Identity，另保留 OriginCardInstanceGuid 連回來源。

[CardInstancePersistenceMapper](../Assets/Scripts/GameModel/Instance/CardInstancePersistenceMapper.cs) 只接受來源身份相符的 Entity／Instance：

- Persistent Self Form 可寫回；Base CardData ID 保持不變。
- BattleOnly、已還原或無 Self Form 會清除既有持久形態，不回溯歷史。
- External Override 永不寫回。
- Runtime 建立與 Clone 的卡片沒有來源 Instance，不收集為既有卡片更新。

## 勝利輸出與未接線部分

[CardInstanceChangeSet](../Assets/Scripts/GameModel/Instance/CardInstanceChangeSet.cs) 已能收集卡片持久形態差異；GameplayManager 僅在勝利結果輸出，失敗及取消不提交。

這不等於已套用至跨戰鬥牌組。現有 Main 尚未消費 ChangeSet，也沒有完整的血量、能量、好感度、牌組寫回或磁碟存檔流程。勝利面板正常完成入口亦未完成，見 [Scene](Scene.md)、[TODO](TODO.md)。

[AllyInstance](../Assets/Scripts/GameModel/Instance/AllyInstance.cs) 是玩家跨戰鬥狀態容器；後續存檔若需版本相容與外部輸入驗證，應另定義儲存邊界，不能將未存在的 SaveData 類別列為既有實作。

形態優先順序與 Clone 規則見 [CardTransformation](CardTransformation.md)。
