using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

public interface ICardPlayResultValueCondition
{
    bool Eval(TriggerContext triggerContext, CardPlayResultSource cardPlayResultSource);
}

[Serializable]
public class CardPlayEffectResultCondition : ICardPlayResultValueCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<IEffectResultCondition> Conditions = new();

    public bool Eval(TriggerContext triggerContext, CardPlayResultSource cardPlayResultSource)
    {
        return cardPlayResultSource.EffectResults.All(effectResult => Conditions.All(c => c.Eval(triggerContext, effectResult)));
    }
}

public interface IEffectResultCondition
{
    bool Eval(TriggerContext triggerContext, IEffectResultAction effectResult);
}

[Serializable]
public class EffectResultTypeCondition : IEffectResultCondition
{
    public enum EffectType
    {
        Damage,
        Heal,
        DrawCard,
        Buff,
        Energy
    }

    [ShowInInspector]
    public EffectType[] EffectTypes = new EffectType[0];

    [ShowInInspector]
    public SetConditionType Condition;


    public bool Eval(TriggerContext triggerContext, IEffectResultAction effectResult)
    {
        return Condition.Eval(EffectTypes, type => _Eval(type, effectResult));
    }

    private bool _Eval(EffectType type, IEffectResultAction effectResult)
    {
        return type switch
        {
            EffectType.Damage => effectResult is DamageResultAction,
            EffectType.Heal => effectResult is HealResultAction,
            EffectType.DrawCard => effectResult is DrawCardResultAction,
            EffectType.Buff => effectResult is AddPlayerBuffResultAction or
                RemovePlayerBuffResultAction or
                AddCardBuffResultAction or
                RemoveCardBuffResultAction,
            EffectType.Energy => effectResult is GainEnergyResultAction or LoseEnergyResultAction,
            _ => false
        };
    }
}

[Serializable]
public class DamageResultCondition : IEffectResultCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<IDamageResultCondition> DamageConditions = new();

    public SetConditionType SetCondition;

    public bool Eval(TriggerContext triggerContext, IEffectResultAction effectResult)
    {
        return effectResult switch
        {
            DamageResultAction damageResult =>
                SetCondition.Eval(
                    DamageConditions,
                    damageCondtion => damageCondtion.Eval(triggerContext, damageResult)
                ),
            _ => false
        };
    }
}

public interface IDamageResultCondition
{
    bool Eval(TriggerContext triggerContext, DamageResultAction damageResult);
}

[Serializable]
public class DamageResultPlayerTargetCondition : IDamageResultCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<IPlayerValueCondition> TargetPlayerConditions = new ();

    public bool Eval(TriggerContext triggerContext, DamageResultAction damageResult)
    {
        return damageResult.Target switch
        {
            PlayerTarget playerTarget =>
                TargetPlayerConditions.All(condition => condition.Eval(triggerContext, playerTarget.Player)),
            PlayerAndCardTarget playerAndCardTarget =>
                TargetPlayerConditions.All(condition => condition.Eval(triggerContext, playerAndCardTarget.Player)),
            _ => false
        };
    }
}

[Serializable]
public class DamageResultCharacterTargetCondition : IDamageResultCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<ICharacterValueCondition> TargetCharacterConditions = new ();

    public bool Eval(TriggerContext triggerContext, DamageResultAction damageResult)
    {
        return damageResult.Target switch
        {
            CharacterTarget characterTarget =>
                TargetCharacterConditions.All(condition => condition.Eval(triggerContext, characterTarget.Character)),
            _ => false
        };
    }
}
