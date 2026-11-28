using System;
using UnityEngine;

namespace MortalGame.GameModel
{
    internal static class EffectResolverDiagnosticLogger
    {
        public static void LogUnknownEffect(Type contractType, Type actualType) =>
            Debug.LogWarning($"[EffectDataResolver] 未知的 {contractType.Name} 類型：{actualType.Name}，回傳空 CommandSet");
    }

    /// <summary>以必要資料組裝出牌追蹤路徑，保留既有預算診斷格式。</summary>
    internal static class CardPlayTrace
    {
        public static string ActiveCard(Guid cardIdentity) => $"ActiveCard:{cardIdentity}";
        public static string RequestedCard(Guid cardIdentity, Type sourceType) =>
            $"CardPlayRequest:{cardIdentity};Source:{sourceType.Name}";
        public static string RepeatedEffect(Guid cardIdentity, int repeatIndex) =>
            $"CardEffectRepeat:{cardIdentity}:{repeatIndex}";
    }
}
