using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;

public interface ICardColletionZone
{
    CardCollectionType Type { get; }
    IReadOnlyCollection<ICardEntity> Cards { get; }
    Option<ICardEntity> GetCardOrNone(Func<ICardEntity, bool> predicate);
    void AddCard(ICardEntity card);
    void AddCards(IEnumerable<ICardEntity> cards);
    bool RemoveCard(ICardEntity card);
}

public abstract class CardColletionZone : ICardColletionZone
{
    private CardCollectionType _type;
    protected List<ICardEntity> _cards;
    public IReadOnlyCollection<ICardEntity> Cards => _cards;
    public CardCollectionType Type => _type;

    public static ICardColletionZone Dummy => new DummyCardCollectionZone();
    
    public CardColletionZone(CardCollectionType type)
    {
        _type = type;
        _cards = new List<ICardEntity>();
    }

    public Option<ICardEntity> GetCardOrNone(Func<ICardEntity, bool> predicate)
    {
        return OptionCollectionExtensions.FirstOrNone(_cards, c => predicate(c));
    }

    public void AddCard(ICardEntity card)
    {
        _cards.Add(card);
    }
    public void AddCards(IEnumerable<ICardEntity> cards)
    {
        _cards.AddRange(cards);
    }
    public bool RemoveCard(ICardEntity card)
    {
        return _cards.Remove(card);
    }
}

public class DummyCardCollectionZone : CardColletionZone
{
    public DummyCardCollectionZone() : base(CardCollectionType.None)
    {
    }
}