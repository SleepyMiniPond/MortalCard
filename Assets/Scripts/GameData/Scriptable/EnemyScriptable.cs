using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

    [CreateAssetMenu(fileName = "EnemyScriptable", menuName = "Scriptable Objects/EnemyScriptable")]
    public class EnemyScriptable : SerializedScriptableObject
    {
        public EnemyData Enemy = new();
    }
}
