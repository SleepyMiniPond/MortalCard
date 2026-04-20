using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Optional;
using UnityEngine;


public interface IPlayerCardManager
{
    IDeckEntity Deck { get; }
    IHandCardEntity HandCard { get; }
    IGraveyardEntity Graveyard { get; }
    IExclusionZoneEntity ExclusionZone { get; }
    IDisposeZoneEntity DisposeZone { get; }
    Option<ICardEntity> PlayingCard { get; }
    ICardColletionZone GetCardCollectionZone(CardCollectionType type);

    (bool Success, IDisposable PlayCardDisposable) TryPlayCard(ICardEntity card, out int handCardIndex, out int handCardsCount);
    IEnumerable<IGameEvent> ClearHandOnTurnEnd(IGameplayModel model);
    IEnumerable<IGameEvent> RecycleCardOnPlayEnd(IGameplayModel model, ICardEntity card);

    Option<ICardEntity> GetCardOrNone(Func<ICardEntity, bool> predicate);
    MoveCardResult MoveCard(
        ICardEntity card,
        CardCollectionType start,
        CardCollectionType destination);

    CreateCardResult CreateNewCard(
        ICardEntity newCard,
        CardCollectionType cloneDestination);

    IEnumerable<ICardEntity> Update(TriggerContext triggerContext);
}

public record CardManagerInfo(
    IReadOnlyDictionary<CardCollectionType, ImmutableArray<Guid>> CardZoneInfos,
    Option<CardInfo> PlayingCard);

public class PlayerCardManager : IPlayerCardManager, IDisposable
{
    public IHandCardEntity HandCard { get; }
    public IDeckEntity Deck { get; }
    public IGraveyardEntity Graveyard { get; }
    public IExclusionZoneEntity ExclusionZone { get; }
    public IDisposeZoneEntity DisposeZone { get; }
    public Option<ICardEntity> PlayingCard { get; private set; }

    public PlayerCardManager(
        int handCardMaxCount)
    {
        Deck = new DeckEntity();
        HandCard = new HandCardEntity(handCardMaxCount);
        Graveyard = new GraveyardEntity();
        ExclusionZone = new ExclusionZoneEntity();
        DisposeZone = new DisposeZoneEntity();
        PlayingCard = Option.None<ICardEntity>();
    }

    public ICardColletionZone GetCardCollectionZone(CardCollectionType type)
    {
        switch (type)
        {
            case CardCollectionType.Deck:
                return Deck;
            case CardCollectionType.HandCard:
                return HandCard;
            case CardCollectionType.Graveyard:
                return Graveyard;
            case CardCollectionType.ExclusionZone:
                return ExclusionZone;
            case CardCollectionType.DisposeZone:
                return DisposeZone;
            default:
                return CardColletionZone.Dummy;
        }
    }

    public (bool Success, IDisposable PlayCardDisposable) TryPlayCard(ICardEntity card, out int index, out int cardsCount)
    {
        cardsCount = HandCard.Cards.Count;
        index = HandCard.Cards.ToList().IndexOf(card);
        if (index < 0)
        {
            return (false, this);
        }
        else
        {
            HandCard.RemoveCard(card);
            PlayingCard = Option.Some(card);
            return (true, this);
        }
    }
    public void Dispose()
    {
        PlayingCard.MatchSome(
            card =>
            {
                ICardColletionZone destination =
                    (card.HasProperty(CardProperty.Dispose) || card.HasProperty(CardProperty.AutoDispose)) ?
                    ExclusionZone : Graveyard;
                destination.AddCard(card);
            }
        );
        PlayingCard = Option.None<ICardEntity>();
    }

    public IEnumerable<IGameEvent> ClearHandOnTurnEnd(IGameplayModel model)
    {
        var events = new List<IGameEvent>();

        // TODO:  trigger preserved timing
        var nonePreservedCards = HandCard.ClearHand();
        var excludeCards = nonePreservedCards.Where(c => c.HasProperty(CardProperty.AutoDispose)).ToArray();
        ExclusionZone.AddCards(excludeCards);

        var discardedCards = nonePreservedCards.Except(excludeCards).ToArray();
        Graveyard.AddCards(discardedCards);
        events.Add(new DiscardHandCardEvent(
            Faction: this.Owner(model).ValueOr(DummyPlayer.Instance).Faction,
            DiscardedCardInfos: discardedCards.Select(c => c.ToInfo(model)).ToArray(),
            ExcludedCardInfos: excludeCards.Select(c => c.ToInfo(model)).ToArray(),
            CardManagerInfo: this.ToInfo(model)));

        return events;
    }

