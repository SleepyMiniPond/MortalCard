using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

public interface ITargetCharacterValue
{
    Option<ICharacterEntity> Eval(TriggerContext triggerContext);
}

[Serializable]
public class NoneCharacter : ITargetCharacterValue
{
    public Option<ICharacterEntity> Eval(TriggerContext triggerContext)
    {
        return Option.None<ICharacterEntity>();
    }
}
[Serializable]
public class MainCharacterOfPlayer : ITargetCharacterValue
{
    [HorizontalGroup("1")]
    public ITargetPlayerValue Player;

    public Option<ICharacterEntity> Eval(TriggerContext triggerContext)
    {
        return Player.Eval(triggerContext).Map(player => player.MainCharacter);
    }
}
[Serializable]
public class SelectedCharacter : ITargetCharacterValue
{
    public Option<ICharacterEntity> Eval(TriggerContext triggerContext)
    {
        return triggerContext.Model.GetCharacter(triggerContext.Model.ContextManager.Context.SelectedCharacter);
    }
}

public interface ITargetCharacterCollectionValue
{
    IReadOnlyCollection<ICharacterEntity> Eval(TriggerContext triggerContext);
}

[Serializable]
public class NoneCharacters : ITargetCharacterCollectionValue
{
    public IReadOnlyCollection<ICharacterEntity> Eval(TriggerContext triggerContext)
    {
        return  Array.Empty<ICharacterEntity>();
    }
}
[Serializable]
public class SingleCharacterCollection : ITargetCharacterCollectionValue
{
    [HorizontalGroup("1")]
    public ITargetCharacterValue Target;

    public IReadOnlyCollection<ICharacterEntity> Eval(TriggerContext triggerContext)
    {
        return Target.Eval(triggerContext).ToEnumerable().ToList();
    }
}