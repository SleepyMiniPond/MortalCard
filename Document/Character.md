# Character 角色系統

> 最後更新：2026-04-20 | 版本：v2.0

## 設計理念

角色（Character）是戰鬥場上的**生命單位**，承載血量、護甲與角色 Buff。角色是判定勝負的核心——當一方所有角色死亡時，該玩家失敗。

角色系統刻意保持輕量，將狀態管理委派給 HealthManager 和 CharacterBuffManager。角色本身主要是這些子系統的**組合容器**。

## CharacterEntity — 角色實體

### 組成結構

```
CharacterEntity
├── Identity (Guid)         # 唯一身份
├── NameKey                 # 本地化名稱鍵
├── HealthManager           # 血量/護甲管理
│   ├── Hp / MaxHp          # 生命值
│   └── Dp                  # 護甲值（Defense Points）
├── CharacterBuffManager    # 角色 Buff 管理
│   └── CharacterBuffEntity[]
└── IsDead                  # HP ≤ 0
```

### 建構方式

透過 `CharacterParameter` Record 建構，包含：
- Identity、NameKey
- MaxHealth、CurrentHealth
- MaxEnergy（影響所屬玩家的能量上限）

### Dummy 模式

`DummyCharacter.Instance` 提供 Null Object 實作，確保安全的預設引用。

## HealthManager — 雙層防禦機制

這是角色系統中最重要的子系統，實現了**護甲優先吸收**的傷害模型。

### 設計模型

```
傳入傷害
    ↓
判斷傷害類型
    ├── Normal / Additional → 護甲優先吸收
    │   ├── 傷害 ≤ 護甲 → 全額扣護甲
    │   └── 傷害 > 護甲 → 護甲歸零 + 溢出扣血
    └── Penetrate / Effective → 無視護甲，直接扣血
```

### 操作與結果

| 操作 | 回傳結果 | 關鍵資訊 |
|------|----------|----------|
| TakeDamage | TakeDamageResult | 實際扣血、護甲吸收、溢出值 |
| GetHeal | GetHealResult | 實際回復量（不超過上限） |
| GetShield | GetShieldResult | 護甲增加量 |

所有結果都是帶 Delta 值的結構，供事件系統產生精確動畫。

## CharacterBuff — 角色 Buff 系統

### CharacterBuffData（設計時）

```
CharacterBuffData
├── ID, MaxLevel          # 識別與最大疊加層數
├── Sessions{}            # 反應會話
├── BuffEffects{}         # GameTiming → ConditionalCharacterBuffEffect[]
├── PropertyDatas[]       # 屬性修正工廠列表
└── LifeTimeData          # 生命週期策略
```

### 效果類型

- `EffectiveDamageCharacterBuffEffect`：對目標造成確實傷害（無視護甲）

### 屬性修正

| 屬性 | 效果 |
|------|------|
| `MaxHealthPropertyCharacterBuffEntity` | 增加角色最大生命值 |
| `MaxEnergyPropertyCharacterBuffEntity` | 增加所屬玩家最大能量 |

### 生命週期

- `AlwaysLifeTime`：永久
- `TurnLifeTime`：N 回合後過期

### CharacterBuffManager

管理單個角色的所有 Buff，提供新增/移除/修改/更新操作。`Update()` 回傳變化的 Buff 集合，供事件產生。

### CharacterBuffLibrary

查詢服務，使用 **Option 模式** 處理「無此 Buff」或「該時機無效果」的情況。

## 與其他系統的關係

- **PlayerEntity**：角色隸屬於玩家，玩家持有角色集合
- **HealthManager**：角色的核心戰鬥數值管理
- **Effect 系統**：傷害/治療/護甲效果直接操作 HealthManager
- **GameFormula**：傷害計算會查詢角色的 Buff 屬性修正
- **GameView/CharacterView**：角色的視覺呈現

## 相關文件

- [Entity 實體系統](Entity.md) — CharacterEntity 在實體階層中的位置
- [Player 玩家系統](Player.md) — 角色的擁有者
- [Effect 效果管線](Effect.md) — 作用於角色的效果處理
- [CharacterView 角色視圖](CharacterView.md) — 角色的視覺呈現
