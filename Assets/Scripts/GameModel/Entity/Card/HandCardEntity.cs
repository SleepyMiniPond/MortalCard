using System;
using System.Collections.Generic;
using System.Linq;
using Optional;

public interface IHandCardEntity : ICardColletionZone
{
    int MaxCount { get; }
    IReadOnlyCollection<ICardEntity> ClearHand();
}

public class HandCardEntity : CardColletionZone, IHandCardEntity
{
    private int _maxCount;

    public int MaxCount => _maxCount;

    public HandCardEntity(int maxCount) : base(CardCollectionType.HandCard)
    {
        _maxCount = maxCount;
    }

    public IReadOnlyCollection<ICardEntity> ClearHand()
    {
        var preservedCards = _cards.Where(c => c.HasProperty(CardProperty.Preserved)).ToList();
        var recycleCards = _cards.Except(preservedCards).ToList();
        _cards = preservedCards;
        return recycleCards;
    }
}

