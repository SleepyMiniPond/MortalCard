using System;
using MortalGame.Presentation.Abstractions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MortalGame.GameModel;
using UniRx;
namespace MortalGame.GameView
{
    [Serializable]
    public sealed class CardSelectionTextSettings
    {
        [Tooltip("專屬提示的本地化 key 格式：{0} 為卡片資料 ID，{1} 為選取群組 ID。")]
        public string PromptKeyFormat;
        [Tooltip("找不到專屬提示時使用的本地化 key。")]
        public string DefaultPromptKey;
        [Tooltip("步驟文案的本地化 key：{0} 為目前步驟，{1} 為總步驟。")]
        public string StepFormatKey;
        [Tooltip("可選數量文案的本地化 key：{0} 為已選張數，{1} 為數量上限。")]
        public string OptionalCountFormatKey;
        [Tooltip("必選數量文案的本地化 key：{0} 為已選張數，{1} 為必要張數。")]
        public string RequiredCountFormatKey;
    }

    public interface ICardSelectionPanel
    {
        void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
        ICardSelectionSession Open(IReadOnlyReactiveProperty<CardSelectionDisplayState> state);
    }

    public class CardSelectionPanel : MonoBehaviour, ICardSelectionPanel
    {
        [SerializeField]
        private GameObject _panel;
        [SerializeField]
        private Transform _cardViewParent;
        [SerializeField]
        private CardViewFactory _cardViewFactory;


        [Header("Objects")]
        [SerializeField]
        private GameObject[] _visibleToggleObjects;
        [SerializeField]
        private GameObject _closeObject;

        [Header("Buttons")]
        [SerializeField]
        private Button[] _closeButtons;
        [SerializeField]
        private Button _visibleToggleButton;
        [SerializeField]
        private Button _confirmButton;

        [Header("Text")]
        [SerializeField]
        private TextMeshProUGUI _visibleButtonText;
        [SerializeField]
        private TextMeshProUGUI _confirmButtonText;
        [SerializeField]
        private TextMeshProUGUI _selectionDescriptionText;

        [Header("LocalizeKey")]
        [SerializeField]
        private string _visibleButtonLocalizeKey;
        [SerializeField]
        private string _confirmButtonLocalizeKey;
        [SerializeField]
        private CardSelectionTextSettings _textSettings;

        private IGameViewModel _gameViewModel;
        private LocalizeLibrary _localizeLibrary;
        private Session _activeSession;

        public void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary)
        {
            _gameViewModel = gameInfoModel;
            _localizeLibrary = localizeLibrary;

            _visibleButtonText.text = _localizeLibrary.Get(LocalizeInfoType.UI, _visibleButtonLocalizeKey).Info;
            _confirmButtonText.text = _localizeLibrary.Get(LocalizeInfoType.UI, _confirmButtonLocalizeKey).Info;
        }

