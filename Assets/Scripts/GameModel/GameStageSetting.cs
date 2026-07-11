using System.Collections.Generic;
using MortalGame.GameData;
using UnityEngine;

namespace MortalGame.GameModel
{

    public record GameStageSetting(
        string StageID,
        int RandomSeed,
        AllyInstance Ally,
        EnemyData Enemy);

}
