using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Optional;
using Optional.Collections;
using UniRx;
using UnityEngine;

public interface IGameViewModel
{
    void UpdateCardCollectionInfo(Faction faction, CardCollectionInfo cardCollectionInfo);
    void UpdateCardManagerInfo(Faction faction, CardManagerInfo cardManagerInfo);
    void EnableHandCardsAction();
    void DisableHandCardsAction();

    IReadOnlyReactiveProperty<bool> IsHandCardsEnabled { get; }
    IReadOnlyReactiveProperty<CardCollectionInfo> ObservableCardCollectionInfo(Faction faction, CardCollectionType type);


    void UpdateCardInfo(CardInfo cardInfo);
    void UpdatePlayerBuffInfo(PlayerBuffInfo playerBuffInfo);
    void UpdateCharacterBuffInfo(CharacterBuffInfo characterBuffInfo);

    Option<IReadOnlyReactiveProperty<CardInfo>> ObservableCardInfo(Guid identity);
    Option<IReadOnlyReactiveProperty<PlayerBuffInfo>> ObservablePlayerBuffInfo(Guid identity);
    Option<IReadOnlyReactiveProperty<CharacterBuffInfo>> ObservableCharacterBuffInfo(Guid identity);

    void UpdateDispositionInfo(DispositionInfo dispositionInfo);
    IReadOnlyReactiveProperty<DispositionInfo> ObservableDispositionInfo { get; }
}

public class GameViewModel : IGameViewModel
{
    private Dictionary<Guid, ReactiveProperty<CardInfo>> _cardInfos;
    private Dictionary<Guid, ReactiveProperty<PlayerBuffInfo>> _playerBuffInfos;
    private Dictionary<Guid, ReactiveProperty<CharacterBuffInfo>> _characterBuffInfos;
    private Dictionary<Faction, Dictionary<CardCollectionType, ReactiveProperty<CardCollectionInfo>>> _cardCollectionInfos;
    private ReactiveProperty<DispositionInfo> _dispositionInfo;

    private readonly ReactiveProperty<bool> _isHandCardsEnabled;
    public IReadOnlyReactiveProperty<bool> IsHandCardsEnabled => _isHandCardsEnabled;

