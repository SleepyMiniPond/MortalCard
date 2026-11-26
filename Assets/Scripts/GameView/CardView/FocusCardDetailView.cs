using System;
using MortalGame.Presentation.Abstractions;
using UniRx;
using UnityEngine;
using MortalGame.UI;

namespace MortalGame.GameView
{
    public class FocusCardDetailView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private RectTransform _content;
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

        // 定位參考留在 View 邊界，不混入純顯示狀態；懸停沒有按鈕，只回傳可釋放的顯示範圍。
        public IDisposable Open(IReadOnlyReactiveProperty<FocusCardDetailDisplayState> state, RectTransform targetRect)
        {
            if (_activeSession != null)
                throw new InvalidOperationException("懸停卡片詳情尚未釋放，不能重複開啟。");
            var session = new Session(this);
            _activeSession = session;
            try
            {
                _panel.SetActive(true);
                var canvas = _content.GetComponentInParent<Canvas>();
                var rectOnCanvas = canvas.GetRectOnCanvas(targetRect, _content.parent as RectTransform);
                _content.anchoredPosition = new Vector2(rectOnCanvas.center.x, _content.anchoredPosition.y);
                session.Bind(state);
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private void _RenderState(FocusCardDetailDisplayState state)
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
            if (_cardView != null)
            {
                // 固定詳情卡保留既有版面；物件池用的 Reset 會將本地位置歸零。
                var position = _cardView.RectTransform.localPosition;
                var rotation = _cardView.RectTransform.localRotation;
                _cardView.Reset();
                _cardView.SetPositionAndRotation(position, rotation);
            }
            if (_cardBuffHint != null) _cardBuffHint.HideHint();
            if (_cardKeywordHint != null) _cardKeywordHint.HideHint();
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy() => _activeSession?.Dispose();

        private sealed class Session : IDisposable
        {
            private readonly FocusCardDetailView _owner;
            private readonly CompositeDisposable _resources = new();
            private bool _isDisposed;
            public Session(FocusCardDetailView owner) => _owner = owner;
            public void Bind(IReadOnlyReactiveProperty<FocusCardDetailDisplayState> state)
                => state.Subscribe(_owner._RenderState).AddTo(_resources);
            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                try { _resources.Dispose(); }
                finally { _owner._Close(this); }
            }
        }
    }
}
