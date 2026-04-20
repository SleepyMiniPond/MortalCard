using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "AllPlayerBuffScriptable", menuName = "Scriptable Objects/AllPlayerBuffScriptable")]
public class AllPlayerBuffScriptable : SerializedScriptableObject
{
    public PlayerBuffDataScriptable[] AllBuffData = new PlayerBuffDataScriptable[0];
}
