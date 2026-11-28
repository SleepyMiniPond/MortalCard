using System;
using MortalGame.Exceptions;
using System.Linq;
using MortalGame.Presentation.Abstractions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.GameView
{
    public interface ISingleCardDetailPopupPanel
    {
        void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
        ISingleCardDetailSession Open(IReadOnlyReactiveProperty<SingleCardDetailDisplayState> state);
    }

    public class SingleCardDetailPopupPanel : MonoBehaviour, ISingleCardDetailPopupPanel
    {
        [SerializeField] private Button[] _closeButtons;
        [SerializeField] private GameObject _panel;
        [SerializeField] private CardView _cardView;
        [SerializeField] private CardPropertyHint _cardBuffHint;
        [SerializeField] private CardPropertyHint _cardKeywordHint;
        private Session _activeSession;

        public void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary)
        {
            _cardBuffHint.Init(localizeLibrary);
            _cardKeywordHint.Init(localizeLibrary);
            _cardView.Initialize(gameInfoModel, localizeLibrary);
        }

        public ISingleCardDetailSession Open(IReadOnlyReactiveProperty<SingleCardDetailDisplayState> state)
        {
            if (_activeSession != null)
                throw new SessionAlreadyOpenException(typeof(SingleCardDetailPopupPanel));
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

        private void _RenderState(SingleCardDetailDisplayState state)
        {
            var property = CardDetailProperty.Create(state.Card);
            _cardView.RenderSnapshot(state.Card);
            _cardBuffHint.ShowHint(property.CardBuffHint, _cardView.RectTransform);
            _cardKeywordHint.ShowHint(property.CardKeywordHint, _cardView.RectTransform);
        }

        private void _Close(Session session)
        {
            if (!ReferenceEquals(_activeSession, session)) return;
            _activeSession = null;
            if (_cardView != null) _cardView.Reset();
            if (_cardBuffHint != null) _cardBuffHint.HideHint();
            if (_cardKeywordHint != null) _cardKeywordHint.HideHint();
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy() => _activeSession?.Dispose();

        private sealed class Session : ISingleCardDetailSession
        {
            private readonly SingleCardDetailPopupPanel _owner;
            private readonly CompositeDisposable _resources = new();
            private readonly Subject<Unit> _ended = new();
            private bool _isDisposed;
            public IObservable<Unit> CloseRequested { get; }

            public Session(SingleCardDetailPopupPanel owner)
            {
                _owner = owner;
                _ended.AddTo(_resources);
                CloseRequested = owner._closeButtons.Select(button => button.OnClickAsObservable())
                    .Merge().TakeUntil(_ended);
            }

            public void Bind(IReadOnlyReactiveProperty<SingleCardDetailDisplayState> state)
                => state.Subscribe(_owner._RenderState).AddTo(_resources);

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
