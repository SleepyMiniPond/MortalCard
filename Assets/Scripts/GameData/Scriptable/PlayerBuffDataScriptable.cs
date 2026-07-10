using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

[CreateAssetMenu(fileName = "PlayerBuffDataScriptable", menuName = "Scriptable Objects/PlayerBuffDataScriptable")]
public class PlayerBuffDataScriptable : SerializedScriptableObject
{
    public PlayerBuffData Data = new();
}
}
