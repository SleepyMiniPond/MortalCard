using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Optional;
using UniRx;
using UnityEngine;

public interface IUIPresenter
{
    public record DeckEvent() : IUniTaskPresenter.Event;
    public record GraveyardEvent() : IUniTaskPresenter.Event;
    public record EnemySelectedEvent() : IUniTaskPresenter.Event;
    UniTaskVoid Run(CancellationToken cancellationToken);
}

public class UIPresenter : IUIPresenter
{
    private readonly DeckCardView _deckCardView;
    private readonly GraveyardCardView _graveyardCardView;
    private readonly EnemySelectedCardView _enemySelectedCardView;

    private IAllCardDetailPresenter _allCardDetailPresenter;

    private IUniTaskPresenter _uniTaskPresenter;

    public UIPresenter(
        IInteractionButtonView buttonView,
        IAllCardDetailPanelView panelView,
        IGameViewModel gameViewModel,
        LocalizeLibrary localizeLibrary)
    {
        _deckCardView = buttonView.DeckCardView;
        _graveyardCardView = buttonView.GraveyardCardView;
        _enemySelectedCardView = buttonView.EnemySelectedCardView;

        _allCardDetailPresenter = new AllCardDetailPresenter(panelView, gameViewModel, localizeLibrary);        
        _uniTaskPresenter = new UniTaskPresenter();
    }

    public async UniTaskVoid Run(CancellationToken cancellationToken)
    {
        await _uniTaskPresenter.Run(
            _SetupSubscriptions(),
            () => true,
            cancellationToken,
            EventHandler);

        async UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
        {
            switch (evt)
            {
                case IUIPresenter.DeckEvent:
                    await _allCardDetailPresenter.Run(Faction.Ally, CardCollectionType.Deck, CancellationToken.None);
                    return new IUniTaskPresenter.None();
                case IUIPresenter.GraveyardEvent:
                    await _allCardDetailPresenter.Run(Faction.Ally, CardCollectionType.Graveyard, CancellationToken.None);
                    return new IUniTaskPresenter.None();
                case IUIPresenter.EnemySelectedEvent:
                    await _allCardDetailPresenter.Run(Faction.Enemy, CardCollectionType.Deck, CancellationToken.None);
                    return new IUniTaskPresenter.None();
            }
            return new IUniTaskPresenter.None();
        }
    }

    private IDisposable _SetupSubscriptions()
    {
        var disposables = new CompositeDisposable();
        _deckCardView.DeckButton.OnClickAsObservable()
            .Subscribe(_ => _uniTaskPresenter.TryEnqueueNextEvent(new IUIPresenter.DeckEvent()))
            .AddTo(disposables);                
        _graveyardCardView.GraveyardButton.OnClickAsObservable()
            .Subscribe(_ => _uniTaskPresenter.TryEnqueueNextEvent(new IUIPresenter.GraveyardEvent()))
            .AddTo(disposables);
        _enemySelectedCardView.DeckButton.OnClickAsObservable()
            .Subscribe(_ => _uniTaskPresenter.TryEnqueueNextEvent(new IUIPresenter.EnemySelectedEvent()))
            .AddTo(disposables);
        return disposables;
    }
}
