# GameData 資料定義層

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

GameData 是整個專案的**資料基礎層**，負責定義所有遊戲實體的靜態配置。這一層的核心原則是：**資料與行為完全分離**。所有的 Data 類別都是純粹的資料容器，不包含任何遊戲邏輯，但透過 `CreateEntity()` 工廠方法提供從設計資料到運行時實體的橋樑。

設計師透過 Odin Inspector 在 Unity 編輯器中直接編輯這些資料，無需接觸程式碼。

## 子系統總覽

```
GameData/
├── GameEnum.cs              # 集中式枚舉庫（23+ 枚舉型別）
├── AttributeData.cs         # 屬性資料定義
├── DispositionLibrary.cs    # 好感度查詢服務
├── Card/                    # 卡牌資料系統
├── CardBuff/                # 卡牌 Buff 資料系統
├── CharacterBuff/           # 角色 Buff 資料系統
├── Player/                  # 玩家資料系統
├── PlayerBuff/              # 玩家 Buff 資料系統
├── Session/                 # 反應會話系統
└── Scriptable/              # ScriptableObject 容器
```

## 枚舉系統設計（GameEnum + CardEnum）

專案將所有枚舉集中管理在 `GameEnum.cs` 與 `CardEnum.cs` 兩個檔案中，這是一個刻意的設計決策——避免枚舉散落各處造成維護困難。

### 核心遊戲枚舉

| 枚舉 | 用途 | 設計要點 |
|------|------|----------|
| `Faction` | 陣營識別（Ally/Enemy） | 最基礎的二元分類 |
| `DamageType` | 傷害類型（Normal/Penetrate/Additional/Effective） | 決定護甲穿透規則 |
| `DamageStyle` | 攻擊風格（FullAttack/QuickAttack 等） | Flags 枚舉，可組合 |
| `EffectType` | 效果類型（20+ 種） | 對應所有 CardEffect 實作 |
| `GameTiming` | 遊戲時機（17 個觸發點） | Buff 反應系統的核心時機 |
| `MoveCardType` | 卡牌移動類型（Draw/Discard/Recycle/Consume/Dispose） | 卡牌區域轉換語義 |
| `PlayerBuffProperty` | 玩家 Buff 屬性（16 種） | 數值修正類型 |

### 卡牌專屬枚舉

| 枚舉 | 用途 | 設計要點 |
|------|------|----------|
| `CardType` | 卡牌類型（Attack/Defense/Speech 等 6 種） | 分類過濾基礎 |
| `CardRarity` | 稀有度（5 級） | 影響卡池與平衡 |
| `CardTheme` | 門派主題（5 大門派） | 武俠世界觀核心 |
| `CardProperty` | 卡牌屬性（10 種 Flags） | Preserved/Consumable/Sealed 等行為標記 |
| `CardTriggeredTiming` | 卡牌觸發時機（11 種） | 抽到/打出/保留/丟棄等 |
| `CardCollectionType` | 卡牌區域（5 種） | Deck/Hand/Graveyard/Exclusion/Dispose |

## 卡牌資料系統

詳見：[Card 卡牌系統](Card.md)

### CardData — 卡牌模板

每張卡牌的完整定義，是三層架構中最上層的設計資料。包含：
- **基礎屬性**：ID、稀有度、類型、門派、費用、威力
- **目標選取規則**：主目標（MainSelect）+ 子目標（SubSelects）
- **直接效果列表**：打出時立即觸發的 ICardEffect 鏈
- **觸發效果列表**：基於 CardTriggeredTiming 的條件觸發效果
- **屬性工廠列表**：透過 ICardPropertyData.CreateEntity() 產生運行時屬性

### CardEffect — 效果介面體系

所有卡牌效果實作 `ICardEffect` 介面，按目標類型分為三大類：
- **角色目標效果**：DamageEffect、ShieldEffect、HealEffect 等
- **玩家目標效果**：GainEnergyEffect、AddPlayerBuffEffect 等
- **卡牌目標效果**：DrawCardEffect、CreateCardEffect、AddCardBuffEffect 等

每個效果都使用 `IIntegerValue`（抽象數值）和 `ITargetCollectionValue`（抽象目標）進行參數化，實現高度組合性。

### CardLibrary / CardViewLibrary

`CardLibrary` 是卡牌資料的唯讀查詢服務（Dictionary 封裝），透過 ID 查詢 CardData。`CardViewLibrary` 為 UI 顯示預留的擴展點。

## Buff 資料系統（三套平行結構）

專案有三套結構高度相似的 Buff 系統，分別作用於不同層級的遊戲實體：

