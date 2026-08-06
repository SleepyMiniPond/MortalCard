using Sirenix.OdinInspector;

namespace MortalGame.GameData
{
    /// <summary>
    /// 統一一般卡片與外部覆寫卡片的資產查詢介面。
    /// </summary>
    public abstract class CardDataScriptableBase : SerializedScriptableObject
    {
        public abstract CardData CardData { get; }
    }
}
