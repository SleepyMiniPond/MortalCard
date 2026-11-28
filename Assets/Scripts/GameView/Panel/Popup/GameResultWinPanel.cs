using System;
using MortalGame.Exceptions;
using MortalGame.Presentation.Abstractions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.GameView
{

    public interface IGameResultWinPanel
    {
        IGameResultWinSession Open();
    }

    public class GameResultWinPanel : MonoBehaviour, IGameResultWinPanel
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _continueButton;
        private Session _activeSession;

        public IGameResultWinSession Open()
        {
            if (_activeSession != null)
                throw new SessionAlreadyOpenException(typeof(GameResultWinPanel));
            var session = new Session(this);
            _activeSession = session;
            try
            {
                _panel.SetActive(true);
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private void _Close(Session session)
        {
            if (!ReferenceEquals(_activeSession, session)) return;
            _activeSession = null;
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy() => _activeSession?.Dispose();

        private sealed class Session : IGameResultWinSession
        {
            private readonly GameResultWinPanel _owner;
            private readonly Subject<Unit> _ended = new();
            private bool _isDisposed;
            public IObservable<Unit> ContinueRequested { get; }

            public Session(GameResultWinPanel owner)
            {
                _owner = owner;
                ContinueRequested = owner._continueButton.OnClickAsObservable().TakeUntil(_ended);
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                try { _ended.OnNext(Unit.Default); }
                finally
                {
                    _ended.Dispose();
                    _owner._Close(this);
                }
            }
        }
    }
}
