using System.Collections.Generic;
using MortalGame.GameData;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MortalGame.GameData
{

[CreateAssetMenu(fileName = "CardDataScriptable", menuName = "Scriptable Objects/CardDataScriptable")]
public class CardDataScriptable : SerializedScriptableObject
{
    public CardData Data = new();
}
}
