using UnityEngine;

namespace MortalGame.GameData
{
    /// <summary>集中內容查詢的診斷格式與等級，不決定查詢失敗後的回傳值。</summary>
    internal static class ContentDiagnosticLogger
    {
        public static void LogMissingCard(string cardId) => _LogMissing("Card", cardId);
        public static void LogMissingCardBuff(string buffId) => _LogMissing("CardBuff", buffId);
        public static void LogMissingPlayerBuff(string buffId) => _LogMissing("PlayerBuff", buffId);
        public static void LogMissingCharacterBuff(string buffId) => _LogMissing("CharacterBuff", buffId);

        public static void LogNotStandardCard(string cardId) => _LogCardTypeMismatch("Standard", cardId);
        public static void LogNotOverrideCard(string cardId) => _LogCardTypeMismatch("Override", cardId);

        public static void LogMissingMainSelect(string cardId) =>
            Debug.LogError($"MainSelect is null. cardId={cardId}");

        public static void LogMissingMainSelectable(string cardId) =>
            Debug.LogError($"MainSelectable is null. cardId={cardId}");

        private static void _LogMissing(string kind, string identity) =>
            Debug.LogError($"{kind} ID[{identity}] not found in library.");

        private static void _LogCardTypeMismatch(string kind, string cardId) =>
            Debug.LogError($"Card ID[{cardId}] 不是 {kind} CardData。");
    }
}
