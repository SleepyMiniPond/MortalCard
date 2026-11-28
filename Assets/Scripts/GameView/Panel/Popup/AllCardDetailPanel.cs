using System;
using MortalGame.Exceptions;
using MortalGame.Presentation.Abstractions;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
namespace MortalGame.GameView
{

    public interface IAllCardDetailPanel
    {
        void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
        IAllCardDetailSession Open(IReadOnlyReactiveProperty<AllCardDetailDisplayState> state);
    }

    public class AllCardDetailPanel : MonoBehaviour, IAllCardDetailPanel
    {
        [SerializeField]
        private Button[] _closeButtons;
        [SerializeField]
        private Button _deckButton;
        [SerializeField]
        private Button _handCardButton;
        [SerializeField]
        private Button _graveyardButton;
        [SerializeField]
        private GameObject _panel;
        [SerializeField]
        private Transform _cardViewParent;
        [SerializeField]
        private CardViewFactory _cardViewFactory;

        private IGameViewModel _gameViewModel;
        private LocalizeLibrary _localizeLibrary;
        private Session _activeSession;

        public void Init(
            IGameViewModel gameInfoModel,
            LocalizeLibrary localizeLibrary)
        {
            _gameViewModel = gameInfoModel;
            _localizeLibrary = localizeLibrary;
        }

        public IAllCardDetailSession Open(IReadOnlyReactiveProperty<AllCardDetailDisplayState> state)
        {
            if (_activeSession != null)
                throw new SessionAlreadyOpenException(typeof(AllCardDetailPanel));
            var session = new Session(this);
            _activeSession = session;
            try
            {
                _panel.SetActive(true);
                session.Bind(state);
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private void _RenderState(Session session, AllCardDetailDisplayState state)
        {
            var identities = state.Cards.Select(card => card.Identity).ToHashSet();
            foreach (var identity in session.Cards.Keys.Where(id => !identities.Contains(id)).ToArray())
            {
                _cardViewFactory.RecyclePrefab(session.Cards[identity].View);
                session.Cards.Remove(identity);
            }

            for (var index = 0; index < state.Cards.Count; index++)
            {
                var card = state.Cards[index];
                var needsRender = !session.Cards.TryGetValue(card.Identity, out var entry);
                if (needsRender)
                {
                    var view = _cardViewFactory.CreatePrefab();
                    entry = (card, view);
                    session.Cards.Add(card.Identity, entry);
                    view.transform.SetParent(_cardViewParent, false);
                    view.Initialize(_gameViewModel, _localizeLibrary);
                }
                if (needsRender || !Equals(entry.Info, card))
                {
                    entry.View.Render(new ICardView.CardClickableProperty(card, true,
                        (identity, _) => session.Click(identity)));
                    session.Cards[card.Identity] = (card, entry.View);
                }
                entry.View.transform.SetSiblingIndex(index);
            }
        }

        private void _Close(Session session)
        {
            try
            {
                foreach (var entry in session.Cards.Values)
                    _cardViewFactory.RecyclePrefab(entry.View);
            }
            finally
            {
                session.Cards.Clear();
                if (ReferenceEquals(_activeSession, session))
                {
                    _activeSession = null;
                    if (_panel != null) _panel.SetActive(false);
                }
            }
        }

        private void OnDestroy() => _activeSession?.Dispose();

        private sealed class Session : IAllCardDetailSession
        {
            public readonly Dictionary<Guid, (CardInfo Info, CardView View)> Cards = new();
            private readonly AllCardDetailPanel _owner;
            private readonly CompositeDisposable _resources = new();
            private readonly Subject<Unit> _ended = new();
            private readonly Subject<Guid> _cardClicked = new();
            private bool _isDisposed;

            public IObservable<CardCollectionType> CollectionRequested { get; }
            public IObservable<Guid> CardClicked { get; }
            public IObservable<Unit> CloseRequested { get; }

            public Session(AllCardDetailPanel owner)
            {
                _owner = owner;
                _ended.AddTo(_resources);
                _cardClicked.AddTo(_resources);
                CollectionRequested = Observable.Merge(
                    owner._deckButton.OnClickAsObservable().Select(_ => CardCollectionType.Deck),
                    owner._handCardButton.OnClickAsObservable().Select(_ => CardCollectionType.HandCard),
                    owner._graveyardButton.OnClickAsObservable().Select(_ => CardCollectionType.Graveyard))
                    .TakeUntil(_ended);
                CardClicked = _cardClicked.TakeUntil(_ended);
                CloseRequested = owner._closeButtons.Select(button => button.OnClickAsObservable())
                    .Merge().TakeUntil(_ended);
            }

            public void Bind(IReadOnlyReactiveProperty<AllCardDetailDisplayState> state)
                => state.Subscribe(value => _owner._RenderState(this, value)).AddTo(_resources);

            public void Click(Guid identity)
            {
                if (!_isDisposed) _cardClicked.OnNext(identity);
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                try { _ended.OnNext(Unit.Default); }
                finally
                {
                    try { _resources.Dispose(); }
                    finally { _owner._Close(this); }
                }
            }
        }
    }
}
