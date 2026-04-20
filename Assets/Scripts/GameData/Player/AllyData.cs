using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class AllyData
{
    [BoxGroup("AllyOnly")]
    public string GameMode;
    [BoxGroup("AllyOnly")]
    [Range(0, 10)]
    public int InitialDisposition;

    public  PlayerData PlayerData;
}