| Buff 類型 | 作用對象 | 典型用途 |
|-----------|----------|----------|
| **[CardBuff](CardBuff.md)** | 單張卡牌 | 封印、威力加成 |
| **[CharacterBuff](Character.md)** | 單個角色 | 生命上限、能量上限 |
| **[PlayerBuff](Player.md)** | 整個玩家 | 全域傷害加成、費用減免 |

### 共通 Buff 結構

三套 Buff 都遵循相同的設計範式：

```
BuffData（模板）
├── ID, MaxLevel          # 識別與疊加上限
├── Sessions{}            # 反應會話（動態狀態追蹤）
├── BuffEffects{}         # 時機→條件效果 的映射
├── PropertyDatas[]       # 屬性修正工廠列表
└── LifeTimeData          # 生命週期策略
    ├── Always            # 永久
    ├── TurnBased         # N 回合後消失
    └── HandCard          # 卡牌離開手牌時消失（僅 CardBuff）
```

### BuffLibrary — 查詢服務

每套 Buff 都有對應的 Library（如 `PlayerBuffLibrary`），提供 `GetBuffEffects(id, timing)` 等查詢方法，回傳值使用 **Option 模式** 處理「Buff 不存在」或「該時機無效果」的情況。

## 玩家資料系統

詳見：[Player 玩家系統](Player.md)

### PlayerData — 基礎模板

定義玩家的基礎數值：生命值、能量值、牌組引用、手牌上限。

### AllyData — 友軍特化

繼承 PlayerData，額外包含：
- **GameMode**：遊戲模式
- **InitialDisposition**：初始好感度（0-10），這是友軍獨有的機制

### EnemyData — 敵軍特化

繼承 PlayerData，額外包含 AI 行為參數：
- **SelectedCardMaxCount**：每回合最多選幾張牌
- **TurnStartDrawCardCount**：回合開始自動抽牌數
- **EnergyRecoverPoint**：回合能量回復量

## 反應會話系統（Session）

詳見：[Session 反應會話](Session.md)

Session 是 Buff 系統中最精巧的設計——讓 Buff 能夠追蹤動態狀態（例如「本回合已觸發幾次」）。

### ReactionSessionData

定義兩種會話值：
- **SessionBoolean**：布林狀態，支援 AND/OR/覆寫 操作
- **SessionInteger**：整數計數器，支援 加法/覆寫 操作

每個會話值都有：
- **LifeTime**：作用域（整場遊戲 / 本回合 / 本次打牌）
- **UpdateRules**：在特定 GameTiming 時、滿足特定條件時，如何更新值

## ScriptableObject 容器層

`Scriptable/` 目錄下的類別負責將 Data 類別包裝為 Unity 可序列化的 ScriptableObject 資產：

```
AllCardScriptable          # 聚合所有 CardDataScriptable
├── CardDataScriptable     # 單張卡牌的 SO 容器
AllPlayerBuffScriptable    # 聚合所有 PlayerBuffDataScriptable
├── PlayerBuffDataScriptable
DeckScriptable             # 牌組定義（CardDataScriptable[] 陣列）
ExcelDatas                 # Excel 匯入的本地化與常數表
```

### ExcelDatas — 外部資料橋接

透過 ExcelAsset 屬性連結 `MortalGames.xlsx`，匯入：
- **常數表**：遊戲平衡數值
- **好感度表**：好感度等級定義（回復能量、抽牌數）
- **本地化表**：卡牌名稱/描述、Buff 名稱/描述、關鍵字說明、UI 文字

## 好感度系統（DispositionLibrary）

好感度是友軍獨有的機制，透過 `DispositionLibrary` 查詢：
- 根據當前好感度值查詢對應的好感度等級
- 每個等級定義不同的回復能量與抽牌加成
- 本地化名稱與說明

## 設計模式總結

| 模式 | 應用 |
|------|------|
| **工廠方法** | PropertyData.CreateEntity()、LifeTimeData.CreateEntity() |
| **服務定位器** | CardLibrary、各 BuffLibrary 提供 ID 查詢 |
| **Option 模式** | BuffLibrary 查詢回傳 Option 類型 |
| **標記介面** | ICardEffect、ICardBuffEffect 等作為型別安全多型 |
| **組合模式** | 效果列表、條件列表均可自由組合 |
| **值物件** | IIntegerValue、ITargetCollectionValue 抽象參數 |
| **Flags 枚舉** | CardProperty、DamageStyle 支援位元組合 |
| **包裝器** | ScriptableObject 包裝 Data 類別實現序列化 |
