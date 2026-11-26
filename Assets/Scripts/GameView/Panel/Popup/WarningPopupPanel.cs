using System;
using System.Linq;
using MortalGame.Presentation.Abstractions;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.GameView
{
    public interface IWarningPopupPanel
    {
        IWarningPopupSession Open(IReadOnlyReactiveProperty<WarningPopupDisplayState> state);
    }

    public class WarningPopupPanel : MonoBehaviour, IWarningPopupPanel
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button[] _closeButtons;

        private Session _activeSession;

        public IWarningPopupSession Open(IReadOnlyReactiveProperty<WarningPopupDisplayState> state)
        {
            if (_activeSession != null)
                throw new InvalidOperationException("警告面板尚未釋放，不能重複開啟。");
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

        private void _Close(Session session)
        {
            if (!ReferenceEquals(_activeSession, session)) return;
            _activeSession = null;
            if (_panel != null) _panel.SetActive(false);
            if (_messageText != null) _messageText.text = string.Empty;
        }

        private void OnDestroy() => _activeSession?.Dispose();

        private sealed class Session : IWarningPopupSession
        {
            private readonly WarningPopupPanel _owner;
            private readonly CompositeDisposable _resources = new();
            private readonly Subject<Unit> _ended = new();
            private bool _isDisposed;

            public IObservable<Unit> CloseRequested { get; }

            public Session(WarningPopupPanel owner)
            {
                _owner = owner;
                _ended.AddTo(_resources);
                CloseRequested = owner._closeButtons.Select(button => button.OnClickAsObservable())
                    .Merge().TakeUntil(_ended);
            }

            public void Bind(IReadOnlyReactiveProperty<WarningPopupDisplayState> state)
                => state.Subscribe(value => _owner._messageText.text = value.Message).AddTo(_resources);

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
