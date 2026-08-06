using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

    [CreateAssetMenu(fileName = "AllCardScriptable", menuName = "Scriptable Objects/AllCardScriptable")]
    public class AllCardScriptable : SerializedScriptableObject
    {
        public CardDataScriptableBase[] AllCardData = new CardDataScriptableBase[0];
    }
}
