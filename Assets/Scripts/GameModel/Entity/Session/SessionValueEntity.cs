using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Optional;

public interface ISessionValueEntity
{
    ISessionValueEntity Clone();

    bool Update(TriggerContext triggerContext);
}

public class SessionBooleanEntity : ISessionValueEntity
{
    public bool Value;
    private readonly List<SessionBoolean.TimingRule> _updateRules;

    public SessionBooleanEntity(
        bool initialValue,
        List<SessionBoolean.TimingRule> updateRules)
    {
        Value = initialValue;
        _updateRules = updateRules;
    }

    public ISessionValueEntity Clone()
    {
        return new SessionBooleanEntity(Value, _updateRules);
    }

    private bool _UpdateRules(
        IReadOnlyCollection<ConditionBooleanUpdateRule> rules, 
        TriggerContext triggerContext)
    {
        foreach (var rule in rules)
        {
            if (rule.Conditions.All(condition => condition.Eval(triggerContext)))
            {
                var newVal = rule.NewValue.Eval(triggerContext);
                Value = rule.Operation switch
                {
                    ConditionBooleanUpdateRule.UpdateType.AndOrigin => Value && newVal,
                    ConditionBooleanUpdateRule.UpdateType.OrOrigin => Value || newVal,
                    ConditionBooleanUpdateRule.UpdateType.Overwrite => newVal,
                    _ => Value
                };
                return true;
            }
            break;
        }
        return false;
    }

    public bool Update(TriggerContext triggerContext)
    {
        var isChanged = false;
        foreach (var rule in _updateRules.Where(r => r.Timing == triggerContext.Action.Timing))
        {
            isChanged |= _UpdateRules(rule.Rules, triggerContext);
        }
        return isChanged;
    }
}

public class SessionIntegerEntity : ISessionValueEntity
{
    public int Value;
    private readonly List<SessionInteger.TimingRule> _updateRules;

    public SessionIntegerEntity(
        int initialValue,
        List<SessionInteger.TimingRule> updateRules)
    {
        Value = initialValue;
        _updateRules = updateRules;
    }

    public ISessionValueEntity Clone()
    {
        return new SessionIntegerEntity(Value, _updateRules);
    }

    private bool _UpdateRules(
        IReadOnlyCollection<ConditionIntegerUpdateRule> rules,
        TriggerContext triggerContext)
    {
        foreach (var rule in rules)
        {
            if (rule.Conditions.All(condition => condition.Eval(triggerContext)))
            {
                var newVal = rule.NewValue.Eval(triggerContext);
                Value = rule.Operation switch
                {
                    ConditionIntegerUpdateRule.UpdateType.AddOrigin => Value + newVal,
                    ConditionIntegerUpdateRule.UpdateType.Overwrite => newVal,
                    _ => Value
                };
                return true;
            }
        }
        return false;
    }

    public bool Update(TriggerContext triggerContext)
    {
        var isChanged = false;

        foreach (var rule in _updateRules.Where(r => r.Timing == triggerContext.Action.Timing))
        {
            isChanged |= _UpdateRules(rule.Rules, triggerContext);
        }
        return isChanged;
    }
}
