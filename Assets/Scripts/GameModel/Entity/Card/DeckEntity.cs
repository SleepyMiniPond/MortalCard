using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;
using UnityEngine;

public interface IDeckEntity : ICardColletionZone
{
    Option<ICardEntity> PopCardOrNone();
    void EnqueueCardsThenShuffle(IEnumerable<ICardEntity> cards);
}
public class DeckEntity : CardColletionZone, IDeckEntity
{    
    public DeckEntity() : base(CardCollectionType.Deck)
    { }

    public Option<ICardEntity> PopCardOrNone()
    {
        var popCard = OptionCollectionExtensions.ElementAtOrNone(Cards, 0);        
        _cards = Cards.Skip(1).ToList();
        return popCard;
    }

    public void EnqueueCardsThenShuffle(IEnumerable<ICardEntity> cards)
    {
        _cards.AddRange(cards);
        
        for (int i = 0; i < _cards.Count; i++)
        {
            var temp = _cards[i];
            var randomIndex = UnityEngine.Random.Range(0, _cards.Count);
            _cards[i] = _cards[randomIndex];
            _cards[randomIndex] = temp;
        }
    }
}
