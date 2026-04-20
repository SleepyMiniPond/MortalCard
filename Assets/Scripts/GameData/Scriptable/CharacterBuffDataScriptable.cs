using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterBuffDataScriptable", menuName = "Scriptable Objects/CharacterBuffDataScriptable")]
public class CharacterBuffDataScriptable : SerializedScriptableObject
{
    public CharacterBuffData Data = new();
}
