using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

public interface IReactionSessionValueCondition
{
    bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity);
}

[Serializable]
public class ReactorSessionUpdatedCondition : IReactionSessionValueCondition
{
    public bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity)
    {
        return sessionEntity.IsSessionValueUpdated;
    }
}
[Serializable]
public class ReactionSessionValueBooleanCondition : IReactionSessionValueCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<IBooleanValueCondition> Conditions = new();

    public bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity)
    {
        return sessionEntity
            .BooleanValue
            .Match(
                value => Conditions.All(condition => condition.Eval(triggerContext, value)),
                () => false);
    }
}

[Serializable]
public class ReactionSessionValueIntegerCondition : IReactionSessionValueCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<IIntegerValueCondition> Conditions = new();

    public bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity)
    {
        return sessionEntity
            .IntegerValue
            .Match(
                value => Conditions.All(condition => condition.Eval(triggerContext, value)),
                () => false);
    }
}