using System;
using System.Collections.Generic;
using System.Linq;

public interface IGraveyardEntity : ICardColletionZone
{
    IReadOnlyCollection<ICardEntity> PopAllCards();
    IReadOnlyCollection<ICardEntity> PopRecycleCards();
}
public class GraveyardEntity : CardColletionZone, IGraveyardEntity
{        
    public GraveyardEntity() : base(CardCollectionType.Graveyard)
    {
    }

    public IReadOnlyCollection<ICardEntity> PopAllCards()
    {
        var cards = _cards.ToList();
        _cards = new List<ICardEntity>();
        return cards;
    }

    public IReadOnlyCollection<ICardEntity> PopRecycleCards()
    {
        var recycleCards = _cards.Where(c => c.HasProperty(CardProperty.Recycle)).ToList();
        _cards = _cards.Except(recycleCards).ToList();
        return recycleCards;
    }
}