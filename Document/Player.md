# Player 玩家系統

> 核對日期：2026-09-19

[PlayerEntity](../Assets/Scripts/GameModel/Entity/Player/PlayerEntity.cs) 是戰鬥控制單位，組合角色、卡牌、能量與 PlayerBuff。MainCharacter 死亡即視為玩家死亡。Ally 另有好感度與來源 Instance，Enemy 另有 AI 選牌及回合資源配置。

## 資源與 Buff

[EnergyManager](../Assets/Scripts/GameModel/Entity/Player/EnergyManager.cs) 維護合法能量範圍。雙方在回合開始恢復能量，主動出牌支付費用，效果可另行增減。

[DispositionManager](../Assets/Scripts/GameModel/Entity/Player/DispositionManager.cs) 管理友軍好感度；[DispositionLibrary](../Assets/Scripts/GameData/DispositionLibrary.cs) 將目前數值對應至回合能量恢復與抽牌數。

PlayerBuff 可藉 GameTiming 執行共用效果，也可提供公式查詢的屬性修正。可建立的屬性以 [PlayerBuffLifePropertyData](../Assets/Scripts/GameData/PlayerBuff/PlayerBuffLifePropertyData.cs) 為準；列舉成員不保證有資料實作或完整消費端。壽命與記憶見 [Session](Session.md)，效果來源規則見 [Effect](Effect.md)。

## 卡片管理

[PlayerCardManager](../Assets/Scripts/GameModel/Entity/Player/PlayerCardManager.cs) 協調五種一般區域與 PlayingCard 暫態。

- 出牌先離開手牌，結算期間留在 PlayingCard；結束時 Dispose／AutoDispose 進入 ExclusionZone，其餘進入 Graveyard。
- Recycle 僅嘗試將剛打出且位於墓地的該卡移回手牌。
- 回合清手先固定資格與目的地，再提交移牌事件及生命週期效果。
- 全域查找含 PlayingCard；一般反應不包含 DisposeZone。

不同操作的移牌及觸發契約集中於 [Card](Card.md)，避免將效果棄牌與出牌離場規則混為一談。

## 跨戰鬥狀態

AllyInstance 保存 Domain 狀態，目前尚無完整戰後數值寫回與磁碟存檔流程。卡片持久形態已有勝利 ChangeSet 收集，實際戰鬥外套用仍未接線，見 [Instance](Instance.md) 與 [TODO](TODO.md)。
