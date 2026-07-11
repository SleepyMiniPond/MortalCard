using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

    [CreateAssetMenu(fileName = "PlayerScriptable", menuName = "Scriptable Objects/PlayerScriptable")]
    public class AllyScriptable : SerializedScriptableObject
    {
        public AllyData Ally = new();
    }
}
