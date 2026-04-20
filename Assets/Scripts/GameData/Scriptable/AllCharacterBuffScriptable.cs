using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "AllCharacterBuffScriptable", menuName = "Scriptable Objects/AllCharacterBuffScriptable")]
public class AllCharacterBuffScriptable : SerializedScriptableObject
{
    public CharacterBuffDataScriptable[] AllBuffData = new CharacterBuffDataScriptable[0];
}
