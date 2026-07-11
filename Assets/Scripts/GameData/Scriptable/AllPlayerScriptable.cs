using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

    [CreateAssetMenu(fileName = "AllPlayerScriptable", menuName = "Scriptable Objects/AllPlayerScriptable")]
    public class AllPlayerScriptable : SerializedScriptableObject
    {
        public AllyScriptable AllyObject;

        public EnemyScriptable[] EnemyObjects = new EnemyScriptable[0];
    }
}
