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
        Button[] CloseButtons { get; }
        Button ConfirmButton { get; }
        Button VisibleToggleButton { get; }
        IObservable<Guid> CardClicked { get; }
        IObservable<Guid> CardLongPressed { get; }

        void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
        void Open(IReadOnlyReactiveProperty<CardSelectionDisplayState> state);
        void Close();
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

        public Button[] CloseButtons => _closeButtons;
        public Button ConfirmButton => _confirmButton;
        public Button VisibleToggleButton => _visibleToggleButton;
        public IObservable<Guid> CardClicked => _cardClicked;
        public IObservable<Guid> CardLongPressed => _cardLongPressed;

        private IGameViewModel _gameViewModel;
        private LocalizeLibrary _localizeLibrary;
        private readonly Dictionary<Guid, (CardInfo Info, CardView View)> _cardViewMap = new();
        private readonly SerialDisposable _stateSubscription = new();
        private readonly Subject<Guid> _cardClicked = new();
        private readonly Subject<Guid> _cardLongPressed = new();

        public void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary)
        {
            _gameViewModel = gameInfoModel;
            _localizeLibrary = localizeLibrary;

            _visibleButtonText.text = _localizeLibrary.Get(LocalizeInfoType.UI, _visibleButtonLocalizeKey).Info;
            _confirmButtonText.text = _localizeLibrary.Get(LocalizeInfoType.UI, _confirmButtonLocalizeKey).Info;
        }

        public void Open(IReadOnlyReactiveProperty<CardSelectionDisplayState> state)
        {
            _Clear();

            _panel.SetActive(true);
            _closeObject.SetActive(true);
            _visibleToggleButton.interactable = true;

            _stateSubscription.Disposable = state.Subscribe(_RenderState);
        }
        private void _RenderState(CardSelectionDisplayState state)
        {
            _SyncCandidates(state.Candidates);
            foreach (var obj in _visibleToggleObjects)
            {
                obj.SetActive(state.IsVisible);
            }

            _confirmButton.interactable = state.IsConfirmable;

            _selectionDescriptionText.text = _FormatDescription(state.Prompt);

            var selectedCardIdentities = state.SelectedCardIdentities.ToHashSet();
            foreach (var kvp in _cardViewMap)
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

        private void _SyncCandidates(IReadOnlyList<CardInfo> candidates)
        {
            var identities = candidates.Select(card => card.Identity).ToHashSet();
            foreach (var identity in _cardViewMap.Keys.Where(id => !identities.Contains(id)).ToArray())
            {
                _cardViewFactory.RecyclePrefab(_cardViewMap[identity].View);
                _cardViewMap.Remove(identity);
            }

            for (var index = 0; index < candidates.Count; index++)
            {
                var card = candidates[index];
                var needsRender = !_cardViewMap.TryGetValue(card.Identity, out var entry);
                if (needsRender)
                {
                    var view = _cardViewFactory.CreatePrefab();
                    entry = (card, view);
                    _cardViewMap.Add(card.Identity, entry);
                    view.transform.SetParent(_cardViewParent, false);
                    view.Initialize(_gameViewModel, _localizeLibrary);
                }
                if (needsRender || !Equals(entry.Info, card))
                {
                    entry.View.Render(new ICardView.CardClickableProperty(card, true,
                        (identity, _) => _cardClicked.OnNext(identity),
                        (identity, _) => _cardLongPressed.OnNext(identity)));
                    _cardViewMap[card.Identity] = (card, entry.View);
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

        public void Close()
        {
            _Clear();
            _panel.SetActive(false);
        }

        private void _Clear()
        {
            // 關閉或重新開窗時先解除舊綁定，避免繼續更新已回收的卡片。
            _stateSubscription.Disposable = null;
            foreach (var entry in _cardViewMap.Values)
            {
                _cardViewFactory.RecyclePrefab(entry.View);
            }

            _cardViewMap.Clear();
        }

        private void OnDestroy()
        {
            _stateSubscription.Dispose();
            _cardClicked.Dispose();
            _cardLongPressed.Dispose();
        }
    }
}
