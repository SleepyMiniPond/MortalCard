using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

public interface ITargetPlayerValue
{
    Option<IPlayerEntity> Eval(TriggerContext triggerContext);
}

[Serializable]
public class NonePlayer : ITargetPlayerValue
{
    public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        return Option.None<IPlayerEntity>();
    }
}
[Serializable]
public class CurrentPlayer : ITargetPlayerValue
{
    public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        return triggerContext.Model.GameStatus.CurrentPlayer.Value;
    }
}
[Serializable]
public class OppositePlayer : ITargetPlayerValue
{    
    [HorizontalGroup("1")]
    public ITargetPlayerValue Reference;

    public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        var referenceOpt = Reference.Eval(triggerContext);
        return
            referenceOpt.FlatMap(reference => 
                reference.Faction == Faction.Ally ? (triggerContext.Model.GameStatus.Enemy as IPlayerEntity).Some() :
                reference.Faction == Faction.Enemy ? (triggerContext.Model.GameStatus.Ally as IPlayerEntity).Some() :
                Option.None<IPlayerEntity>());
    }
}
[Serializable]
public class CardOwner : ITargetPlayerValue
{
    [HorizontalGroup("1")]
    public ITargetCardValue Card;

    public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        var cardOpt = Card.Eval(triggerContext);
        return cardOpt.FlatMap(card => card.Owner(triggerContext.Model));
    }
}
[Serializable]
public class PlayerBuffContentPlayer : ITargetPlayerValue
{
    public enum PlayerType
    {
        Owner,
        Caster,
    }

    [HorizontalGroup("1")]
    public ITargetPlayerBuffValue PlayerBuff;
    
    public PlayerType Type;

    public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        var playerBuffOpt = PlayerBuff.Eval(triggerContext);
        return playerBuffOpt.FlatMap(playerBuff => Type switch
        {
            PlayerType.Owner => playerBuff.Owner(triggerContext.Model),
            PlayerType.Caster => playerBuff.Caster,
            _ => Option.None<IPlayerEntity>()
        });
    }
}
[SerializeField]
public class CharacterOwner : ITargetPlayerValue
{
    [HorizontalGroup("1")]
    public ITargetCharacterValue Character;

    public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        var characterOpt = Character.Eval(triggerContext);
        return characterOpt.FlatMap(character => character.Owner(triggerContext.Model));
    }
}
[Serializable]
public class SelectedPlayer : ITargetPlayerValue
{
    public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        return triggerContext.Model.GameStatus.GetPlayer(triggerContext.Model.ContextManager.Context.SelectedPlayer);
    }
}

public interface ITargetPlayerCollectionValue
{
    IReadOnlyCollection<IPlayerEntity> Eval(TriggerContext triggerContext);
}
[Serializable]
public class NonePlayers : ITargetPlayerCollectionValue
{
    public IReadOnlyCollection<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        return Array.Empty<IPlayerEntity>();
    }
}
[Serializable]
public class SinglePlayerCollection : ITargetPlayerCollectionValue
{
    [HorizontalGroup("1")]
    public ITargetPlayerValue Target;

    public IReadOnlyCollection<IPlayerEntity> Eval(TriggerContext triggerContext)
    {
        return Target.Eval(triggerContext).ToEnumerable().ToList();
    }
}