# T-002 實作計畫：接通 CardBuff / CharacterBuff 觸發管線

> 狀態：⬜ 未開始  
> 相依：T-001 已完成（Resolver/Handler 架構就緒）  
> 影響檔案：12 處修改，4 個新建檔案

---

## 背景與設計說明

### 現況

`GameplayManager._TriggerTiming()` 中有 6 個 foreach 迴圈，只有 PlayerBuff（Ally + Enemy）有完整實作：

```
PlayerBuff (Ally)     → ✅ 完整：查 Library → 評估條件 → Resolve → Execute
CharacterBuff (Ally)  → ❌ 空殼
CardBuff (Ally)       → ❌ 空殼（已改為枚舉全部區域）
PlayerBuff (Enemy)    → ✅ 完整
CharacterBuff (Enemy) → ❌ 空殼
CardBuff (Enemy)      → ❌ 空殼（已改為枚舉全部區域）
```

### Timing Enum 設計說明

`CardBuff` 的 `Effects` 欄位使用 `CardTriggeredTiming`（卡牌生命週期：被抽到、被打出等），
與 `PlayerBuff` / `CharacterBuff` 使用的 `GameTiming`（遊戲流程節點：回合開始、回合結束等）語義不同，這是刻意的分離，**不是設計失誤**。

問題在於 `CardBuff` 缺少「被動全域觸發」的設計空間。本次在 `CardBuffData` 新增第二個欄位：

```
CardBuffData.Effects     → CardTriggeredTiming（既有）→ 跟著牌的生命週期觸發
CardBuffData.BuffEffects → GameTiming（新增）        → 跟著遊戲時機的被動觸發
```

---

## Phase 1 — CharacterBuff（步驟 1–5）

### Step 1：`ActionSource.cs` — 新增 CharacterBuffSource

**檔案**：`Assets/Scripts/GameModel/Action/ActionSource.cs`

在 `CardBuffSource` 下方新增一行：

```csharp
public record CharacterBuffSource(ICharacterBuffEntity Buff) : IActionSource;
```

---

### Step 2：新建 `ICharacterBuffEffectResolver.cs`

**路徑**：`Assets/Scripts/GameModel/Effect/Resolvers/ICharacterBuffEffectResolver.cs`

```csharp
public interface ICharacterBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICharacterBuffEffect effect);
}
```

---

### Step 3：新建 `DamageCharacterBuffEffectResolver.cs`

**路徑**：`Assets/Scripts/GameModel/Effect/Resolvers/DamageCharacterBuffEffectResolver.cs`

```csharp
using System;
using System.Collections.Generic;

public class DamageCharacterBuffEffectResolver : ICharacterBuffEffectResolver
{
    public EffectCommandSet Resolve(TriggerContext context, ICharacterBuffEffect effect)
    {
        var e = (EffectiveDamageCharacterBuffEffect)effect;
        var effectCommands = new List<IEffectCommand>();
        var intent = new DamageIntentAction(context.Action.Source, DamageType.Effective);
        var triggerContext = context with { Action = intent };
        var targetEntities = e.Targets.Eval(triggerContext);

        foreach (var target in targetEntities)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new DamageIntentTargetAction(context.Action.Source, characterTarget, DamageType.Effective);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var damagePoint = e.Value.Eval(targetTriggerContext);
            var damageFormulaPoint = GameFormula.EffectiveDamagePoint(targetTriggerContext, damagePoint);
            effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, DamageType.Effective));
        }

        return new EffectCommandSet(effectCommands);
    }
}
```

---

### Step 4：`EffectDataResolver.cs` — 新增 CharacterBuff registry 與方法

**檔案**：`Assets/Scripts/GameModel/Effect/EffectDataResolver.cs`

在 `_playerBuffResolverRegistry` 下方新增：

```csharp
private static readonly Dictionary<Type, ICharacterBuffEffectResolver> _characterBuffResolverRegistry = new()
{
    [typeof(EffectiveDamageCharacterBuffEffect)] = new DamageCharacterBuffEffectResolver(),
};
```

在 `ResolvePlayerBuffEffect()` 方法下方新增：

```csharp
#region CharacterBuffEffect
public static EffectCommandSet ResolveCharacterBuffEffect(
    TriggerContext context,
    ICharacterBuffEffect buffEffect)
{
    if (_characterBuffResolverRegistry.TryGetValue(buffEffect.GetType(), out var resolver))
        return resolver.Resolve(context, buffEffect);

    Debug.LogWarning($"[EffectDataResolver] 未知的 ICharacterBuffEffect 類型：{buffEffect.GetType().Name}，回傳空 CommandSet");
    return new EffectCommandSet(Array.Empty<IEffectCommand>());
}
#endregion
```

---

### Step 5：`GameplayManager.cs` — 填入 Ally + Enemy CharacterBuff loop body

