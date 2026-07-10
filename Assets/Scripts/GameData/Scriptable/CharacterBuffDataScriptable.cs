using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

[CreateAssetMenu(fileName = "CharacterBuffDataScriptable", menuName = "Scriptable Objects/CharacterBuffDataScriptable")]
public class CharacterBuffDataScriptable : SerializedScriptableObject
{
    public CharacterBuffData Data = new();
}
}
