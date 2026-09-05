using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface ITargetCardValue
    {
        Option<ICardEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NoneCard : ITargetCardValue
    {
        public Option<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return Option.None<ICardEntity>();
        }
    }
    [Serializable]
    public class SelectedCard : ITargetCardValue
    {
        public Option<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Model.GetCard(triggerContext.Model.ContextManager.Context.SelectedCard);
        }
    }
    [Serializable]
    public class TriggeredCard : ITargetCardValue
    {
        public Option<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered is ICardTriggeredSource source
                ? source.Card.SomeNotNull()
                : Option.None<ICardEntity>();
        }
    }
    [Serializable]
    public class ActionCard : ITargetCardValue
    {
        public Option<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Action.Source switch
            {
                CardPlaySource cardPlaySource =>
                    cardPlaySource.Card.SomeNotNull(),
                CardPlayResultSource cardPlayResultSource =>
                    cardPlayResultSource.CardPlaySource.Card.SomeNotNull(),
                _ => Option.None<ICardEntity>()
            };
        }
    }
    [Serializable]
    public class PlayingCardOfPlayer : ITargetCardValue
    {
        [HorizontalGroup("1")]
        public ITargetPlayerValue Player;

        public Option<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return Player
                .Eval(triggerContext)
                .FlatMap(player => player.CardManager.PlayingCard);
        }
    }
    [Serializable]
    public class IndexOfCardCollection : ITargetCardValue
    {


        [HorizontalGroup("1")]
        public ITargetCardCollectionValue CardCollection;

        [HorizontalGroup("2")]
        public IIntegerValue Index;

        public OrderType Order;

        public Option<ICardEntity> Eval(TriggerContext triggerContext)
        {
            var cards = CardCollection.Eval(triggerContext);
            return Order switch
            {
                OrderType.Ascending => _EvalIndexed(cards.ToList(), triggerContext),
                OrderType.Descending => _EvalIndexed(cards.Reverse().ToList(), triggerContext),
                _ => Option.None<ICardEntity>()
            };
        }

        private Option<ICardEntity> _EvalIndexed(
            IReadOnlyList<ICardEntity> orderedCards,
            TriggerContext triggerContext)
        {
            return Index
                .Eval(triggerContext)
                .Filter(index => orderedCards.Count > index && index >= 0)
                .FlatMap(index => orderedCards.ElementAt(index).SomeNotNull());
        }
    }

    public interface ITargetCardCollectionValue
    {
        IReadOnlyCollection<ICardEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class SingleCardCollection : ITargetCardCollectionValue
    {
        [HorizontalGroup("1")]
        public ITargetCardValue TargetCard;

        public IReadOnlyCollection<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return TargetCard
                .Eval(triggerContext)
                .ToEnumerable().ToList();
        }
    }
    [Serializable]
    public class CardsOfPlayer : ITargetCardCollectionValue
    {
        [HorizontalGroup("1")]
        public ITargetPlayerValue Player;

        [HorizontalGroup("1")]
        public CardCollectionType Zone = CardCollectionType.HandCard;

        public IReadOnlyCollection<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return Player
                .Eval(triggerContext)
                .Map(player => player.CardManager.GetCardCollectionZone(Zone).Cards)
                .ValueOr(Array.Empty<ICardEntity>());
        }
    }

    [Serializable]
    public class FilteredCardCollection : ITargetCardCollectionValue
    {
        [HorizontalGroup("1")]
        public ITargetCardCollectionValue CardCollection;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<ICardValueCondition> Conditions = new();

        public IReadOnlyCollection<ICardEntity> Eval(TriggerContext triggerContext)
        {
            return CardCollection
                .Eval(triggerContext)
                .Where(card => Conditions.All(
                    condition => condition.Eval(triggerContext, card)))
                .ToArray();
        }
    }


}
