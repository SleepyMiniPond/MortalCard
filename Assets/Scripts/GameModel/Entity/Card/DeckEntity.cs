using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;

public interface IDeckEntity : ICardColletionZone
{
    Option<ICardEntity> PopCardOrNone();
    void EnqueueCardsThenShuffle(IEnumerable<ICardEntity> cards);
}
public class DeckEntity : CardColletionZone, IDeckEntity
{
    private readonly IGameRandom _random;

    public DeckEntity(IGameRandom random) : base(CardCollectionType.Deck)
    {
        _random = random;
    }

    public Option<ICardEntity> PopCardOrNone()
    {
        var popCard = OptionCollectionExtensions.ElementAtOrNone(Cards, 0);        
        _cards = Cards.Skip(1).ToList();
        return popCard;
    }

    public void EnqueueCardsThenShuffle(IEnumerable<ICardEntity> cards)
    {
        _cards.AddRange(cards);
        _random.ShuffleInPlace(_cards);
    }
}
