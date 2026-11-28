using System.Collections.Generic;
using UnityEngine;

namespace MortalGame.GameData
{

    public class CardLibrary
    {
        private readonly Dictionary<string, CardData> _cards;

        public CardLibrary(IReadOnlyDictionary<string, CardData> cards)
        {
            _cards = new Dictionary<string, CardData>(cards);
        }

        public CardData GetCardData(string cardId)
        {
            if (!_cards.ContainsKey(cardId))
            {
                ContentDiagnosticLogger.LogMissingCard(cardId);
                return null;
            }

            return _cards[cardId];
        }

        public StandardCardData GetStandardCardData(string cardId)
        {
            var cardData = GetCardData(cardId);
            if (cardData is StandardCardData standardCardData)
                return standardCardData;

            ContentDiagnosticLogger.LogNotStandardCard(cardId);
            return null;
        }

        public OverrideCardData GetOverrideCardData(string cardId)
        {
            var cardData = GetCardData(cardId);
            if (cardData is OverrideCardData overrideCardData)
                return overrideCardData;

            ContentDiagnosticLogger.LogNotOverrideCard(cardId);
            return null;
        }
    }
}
