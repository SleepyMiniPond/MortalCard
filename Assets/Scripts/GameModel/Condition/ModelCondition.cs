using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
 

public interface ICondition
{
    bool Eval(TriggerContext triggerContext);
}

public interface ICardBuffCondition : ICondition { }
public interface IPlayerBuffCondition : ICondition { }
public interface ICharacterBuffCondition : ICondition { }

[Serializable]
public class ConstCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    public bool Value;
    public bool Eval(TriggerContext triggerContext)
    {
        return Value;
    }
}

[Serializable]
public class AllCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [ShowInInspector]    
    [HorizontalGroup("1")]
    public List<ICondition> Conditions = new();

    public bool Eval(TriggerContext triggerContext)
    {
        return Conditions.All(condition => condition.Eval(triggerContext));
    }
}

[Serializable]
public class AnyCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<ICondition> Conditions = new();

    public bool Eval(TriggerContext triggerContext)
    {
        return Conditions.Any(condition => condition.Eval(triggerContext));
    }
}

[Serializable]
public class InverseCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [HorizontalGroup("1")]
    public ICondition Condition;

    public bool Eval(TriggerContext triggerContext)
    {
        return !Condition.Eval(triggerContext);
    }
}

[Serializable]
public class IsTriggeredOwnerTurnCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    public bool Eval(TriggerContext triggerContext)
    {

        return triggerContext.Triggered switch
        {
            CardPlayTrigger cardPlayTrigger => 
                triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .Combine(cardPlayTrigger.CardPlay.Card.Owner(triggerContext.Model))
                    .Map(t => t.Item1 == t.Item2)
                    .ValueOr(false),
            CardTrigger cardTrigger => 
                triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .Combine(cardTrigger.Card.Owner(triggerContext.Model))
                    .Map(t => t.Item1 == t.Item2)
                    .ValueOr(false),
            CardBuffTrigger cardBuffTrigger => 
                triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .Combine(cardBuffTrigger.Buff.Owner(triggerContext.Model))
                    .Map(t => t.Item1 == t.Item2)
                    .ValueOr(false),
            PlayerBuffTrigger playerBuffTrigger => 
                triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .Combine(playerBuffTrigger.Buff.Owner(triggerContext.Model))
                    .Map(t => t.Item1 == t.Item2)
                    .ValueOr(false),
            CharacterBuffTrigger characterBuffTrigger => 
                triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .Combine(characterBuffTrigger.Buff.Owner(triggerContext.Model))
                    .Map(t => t.Item1 == t.Item2)
                    .ValueOr(false),
            PlayerTrigger playerTrigger => 
                triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .Map(currentPlayer => currentPlayer == playerTrigger.Player)
                    .ValueOr(false),
            _ => false
        };
    }
}

[Serializable]
public class IntegerCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [HorizontalGroup("1")]
    public IIntegerValue Value;

    [ShowInInspector]
    [HorizontalGroup("2")]
    public List<IIntegerValueCondition> Conditions = new ();

    public bool Eval(TriggerContext triggerContext)
    {
        var value = Value.Eval(triggerContext);
        return Conditions.All(c => c.Eval(triggerContext, value));
    }
}

[Serializable]
public class CardCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [HorizontalGroup("1")]
    public ITargetCardValue Card;
    
    [ShowInInspector]
    [HorizontalGroup("2")]
    public List<ICardValueCondition> Conditions = new ();

    public bool Eval(TriggerContext triggerContext)
    {
        var cardOpt = Card.Eval(triggerContext);
        return cardOpt.Match(
            card => Conditions.All(c => c.Eval(triggerContext, card)),
            ()   => false
        );
    }
}
[SerializeField]
public class CardPlayCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<ICardPlayValueCondition> Conditions = new();

    public bool Eval(TriggerContext triggerContext)
    {
        return triggerContext.Action.Source switch
        {
            CardPlaySource cardPlaySource =>
                Conditions.All(c => c.Eval(triggerContext, cardPlaySource)),
            CardPlayResultSource cardPlayResultSource =>
                Conditions.All(c => c.Eval(triggerContext, cardPlayResultSource.CardPlaySource)),
            _ => false
        };
    }
}

[SerializeField]
public class CardPlayResultCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [ShowInInspector]
    [HorizontalGroup("1")]
    public List<ICardPlayResultValueCondition> Conditions = new();

    public bool Eval(TriggerContext triggerContext)
    {
        return triggerContext.Action.Source switch
        {
            CardPlayResultSource cardPlayResultSource =>
                Conditions.All(c => c.Eval(triggerContext, cardPlayResultSource)),
            _ => false
        };
    }
}

[Serializable]
public class PlayerCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [HorizontalGroup("1")]
    public ITargetPlayerValue Player;

    [ShowInInspector]
    [HorizontalGroup("2")]
    public List<IPlayerValueCondition> Conditions = new ();

    public bool Eval(TriggerContext triggerContext)
    {
        var playerOpt = Player.Eval(triggerContext);
        return playerOpt.Match(
            player => Conditions.All(c => c.Eval(triggerContext, player)),
            ()     => false
        );
    }
}

[Serializable]
public class CharacterCondition : ICardBuffCondition, IPlayerBuffCondition, ICharacterBuffCondition
{
    [HorizontalGroup("1")]
    public ITargetCharacterValue Character;
    
    [ShowInInspector]
    [HorizontalGroup("2")]
    public List<ICharacterValueCondition> Conditions = new ();

    public bool Eval(TriggerContext triggerContext)
    {
        var characterOpt = Character.Eval(triggerContext);
        return characterOpt.Match(
            character => Conditions.All(c => c.Eval(triggerContext, character)),
            ()        => false
        );
    }
}

[Serializable]
public class PlayerBuffCondition : IPlayerBuffCondition
{
    [HorizontalGroup("1")]
    public ITargetPlayerBuffValue PlayerBuff;

    [ShowInInspector]
    [HorizontalGroup("2")]
    public List<IPlayerBuffValueCondition> Conditions = new ();
    public bool Eval(TriggerContext triggerContext)
    {
        var playerBuffOpt = PlayerBuff.Eval(triggerContext);
        return playerBuffOpt.Match(
            playerBuff => Conditions.All(c => c.Eval(triggerContext, playerBuff)),
            ()         => false
        );
    }
}