using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{
    [CreateAssetMenu(
        fileName = "StandardCardData",
        menuName = "Scriptable Objects/Card/Standard Card Data")]
    public sealed class StandardCardDataScriptable : CardDataScriptableBase
    {
        public StandardCardData Data = new();

        public override CardData CardData => Data;
    }
}
