using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ConditionalPlayerBuffEffect
{
    [ShowInInspector]    
    [HorizontalGroup("1")]
    public List<IPlayerBuffCondition> Conditions = new ();

    [Space(20)]
    [HorizontalGroup("2")]
    public IPlayerBuffEffect Effect;
}

[Serializable]
public class EffectiveDamagePlayerBuffEffect : IPlayerBuffEffect
{
    [HorizontalGroup("1")]
    public ITargetCharacterCollectionValue Targets;
    
    [HorizontalGroup("2")]
    public IIntegerValue Value;
}
[Serializable]
public class AdditionalDamagePlayerBuffEffect : IPlayerBuffEffect
{
    [HorizontalGroup("1")]
    public ITargetCharacterCollectionValue Targets;
    
    [HorizontalGroup("2")]
    public IIntegerValue Value;
}

[Serializable]
public class CardPlayEffectAttributeAdditionPlayerBuffEffect : IPlayerBuffEffect
{
    [HorizontalGroup("1")]
    public EffectAttributeAdditionType Type;

    [HorizontalGroup("2")]
    public IIntegerValue Value;
}

[Serializable]
public class AddCardBuffPlayerBuffEffect : IPlayerBuffEffect
{
    [HorizontalGroup("1")]
    public ITargetCardCollectionValue Targets;

    [ShowInInspector]
    [HorizontalGroup("2")]
    public List<AddCardBuffData> AddCardBuffDatas = new();
}
[Serializable]
public class RemoveCardBuffPlayerBuffEffect : IPlayerBuffEffect
{
    [HorizontalGroup("1")]
    public ITargetCardCollectionValue Targets;

    [ValueDropdown("@DropdownHelper.CardBuffNames")]
    [HorizontalGroup("2")]
    public string BuffId;
}