        public ICardSelectionSession Open(IReadOnlyReactiveProperty<CardSelectionDisplayState> state)
        {
            if (_activeSession != null)
                throw new InvalidOperationException("選卡面板尚未釋放，不能重複開啟。");
            var session = new Session(this);
            _activeSession = session;
            try
            {
                _panel.SetActive(true);
                _closeObject.SetActive(true);
                _visibleToggleButton.interactable = true;
                session.Bind(state);
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }
        private void _RenderState(Session session, CardSelectionDisplayState state)
        {
            _SyncCandidates(session, state.Candidates);
            foreach (var obj in _visibleToggleObjects)
            {
                obj.SetActive(state.IsVisible);
            }

            _confirmButton.interactable = state.IsConfirmable;

            _selectionDescriptionText.text = _FormatDescription(state.Prompt);

            var selectedCardIdentities = state.SelectedCardIdentities.ToHashSet();
            foreach (var kvp in session.Cards)
            {
                if (selectedCardIdentities.Contains(kvp.Key))
                {
                    kvp.Value.View.OnSelect();
                }
                else
                {
                    kvp.Value.View.OnDeselect();
                }
            }
        }

        private void _SyncCandidates(Session session, IReadOnlyList<CardInfo> candidates)
        {
            var identities = candidates.Select(card => card.Identity).ToHashSet();
            foreach (var identity in session.Cards.Keys.Where(id => !identities.Contains(id)).ToArray())
            {
                _cardViewFactory.RecyclePrefab(session.Cards[identity].View);
                session.Cards.Remove(identity);
            }

            for (var index = 0; index < candidates.Count; index++)
            {
                var card = candidates[index];
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
                        (identity, _) => session.Click(identity),
                        (identity, _) => session.LongPress(identity)));
                    session.Cards[card.Identity] = (card, entry.View);
                }
                // 身份不變就沿用卡片，只依狀態中的順序排列。
                entry.View.transform.SetSiblingIndex(index);
            }
        }

        private string _FormatDescription(CardSelectionPromptInfo prompt)
        {
            var promptKey = string.Format(_textSettings.PromptKeyFormat, prompt.CardDataId, prompt.SelectionId);
            var description = _localizeLibrary.TryGet(LocalizeInfoType.UI, promptKey, out var localized)
                ? localized.Info : _Text(_textSettings.DefaultPromptKey);
            return string.Join("\n",
                description,
                string.Format(_Text(_textSettings.StepFormatKey), prompt.StepIndex + 1, prompt.StepCount),
                string.Format(_Text(prompt.CountRule == CardSelectionCountRule.Optional
                    ? _textSettings.OptionalCountFormatKey : _textSettings.RequiredCountFormatKey),
                    prompt.SelectedCount, prompt.MaxCount));
        }

        private string _Text(string key) => _localizeLibrary.Get(LocalizeInfoType.UI, key).Info;

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
                    // 場景銷毀時，面板物件可能已先被 Unity 移除。
                    if (_panel != null) _panel.SetActive(false);
                }
            }
        }

        private void OnDestroy() => _activeSession?.Dispose();

        private sealed class Session : ICardSelectionSession
        {
            public readonly Dictionary<Guid, (CardInfo Info, CardView View)> Cards = new();
            private readonly CardSelectionPanel _owner;
            private readonly CompositeDisposable _resources = new();
            private readonly Subject<Unit> _ended = new();
            private readonly Subject<Guid> _cardClicked = new();
            private readonly Subject<Guid> _cardLongPressed = new();
            private bool _isDisposed;

            public IObservable<Unit> CloseRequested { get; }
            public IObservable<Unit> ConfirmRequested { get; }
            public IObservable<Unit> VisibilityToggleRequested { get; }
            public IObservable<Guid> CardClicked { get; }
            public IObservable<Guid> CardLongPressed { get; }

            public Session(CardSelectionPanel owner)
            {
                _owner = owner;
                _ended.AddTo(_resources);
                _cardClicked.AddTo(_resources);
                _cardLongPressed.AddTo(_resources);
                CloseRequested = owner._closeButtons.Select(button => button.OnClickAsObservable())
                    .Merge().TakeUntil(_ended);
                ConfirmRequested = owner._confirmButton.OnClickAsObservable().TakeUntil(_ended);
                VisibilityToggleRequested = owner._visibleToggleButton.OnClickAsObservable().TakeUntil(_ended);
                CardClicked = _cardClicked.TakeUntil(_ended);
                CardLongPressed = _cardLongPressed.TakeUntil(_ended);
            }

            public void Bind(IReadOnlyReactiveProperty<CardSelectionDisplayState> state)
                => state.Subscribe(value => _owner._RenderState(this, value)).AddTo(_resources);

            public void Click(Guid identity)
            {
                if (!_isDisposed) _cardClicked.OnNext(identity);
            }

            public void LongPress(Guid identity)
            {
                if (!_isDisposed) _cardLongPressed.OnNext(identity);
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                try
                {
                    // 先結束操作事件，讓舊 Session 的訂閱無法收到下次開窗的按鈕輸入。
                    _ended.OnNext(Unit.Default);
                }
                finally
                {
                    try { _resources.Dispose(); }
                    finally { _owner._Close(this); }
                }
            }
        }
    }
}
