using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "AllCardScriptable", menuName = "Scriptable Objects/AllCardScriptable")]
public class AllCardScriptable : SerializedScriptableObject
{
    public CardDataScriptable[] AllCardData = new CardDataScriptable[0];
}