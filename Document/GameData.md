# GameData 系統 - 資料定義層架構總覽

## 🎯 系統定位與職責

**GameData 是 MortalGame 的核心資料定義層**，負責定義遊戲中所有實體的靜態數據結構與配置規範。採用**三層資料架構**設計：Data 類別定義 → ScriptableObject 封裝 → 運行時實例化。

## 📊 系統架構設計

### 資料分層設計思路
**Data 定義層**：純粹的資料結構類別，定義各種遊戲實體的屬性與行為配置
**ScriptableObject 層**：Unity 資產封裝，提供編輯器友善的資料配置介面  
**運行時層**：由 GameModel 系統將靜態資料轉換為動態遊戲實例

### 核心子系統職責

#### [Card](Assets/Scripts/GameData/Card) - 卡牌資料系統
定義卡牌的基礎屬性、效果觸發機制、主題分類等核心卡牌遊戲邏輯的資料結構

#### [CardBuff](Assets/Scripts/GameData/CardBuff) - 卡牌增益系統  
管理卡牌上的臨時效果、狀態修飾器與生命週期控制的資料定義

#### [CharacterBuff](Assets/Scripts/GameData/CharacterBuff) - 角色增益系統
處理角色身上的持續效果、屬性修飾與狀態變化的資料結構

#### [Player](Assets/Scripts/GameData/Player) - 玩家角色資料
定義玩家與敵人的基礎屬性、初始配置、牌組關聯等角色實體資料

#### [PlayerBuff](Assets/Scripts/GameData/PlayerBuff) - 玩家增益系統
管理玩家角色的狀態效果、屬性加成與視覺呈現的資料配置

#### [Scriptable](Assets/Scripts/GameData/Scriptable) - ScriptableObject 封裝層
將所有 Data 類別封裝為 Unity ScriptableObject，提供編輯器資產管理與配置介面

#### [Session](Assets/Scripts/GameData/Session) - 會話資料系統  
定義反應式效果觸發、數值更新規則等跨系統協作的資料結構

### 條件式效果系統
透過 [GameEnum.cs](Assets/Scripts/GameData/GameEnum.cs) 定義的豐富枚舉系統，支援複雜的條件判斷與效果觸發邏輯
