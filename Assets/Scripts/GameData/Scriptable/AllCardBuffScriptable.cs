using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

[CreateAssetMenu(fileName = "AllCardBuffScriptable", menuName = "Scriptable Objects/AllCardBuffScriptable")]
public class AllCardBuffScriptable : SerializedScriptableObject
{
    public CardBuffScriptable[] AllCardBuffData = new CardBuffScriptable[0];
}
}
