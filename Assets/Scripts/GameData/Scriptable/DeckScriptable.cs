using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

[CreateAssetMenu(fileName = "DeckScriptable", menuName = "Scriptable Objects/DeckScriptable")]
public class DeckScriptable : SerializedScriptableObject
{
    public CardDataScriptable[] Cards = new CardDataScriptable[0];
}
}
