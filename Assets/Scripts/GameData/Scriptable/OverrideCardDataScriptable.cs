using UnityEngine;

namespace MortalGame.GameData
{
    [CreateAssetMenu(
        fileName = "OverrideCardData",
        menuName = "Scriptable Objects/Card/Override Card Data")]
    public sealed class OverrideCardDataScriptable : CardDataScriptableBase
    {
        public OverrideCardData Data = new();

        public override CardData CardData => Data;
    }
}
