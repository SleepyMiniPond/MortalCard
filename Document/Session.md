# Session 反應記憶

> 核對日期：2026-09-19

Session 讓 Buff 或 External Override 記住反應過程的狀態，例如本回合是否觸發、累計次數。資料描述初始值、更新條件與有效期間，Runtime 依收到的 Action 更新，不直接修改遊戲其他狀態。

## 有效期間

| LifeTime | 建立與重置 | 清除 |
|---|---|---|
| WholeGame | 建立時初始化 | 不依回合自動清除 |
| WholeTurn | 建立時初始化，BeforeTurnStart 重置 | AfterTurnEnd |
| PlayCard | 初始無值，BeforePlayCardStart 重置 | AfterPlayCardEnd |

這不是首次讀取才初始化的機制。無有效值與數值為 0／false 是不同狀態；IsSessionValueUpdated 目前表示有有效 Session 值，不代表某條規則剛匹配。

生命週期來源：[ReactionSessionEntity](../Assets/Scripts/GameModel/Entity/Session/ReactionSessionEntity.cs)。

## 更新規則

布林支援覆寫、AND、OR；整數支援覆寫及累加。規則取第一個相符的 Action 時機群組，再採其中第一條符合條件的更新，不會累積執行所有匹配規則；整數缺值不偽裝為 0。一般 Timing 管線先更新 Session，再判斷 Buff 效果；出牌 Intent／Result 等觀察亦可更新 Session。

配置見 [ReactionSessionData](../Assets/Scripts/GameData/Session/ReactionSessionData.cs)、[SessionValueUpdateRule](../Assets/Scripts/GameData/Session/SessionValueUpdateRule.cs)，行為見 [SessionValueEntity](../Assets/Scripts/GameModel/Entity/Session/SessionValueEntity.cs)。

External Override 的 Session 與 Base Buff 狀態分離；解除規則只在明確 Timing 派送時判斷，見 [CardTransformation](CardTransformation.md)。

同目錄的 [SelectedCardEntity](../Assets/Scripts/GameModel/Entity/Session/SelectedCardEntity.cs) 是敵人選牌容器，不屬於上述反應記憶契約。
