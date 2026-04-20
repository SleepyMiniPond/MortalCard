using System;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IIntegerValueCondition
{
    bool Eval(TriggerContext triggerContext, int value);
}

[Serializable]
public class IntegerCompare : IIntegerValueCondition
{
    public ArithmeticConditionType Arithmetic;

    [HorizontalGroup("1")]
    public IIntegerValue CompareValue;

    public bool Eval(TriggerContext triggerContext, int value)
    {
        var compareValue = CompareValue.Eval(triggerContext);
        return Arithmetic switch
        {
            ArithmeticConditionType.Equal               => value == compareValue,
            ArithmeticConditionType.NotEqual            => value != compareValue,
            ArithmeticConditionType.GreaterThan         => value > compareValue,
            ArithmeticConditionType.LessThan            => value < compareValue,
            ArithmeticConditionType.GreaterThanOrEqual  => value >= compareValue,
            ArithmeticConditionType.LessThanOrEqual     => value <= compareValue,
            _ => false
        };
    }
}