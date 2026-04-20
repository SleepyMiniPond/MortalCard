using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyData
{
    [BoxGroup("EnemyOnly")]
    public string EnemyID;
    [BoxGroup("EnemyOnly")]
    public int Level;
    [BoxGroup("EnemyOnly")]
    public int SelectedCardMaxCount;
    [BoxGroup("EnemyOnly")]
    public int TurnStartDrawCardCount;
    [BoxGroup("EnemyOnly")]
    public int EnergyRecoverPoint;

    public PlayerData PlayerData;
}