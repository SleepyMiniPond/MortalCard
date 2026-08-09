using System;
using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;
using Optional;
using UniRx;

namespace MortalGame.Presentation.Abstractions
{
    public interface IGameplayActionReciever
    {
        void RecieveEvent(IGameCommand gameCommand);

        IEnumerable<ISelectableView> SelectableViews { get; }
        ISelectableView BasicSelectableView { get; }
    }

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

        Option<CardInfo> GetCardInfoOrNone(Guid identity);
        Option<IReadOnlyReactiveProperty<CardInfo>> ObservableCardInfo(Guid identity);
        Option<IReadOnlyReactiveProperty<PlayerBuffInfo>> ObservablePlayerBuffInfo(Guid identity);
        Option<IReadOnlyReactiveProperty<CharacterBuffInfo>> ObservableCharacterBuffInfo(Guid identity);

        void UpdateDispositionInfo(DispositionInfo dispositionInfo);
        IReadOnlyReactiveProperty<DispositionInfo> ObservableDispositionInfo { get; }
    }
}
