using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDataScriptable", menuName = "Scriptable Objects/CardDataScriptable")]
public class CardDataScriptable : SerializedScriptableObject
{
    public CardData Data = new();
}