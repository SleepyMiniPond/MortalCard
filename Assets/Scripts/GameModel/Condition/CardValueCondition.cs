using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;

public interface ICardValueCondition
{
    bool Eval(TriggerContext triggerContext, ICardEntity card);
}

[Serializable]
public class CardEqualCondition : ICardValueCondition
{
    [HorizontalGroup("1")]
    public ITargetCardValue CompareCard;

    public bool Eval(TriggerContext triggerContext, ICardEntity card)
    {
        return CompareCard
            .Eval(triggerContext)
            .Match(
                compareCard => card.Identity == compareCard.Identity,
                () => false);
    }
}
[Serializable]
public class CardTypesCondition : ICardValueCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<CardType> CardTypes = new();

    public SetConditionType Condition;

    public bool Eval(TriggerContext triggerContext, ICardEntity card)
    {
        return Condition.Eval(CardTypes, type => type == card.Type);
    }
}