    public GameViewModel()
    {
        _cardInfos = new Dictionary<Guid, ReactiveProperty<CardInfo>>();
        _playerBuffInfos = new Dictionary<Guid, ReactiveProperty<PlayerBuffInfo>>();
        _characterBuffInfos = new Dictionary<Guid, ReactiveProperty<CharacterBuffInfo>>();
        _cardCollectionInfos = new Dictionary<Faction, Dictionary<CardCollectionType, ReactiveProperty<CardCollectionInfo>>>
        {
            { Faction.Ally, new Dictionary<CardCollectionType, ReactiveProperty<CardCollectionInfo>>
                {
                    { CardCollectionType.Deck, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.HandCard, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.Graveyard, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.ExclusionZone, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.DisposeZone, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) }
                }
            },
            { Faction.Enemy, new Dictionary<CardCollectionType, ReactiveProperty<CardCollectionInfo>>
                {
                    { CardCollectionType.Deck, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.HandCard, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.Graveyard, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.ExclusionZone, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) },
                    { CardCollectionType.DisposeZone, new ReactiveProperty<CardCollectionInfo>(CardCollectionInfo.Empty) }
                }
            }
        };
        _dispositionInfo = new ReactiveProperty<DispositionInfo>(new DispositionInfo(0, 0));
        _isHandCardsEnabled = new ReactiveProperty<bool>(false);
    }

    public void EnableHandCardsAction()
    {
        _isHandCardsEnabled.Value = true;
    }
    public void DisableHandCardsAction()
    {
        _isHandCardsEnabled.Value = false;
    }
    public void UpdateCardManagerInfo(Faction faction, CardManagerInfo cardManagerInfo)
    {
        foreach (var kvp in cardManagerInfo.CardZoneInfos)
        {
            var cardCollectionType = kvp.Key;
            var cardIdentities = kvp.Value;

            var info = new CardCollectionInfo(
                cardCollectionType,
                cardIdentities
                    .Select((guid, idx) => (guid, idx))
                    .ToImmutableDictionary(
                        pair => _cardInfos[pair.guid].Value,
                        pair => pair.idx));
            
            if (!_cardCollectionInfos[faction].ContainsKey(cardCollectionType))
            {
                _cardCollectionInfos[faction][cardCollectionType] = new ReactiveProperty<CardCollectionInfo>(info);
            }
            else
            {
                _cardCollectionInfos[faction][cardCollectionType].Value = info;
            }
        }
    }
    public void UpdateCardCollectionInfo(Faction faction, CardCollectionInfo cardCollectionInfo)
    {
        if (!_cardCollectionInfos[faction].ContainsKey(cardCollectionInfo.Type))
        {
            _cardCollectionInfos[faction][cardCollectionInfo.Type] = new ReactiveProperty<CardCollectionInfo>(cardCollectionInfo);
        }
        else
        {
            _cardCollectionInfos[faction][cardCollectionInfo.Type].Value = cardCollectionInfo;
        }

        foreach (var cardInfo in cardCollectionInfo.CardInfos)
        {
            UpdateCardInfo(cardInfo.Key);
        }
    }

    public void UpdateCardInfo(CardInfo cardInfo)
    {
        if (!_cardInfos.ContainsKey(cardInfo.Identity))
        {
            _cardInfos[cardInfo.Identity] = new ReactiveProperty<CardInfo>(cardInfo);
        }
        else
        {
            _cardInfos[cardInfo.Identity].Value = cardInfo;
        }
    }

    public void UpdatePlayerBuffInfo(PlayerBuffInfo playerBuffInfo)
    {
        if (!_playerBuffInfos.ContainsKey(playerBuffInfo.Identity))
        {
            _playerBuffInfos[playerBuffInfo.Identity] = new ReactiveProperty<PlayerBuffInfo>(playerBuffInfo);
        }
        else
        {
            _playerBuffInfos[playerBuffInfo.Identity].Value = playerBuffInfo;
        }
    }

    public void UpdateCharacterBuffInfo(CharacterBuffInfo characterBuffInfo)
    {
        if (!_characterBuffInfos.ContainsKey(characterBuffInfo.Identity))
        {
            _characterBuffInfos[characterBuffInfo.Identity] = new ReactiveProperty<CharacterBuffInfo>(characterBuffInfo);
        }
        else
        {
            _characterBuffInfos[characterBuffInfo.Identity].Value = characterBuffInfo;
        }
    }
    public void UpdateDispositionInfo(DispositionInfo dispositionInfo)
    {
        _dispositionInfo.Value = dispositionInfo;
    }

    public IReadOnlyReactiveProperty<CardCollectionInfo> ObservableCardCollectionInfo(Faction faction, CardCollectionType type)
    {
        return _cardCollectionInfos[faction][type];
    }


    public Option<IReadOnlyReactiveProperty<CardInfo>> ObservableCardInfo(Guid identity)
    {
        return OptionCollectionExtensions.GetValueOrNone(_cardInfos, identity)
            .Map(prop => (IReadOnlyReactiveProperty<CardInfo>)prop);
    }

    public Option<IReadOnlyReactiveProperty<PlayerBuffInfo>> ObservablePlayerBuffInfo(Guid identity)
    {
        return OptionCollectionExtensions.GetValueOrNone(_playerBuffInfos, identity)
            .Map(prop => (IReadOnlyReactiveProperty<PlayerBuffInfo>)prop);
    }

    public Option<IReadOnlyReactiveProperty<CharacterBuffInfo>> ObservableCharacterBuffInfo(Guid identity)
    {
        return OptionCollectionExtensions.GetValueOrNone(_characterBuffInfos, identity)
            .Map(prop => (IReadOnlyReactiveProperty<CharacterBuffInfo>)prop);
    }

    public IReadOnlyReactiveProperty<DispositionInfo> ObservableDispositionInfo { get { return _dispositionInfo; } }
}