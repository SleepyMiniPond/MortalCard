using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CardBuffData
{
    [BoxGroup("Identification")]
    public string ID;

    [ShowInInspector]
    [BoxGroup("Effects")]
    public Dictionary<string, IReactionSessionData> Sessions = new();

    [Space(20)]
    [ShowInInspector]
    [BoxGroup("Effects")]
    public Dictionary<CardTriggeredTiming, ConditionalCardBuffEffect[]> Effects = new ();

    [ShowInInspector]
    [TitleGroup("Properties")]
    public List<ICardBuffPropertyData> PropertyDatas = new ();

    [TitleGroup("LifeTime")]
    public ICardBuffLifeTimeData LifeTimeData;
}


[Serializable]
public class AddCardBuffData
{
    [ValueDropdown("@DropdownHelper.CardBuffNames")]
    public string CardBuffId;

    public IIntegerValue Level;
}