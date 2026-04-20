using System;
using System.Collections.Generic;
using System.Linq;

public interface ISelectedCardEntity
{
    int MaxCount { get; }
    IReadOnlyCollection<ICardEntity> Cards { get; }
    bool TryGetCard(Guid cardIdentity, out ICardEntity card);
    bool TryAddCard(ICardEntity card);
    bool RemoveCard(ICardEntity card);

    IReadOnlyCollection<ICardEntity> UnSelectAllCards();
}

public class SelectedCardEntity : ISelectedCardEntity
{
    public IReadOnlyCollection<ICardEntity> Cards => _cards;
    public int MaxCount => _maxCount;

    private int _maxCount;
    private List<ICardEntity> _cards;

    public SelectedCardEntity(int selectedCardMaxCount, IEnumerable<ICardEntity> cards)
    {
        _maxCount = selectedCardMaxCount;
        _cards = new List<ICardEntity>(cards);
    }

    public bool TryGetCard(Guid cardIdentity, out ICardEntity card)
    {
        card = _cards.FirstOrDefault(c => c.Identity == cardIdentity);
        return card != null;
    }

    public bool TryAddCard(ICardEntity card)
    {
        if (_cards.Count < _maxCount) 
        {
            _cards.Add(card);
            return true;
        }
        return false;
    }

    public bool RemoveCard(ICardEntity card)
    {
        return _cards.Remove(card);
    }

    public IReadOnlyCollection<ICardEntity> UnSelectAllCards()
    {
        var removedCards = _cards.ToList();
        _cards.Clear();
        return removedCards;
    }
}

