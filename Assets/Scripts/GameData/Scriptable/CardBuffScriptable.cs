using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "CardBuffScriptable", menuName = "Scriptable Objects/CardBuffScriptable")]
public class CardBuffScriptable : SerializedScriptableObject
{
    public CardBuffData Data = new();
}
