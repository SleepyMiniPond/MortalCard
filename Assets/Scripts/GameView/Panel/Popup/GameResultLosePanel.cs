using System;
using MortalGame.GameModel;
using MortalGame.Presentation.Abstractions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.GameView
{

    public interface IGameResultLosePanel
    {
        IGameResultLoseSession Open();
    }

    public class GameResultLosePanel : MonoBehaviour, IGameResultLosePanel
    {
        [SerializeField] private GameObject _panel;

        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        private Session _activeSession;

        public IGameResultLoseSession Open()
        {
            if (_activeSession != null)
                throw new InvalidOperationException("失敗面板尚未釋放，不能重複開啟。");
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

        private sealed class Session : IGameResultLoseSession
        {
            private readonly GameResultLosePanel _owner;
            private readonly Subject<Unit> _ended = new();
            private bool _isDisposed;
            public IObservable<LoseReactionType> ReactionRequested { get; }

            public Session(GameResultLosePanel owner)
            {
                _owner = owner;
                ReactionRequested = Observable.Merge(
                    owner._retryButton.OnClickAsObservable().Select(_ => LoseReactionType.Retry),
                    owner._restartButton.OnClickAsObservable().Select(_ => LoseReactionType.Restart),
                    owner._quitButton.OnClickAsObservable().Select(_ => LoseReactionType.Quit))
                    .TakeUntil(_ended);
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
