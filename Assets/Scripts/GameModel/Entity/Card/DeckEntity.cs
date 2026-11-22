using System;
using MortalGame.GameData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;

namespace MortalGame.GameModel
{

    public interface IDeckEntity : ICardColletionZone
    {
        Option<ICardEntity> PopCardOrNone();
        void EnqueueCardsThenShuffle(IEnumerable<ICardEntity> cards);
        void ApplyInitialPriority();
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

        public void ApplyInitialPriority()
        {
            _cards = _cards.OrderByDescending(card => card.HasProperty(CardProperty.InitialPriority)).ToList();
        }

        public void EnqueueCardsThenShuffle(IEnumerable<ICardEntity> cards)
        {
            _cards.AddRange(cards);
            _random.ShuffleInPlace(_cards);
        }
    }

}