**檔案**：`Assets/Scripts/GameModel/GameplayManager.cs`

將兩個 `foreach (var character in ...)` 空殼替換為：

```csharp
foreach (var character in _gameStatus.Ally.Characters)
{
    using var characterContext = _contextMgr.SetSelectedCharacter(character.Some());
    foreach (var buff in character.BuffManager.Buffs)
    {
        var buffTrigger = new CharacterBuffTrigger(buff);
        var buffTriggerContext = new TriggerContext(this, buffTrigger, timingAction);
        var conditionalEffectsOpt = _contextMgr.CharacterBuffLibrary.GetBuffEffects(buff.CharacterBuffDataId, timing);
        conditionalEffectsOpt.MatchSome(conditionalEffects =>
        {
            foreach (var conditionalEffect in conditionalEffects)
            {
                if (conditionalEffect.Conditions.All(c => c.Eval(buffTriggerContext)))
                {
                    var updateTimingAction = new UpdateTimingAction(GameTiming.TriggerBuffStart, buffTriggerContext.Action.Source);
                    triggerEvents.AddRange(UpdateReactorSessionAction(updateTimingAction));

                    var resolvedCommand = EffectDataResolver.ResolveCharacterBuffEffect(buffTriggerContext, conditionalEffect.Effect);
                    var applyEvts = EffectCommandExecutor.ApplyEffectCommands(buffTriggerContext, resolvedCommand);
                    triggerEvents.AddRange(applyEvts.Events);

                    var nextTriggerSource = new CharacterBuffSource(buff);
                    triggerEvents.AddRange(_TriggerTiming(GameTiming.TriggerBuffEnd, nextTriggerSource));
                }
            }
        });
    }
}
```

Enemy 的 `foreach (var character in _gameStatus.Enemy.Characters)` 套用完全相同的 body（`_gameStatus.Enemy` 替換 `_gameStatus.Ally`）。

---

## Phase 2 — CardBuff（步驟 6–9）

### Step 6：`CardBuffData.cs` — 新增 GameTiming 字典

**檔案**：`Assets/Scripts/GameData/CardBuff/CardBuffData.cs`

在現有 `Effects` 欄位下方新增：

```csharp
[Space(20)]
[ShowInInspector]
[BoxGroup("Effects")]
public Dictionary<GameTiming, ConditionalCardBuffEffect[]> BuffEffects = new();
```

---

### Step 7：`CardBuffLibrary.cs` — 新增 GetBuffEffects(GameTiming)

**檔案**：`Assets/Scripts/GameData/CardBuff/CardBuffLibrary.cs`

在 `GetCardBuffData()` 下方新增：

```csharp
public Option<ConditionalCardBuffEffect[]> GetBuffEffects(string buffId, GameTiming triggerTiming)
{
    if (!_buffs.ContainsKey(buffId))
    {
        Debug.LogError($"CardBuff ID[{buffId}] not found in library.");
        return Option.None<ConditionalCardBuffEffect[]>();
    }

    return _buffs[buffId].BuffEffects.TryGetValue(triggerTiming, out var effects)
        ? effects.SomeNotNull()
        : Option.None<ConditionalCardBuffEffect[]>();
}
```

需在檔案頂部加 `using Optional;`。

---

### Step 8：新建 `ICardBuffEffectResolver.cs`

**路徑**：`Assets/Scripts/GameModel/Effect/Resolvers/ICardBuffEffectResolver.cs`

```csharp
public interface ICardBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICardBuffEffect effect);
}
```

---

### Step 9：`EffectDataResolver.cs` — 新增 CardBuff registry 與方法

**檔案**：`Assets/Scripts/GameModel/Effect/EffectDataResolver.cs`

在 `_characterBuffResolverRegistry` 下方新增（初始為空，待 Step 10 後填入）：

```csharp
private static readonly Dictionary<Type, ICardBuffEffectResolver> _cardBuffResolverRegistry = new()
{
    // 待 CardBuff 效果類型定義後填入
    // 例：[typeof(DamageCardBuffEffect)] = new DamageCardBuffEffectResolver(),
};
```

在 `ResolveCharacterBuffEffect()` 下方新增：

```csharp
#region CardBuffEffect
public static EffectCommandSet ResolveCardBuffEffect(
    TriggerContext context,
    ICardBuffEffect buffEffect)
{
    if (_cardBuffResolverRegistry.TryGetValue(buffEffect.GetType(), out var resolver))
        return resolver.Resolve(context, buffEffect);

    Debug.LogWarning($"[EffectDataResolver] 未知的 ICardBuffEffect 類型：{buffEffect.GetType().Name}，回傳空 CommandSet");
    return new EffectCommandSet(Array.Empty<IEffectCommand>());
}
#endregion
```

---

### Step 10：`GameplayManager.cs` — 填入 Ally + Enemy CardBuff loop body

