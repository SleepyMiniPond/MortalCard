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
                Debug.LogError($"Card ID[{cardId}] not found in library.");
                return null;
            }

            return _cards[cardId];
        }

        public StandardCardData GetStandardCardData(string cardId)
        {
            var cardData = GetCardData(cardId);
            if (cardData is StandardCardData standardCardData)
                return standardCardData;

            Debug.LogError($"Card ID[{cardId}] 不是 Standard CardData。");
            return null;
        }
    }
}
