using System.Collections.Generic;
using UnityEngine;

public record GameStageSetting(
    string StageID,
    int RandomSeed,
    AllyInstance Ally,
    EnemyData Enemy);
