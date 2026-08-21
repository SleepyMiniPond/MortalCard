using System;
using System.Collections.Generic;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{

    public interface ISelectedCardEntity
    {
        int MaxCount { get; }
        IReadOnlyCollection<ICardEntity> Cards { get; }
        bool TryGetCard(Guid cardIdentity, out ICardEntity card);
        bool TryAddCard(ICardEntity card);
        bool RemoveCard(ICardEntity card);
        Option<int> EvalTotalCost(IGameplayModel model);

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

        public Option<int> EvalTotalCost(IGameplayModel model)
        {
            var totalCost = 0;
            foreach (var card in _cards)
            {
                var triggerContext = new TriggerContext(
                    model,
                    new CardTrigger(card),
                    new CardLookIntentAction(card));
                if (!GameFormula.CardCost(triggerContext, card)
                        .FlatMap(cost => GameplayIntegerMath.Add(totalCost, cost))
                        .TryGetValue(out var newTotalCost))
                {
                    return Option.None<int>();
                }

                totalCost = newTotalCost;
            }

            return totalCost.Some();
        }

        public IReadOnlyCollection<ICardEntity> UnSelectAllCards()
        {
            var removedCards = _cards.ToList();
            _cards.Clear();
            return removedCards;
        }
    }


}
