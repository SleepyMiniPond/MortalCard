using System;
using System.Collections.Generic;
using System.Linq;
using Optional.Collections;
using Sirenix.OdinInspector;

public interface IIntegerValue
{
    int Eval(TriggerContext triggerContext);
}

[Serializable]
public class ConstInteger : IIntegerValue
{
    public int Value;

    public int Eval(TriggerContext triggerContext)
    {
        return Value;
    }
}

[Serializable]
public class ArithmeticInteger : IIntegerValue
{
    public ArithmeticType Operation;
    public IIntegerValue Left;
    public IIntegerValue Right;

    public int Eval(TriggerContext triggerContext)
    {
        var leftValue = Left.Eval(triggerContext);
        var rightValue = Right.Eval(triggerContext);

        return Operation switch
        {
            ArithmeticType.Add => leftValue + rightValue,
            ArithmeticType.Multiply => leftValue * rightValue,
            _ => 0
        };
    }
}

[Serializable]
public class CardIntegerProperty : IIntegerValue
{   
    public enum CardIntegerValueType
    {
        Power,
        Cost,
    }

    [HorizontalGroup("1")]
    public ITargetCardValue Card;
    public CardIntegerValueType Property;

    public int Eval(TriggerContext triggerContext)
    {
        return Card
            .Eval(triggerContext)
            .Map(
                card => Property switch
                {
                    // TODO: Apply EffectAttribute.Power adjust
                    CardIntegerValueType.Power => GameFormula.CardPower(triggerContext, card),
                    // TODO: Apply EffectAttribute.Cost adjust
                    CardIntegerValueType.Cost => GameFormula.CardCost(triggerContext, card),
                    _ => 0
                })
            .ValueOr(0);
    }
}

[Serializable]
public class PlayerIntegerProperty : IIntegerValue
{
    public enum PlayerIntegerValueType
    {
        MaxEnergy,
        CurrentEnergy,
    }

    [HorizontalGroup("1")]
    public ITargetPlayerValue Player;
    public PlayerIntegerValueType Property;

    public int Eval(TriggerContext triggerContext)
    {
        var playerOpt = Player.Eval(triggerContext);
        return Player
            .Eval(triggerContext)
            .Map(player => 
                Property switch
                {
                    PlayerIntegerValueType.MaxEnergy => player.MaxEnergy,
                    PlayerIntegerValueType.CurrentEnergy => player.CurrentEnergy,
                    _ => 0
                })
            .ValueOr(0);
    }
}

[Serializable]
public class CardBuffIntegerProperty : IIntegerValue
{
    public enum CardBuffIntegerValueType
    {
        Level
    }

    [HorizontalGroup("1")]
    public ITargetCardBuffValue CardBuff;
    public CardBuffIntegerValueType Property;

    public int Eval(TriggerContext triggerContext)
    {
        var cardBuffOpt = CardBuff.Eval(triggerContext);
        return cardBuffOpt
            .Map(
                cardBuff => Property switch
                {
                    CardBuffIntegerValueType.Level => cardBuff.Level,
                    _ => 0
                })
            .ValueOr(0);
    }
}

[Serializable]
public class PlayerBuffIntegerProperty : IIntegerValue
{
    public enum PlayerBuffIntegerValueType
    {
        Level,
    }

    [HorizontalGroup("1")]
    public ITargetPlayerBuffValue PlayerBuff;
    public PlayerBuffIntegerValueType Property;

    public int Eval(TriggerContext triggerContext)
    {
        return PlayerBuff
            .Eval(triggerContext)
            .Map(
                playerBuff => Property switch
                {
                    PlayerBuffIntegerValueType.Level => playerBuff.Level,
                    _ => 0
                })
            .ValueOr(0);
    }
}

[Serializable]
public class PlayerBuffSessionInteger : IIntegerValue
{
    [HorizontalGroup("1")]
    public ITargetPlayerBuffValue PlayerBuff;
    public string SessionIntegerId;

    public int Eval(TriggerContext triggerContext)
    {
        return PlayerBuff
            .Eval(triggerContext)
            .FlatMap(playerBuff => playerBuff.GetSessionInteger(SessionIntegerId))
            .ValueOr(0);
    }
}

[Serializable]
public class ConditionalValue : IIntegerValue
{
    [Serializable]
    public class ConditionPair
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<IPlayerBuffCondition> Conditions = new ();

        [HorizontalGroup("2")]
        public IIntegerValue Value;
    }

    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<ConditionPair> Pairs = new ();

    public int Eval(TriggerContext triggerContext)
    {
        foreach (var pair in Pairs)
        {
            if (pair.Conditions.All(condition => condition.Eval(triggerContext)))
            {
                return pair.Value.Eval(triggerContext);
            }
        }
        return 0;
    }
}