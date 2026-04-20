using System;
using UnityEngine;

public interface IBooleanValueCondition
{
    bool Eval(TriggerContext triggerContext, bool value);
}

[Serializable]
public class IsTrueCondition : IBooleanValueCondition
{
    public bool Eval(TriggerContext triggerContext, bool value)
    {
        return value;
    }
}

[Serializable]
public class IsFalseCondition : IBooleanValueCondition
{
    public bool Eval(TriggerContext triggerContext, bool value)
    {
        return !value;
    }
}

[Serializable]
public class IsEqualCondition : IBooleanValueCondition
{
    public IBooleanValue Boolean;

    public bool Eval(TriggerContext triggerContext, bool value)
    {
        return value == Boolean.Eval(triggerContext);
    }
}