    public IEnumerable<IGameEvent> RecycleCardOnPlayEnd(IGameplayModel model, ICardEntity card)
    {
        var events = new List<IGameEvent>();

        if(Graveyard.GetCardOrNone(c => c.Identity == card.Identity).TryGetValue(out var recycledCard))
        {
            Graveyard.RemoveCard(recycledCard);
            HandCard.AddCard(recycledCard);

            events.Add(new RecycleGraveyardToHandCardEvent(
                Faction: this.Owner(model).ValueOr(DummyPlayer.Instance).Faction,
                RecycledCardInfo: recycledCard.ToInfo(model),
                CardManagerInfo: this.ToInfo(model)));
        }

        return events;
    }

    public Option<ICardEntity> GetCardOrNone(Func<ICardEntity, bool> predicate)
    {
        return HandCard.GetCardOrNone(predicate)
            .Else(Deck.GetCardOrNone(predicate))
            .Else(Graveyard.GetCardOrNone(predicate))
            .Else(ExclusionZone.GetCardOrNone(predicate))
            .Else(DisposeZone.GetCardOrNone(predicate))
            .Else(PlayingCard.Filter(card => predicate(card)));
    }

    public MoveCardResult MoveCard(
        ICardEntity card,
        CardCollectionType start,
        CardCollectionType destination)
    {
        var startZone = GetCardCollectionZone(start);
        var destinationZone = GetCardCollectionZone(destination);

        startZone.RemoveCard(card);
        destinationZone.AddCard(card);

        return new MoveCardResult(card, start, destination);
    }

    public CreateCardResult CreateNewCard(
        ICardEntity newCard,
        CardCollectionType cloneDestination)
    {
        _AddCard(newCard, cloneDestination);

        return new CreateCardResult(newCard, cloneDestination);
    }
    
    private void _AddCard(ICardEntity card, CardCollectionType type)
    {
        switch (type)
        {
            case CardCollectionType.Deck:
                Deck.EnqueueCardsThenShuffle(new[] { card });
                break;
            case CardCollectionType.HandCard:
                HandCard.AddCard(card);
                break;
            case CardCollectionType.Graveyard:
                Graveyard.AddCard(card);
                break;
            case CardCollectionType.ExclusionZone:
                ExclusionZone.AddCard(card);
                break;
            case CardCollectionType.DisposeZone:
                DisposeZone.AddCard(card);
                break;
        }
    }

    public IEnumerable<ICardEntity> Update(TriggerContext triggerContext)
    {
        foreach (var card in HandCard.Cards
            .Concat(Deck.Cards)
            .Concat(Graveyard.Cards)
            .Concat(ExclusionZone.Cards)
            .Concat(DisposeZone.Cards))
        {
            if (card.BuffManager.Update(triggerContext))
            {
                yield return card;
            }
        }
    }
}

public static class PlayerCardManagerExtensions
{
    public static Option<IPlayerEntity> Owner(this IPlayerCardManager cardManager, IGameplayModel watcher)
    {
        if (watcher.GameStatus.Ally.CardManager == cardManager)
            return (watcher.GameStatus.Ally as IPlayerEntity).Some();
        if (watcher.GameStatus.Enemy.CardManager == cardManager)
            return (watcher.GameStatus.Enemy as IPlayerEntity).Some();
        return Option.None<IPlayerEntity>();
    }

    public static CardManagerInfo ToInfo(this IPlayerCardManager cardManager, IGameplayModel gameWatcher)
        => new(
            new Dictionary<CardCollectionType, ImmutableArray<Guid>>
            {
                { CardCollectionType.Deck, cardManager.Deck.ToCardIdentities() },
                { CardCollectionType.HandCard, cardManager.HandCard.ToCardIdentities() },
                { CardCollectionType.Graveyard, cardManager.Graveyard.ToCardIdentities() },
                { CardCollectionType.ExclusionZone, cardManager.ExclusionZone.ToCardIdentities() },
                { CardCollectionType.DisposeZone, cardManager.DisposeZone.ToCardIdentities() }
            },
            cardManager.PlayingCard.Map(c => c.ToInfo(gameWatcher)));
}