**檔案**：`Assets/Scripts/GameModel/GameplayManager.cs`

將兩個 `foreach (var card in allyAllCards/enemyAllCards)` 空殼替換為：

```csharp
foreach (var card in allyAllCards)
{
    using var cardContext = _contextMgr.SetSelectedCard(card.Some());
    foreach (var buff in card.BuffManager.Buffs)
    {
        var cardBuffTrigger = new CardBuffTrigger(buff);
        var buffTriggerContext = new TriggerContext(this, cardBuffTrigger, timingAction);
        var conditionalEffectsOpt = _contextMgr.CardBuffLibrary.GetBuffEffects(buff.CardBuffDataID, timing);
        conditionalEffectsOpt.MatchSome(conditionalEffects =>
        {
            foreach (var conditionalEffect in conditionalEffects)
            {
                if (conditionalEffect.Conditions.All(c => c.Eval(buffTriggerContext)))
                {
                    var updateTimingAction = new UpdateTimingAction(GameTiming.TriggerBuffStart, buffTriggerContext.Action.Source);
                    triggerEvents.AddRange(UpdateReactorSessionAction(updateTimingAction));

                    var resolvedCommand = EffectDataResolver.ResolveCardBuffEffect(buffTriggerContext, conditionalEffect.Effect);
                    var applyEvts = EffectCommandExecutor.ApplyEffectCommands(buffTriggerContext, resolvedCommand);
                    triggerEvents.AddRange(applyEvts.Events);

                    var nextTriggerSource = new CardBuffSource(buff);
                    triggerEvents.AddRange(_TriggerTiming(GameTiming.TriggerBuffEnd, nextTriggerSource));
                }
            }
        });
    }
}
```

> `using var cardContext = _contextMgr.SetSelectedCard(card.Some())`  
> 利用 stack-based context 機制，確保 CardBuff 效果的 `ITargetCardValue` 等查詢能正確識別「是哪張牌的 buff 在觸發」。

Enemy 的 `foreach (var card in enemyAllCards)` 套用相同 body。

---

## 新增 CardBuff 效果類型（依需求擴充）

當設計師需要 CardBuff 的被動全域效果時，在 `CardBuffEffect.cs` 中新增具體類型，並在 `_cardBuffResolverRegistry` 登錄對應 Resolver：

```csharp
// 範例：CardBuff 被動傷害效果
[Serializable]
public class DamageCardBuffEffect : ICardBuffEffect
{
    public ITargetCharacterCollectionValue Targets;
    public IIntegerValue Value;
}

// 範例：CardBuff 被動抽牌效果
[Serializable]
public class DrawCardCardBuffEffect : ICardBuffEffect
{
    public IIntegerValue Count;
}
```

每新增一種效果類型，對應建立一個 `XxxCardBuffEffectResolver` 並在 registry 登錄，不需動其他檔案。

---

## 修改清單彙整

| # | 檔案 | 動作 |
|---|------|------|
| 1 | `GameModel/Action/ActionSource.cs` | 新增 `CharacterBuffSource` record |
| 2 | `GameModel/Effect/Resolvers/ICharacterBuffEffectResolver.cs` | **新建** |
| 3 | `GameModel/Effect/Resolvers/DamageCharacterBuffEffectResolver.cs` | **新建** |
| 4 | `GameModel/Effect/EffectDataResolver.cs` | 新增 `_characterBuffResolverRegistry` + `ResolveCharacterBuffEffect()` |
| 5 | `GameModel/GameplayManager.cs` | 填入 Ally + Enemy CharacterBuff loop body |
| 6 | `GameData/CardBuff/CardBuffData.cs` | 新增 `BuffEffects: Dictionary<GameTiming, ...>` |
| 7 | `GameData/CardBuff/CardBuffLibrary.cs` | 新增 `GetBuffEffects(buffId, GameTiming)` |
| 8 | `GameModel/Effect/Resolvers/ICardBuffEffectResolver.cs` | **新建** |
| 9 | `GameModel/Effect/EffectDataResolver.cs` | 新增 `_cardBuffResolverRegistry` + `ResolveCardBuffEffect()` |
| 10 | `GameModel/GameplayManager.cs` | 填入 Ally + Enemy CardBuff loop body |

**合計：4 個既有檔案修改 + 2 個 cs 修改（CardBuffData、CardBuffLibrary）+ 4 個新建檔案**

---

## 執行順序建議

```
Step 1 → Step 2 → Step 3 → Step 4 → Step 5   ← Phase 1 完整可編譯
    ↓
Step 6 → Step 7 → Step 8 → Step 9 → Step 10  ← Phase 2 完整可編譯
```

Phase 1 完成後可先 Recompile 確認 0 error，再進行 Phase 2。

---

> 最後更新：2026-05-12
