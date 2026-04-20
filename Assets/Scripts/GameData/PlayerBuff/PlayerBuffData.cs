using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerBuffData
{
    [TitleGroup("BasicData")]
    public string ID;

    [TitleGroup("BasicData")]
    public int MaxLevel;
 
    [ShowInInspector]
    [BoxGroup("Effects")]
    public Dictionary<string, IReactionSessionData> Sessions = new();
    
    [Space(20)]
    [ShowInInspector]
    [BoxGroup("Effects")]
    public Dictionary<GameTiming, ConditionalPlayerBuffEffect[]> BuffEffects = new();  

    [ShowInInspector]
    [BoxGroup("Properties")]
    public List<IPlayerBuffPropertyData> PropertyDatas = new();

    [BoxGroup("LifeTime")]
    public IPlayerBuffLifeTimeData LifeTimeData;
}