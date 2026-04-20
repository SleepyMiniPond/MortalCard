using System.Collections.Generic;
using UnityEngine;

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
}
