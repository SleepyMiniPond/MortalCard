using System;
using MortalGame.GameData;
using TMPro;
using UniRx;
using UnityEngine;
using MortalGame.UI;
using MortalGame.Presentation.Abstractions;
using MortalGame.GameModel;
using Optional;

namespace MortalGame.GameView
{

    public interface IAiCardView : IRecyclable, ISelectableView
    {
        void Initialize(IGameViewModel gameViewModel, LocalizeLibrary localizeLibrary);
        void SetCardInfo(CardInfo cardInfo);
        void SetPositionAndRotation(Vector3 position, Quaternion rotation);
    }

    public class AiCardView : MonoBehaviour, IAiCardView
    {
        [SerializeField]
        private RectTransform _rectTransform;
        [SerializeField]
        private TextMeshProUGUI _title;
        [SerializeField]
        private TextMeshProUGUI _info;
        [SerializeField]
        private TextMeshProUGUI _cost;
        [SerializeField]
        private TextMeshProUGUI _power;


        private readonly SerialDisposable _cardInfoSubscription = new();

        public RectTransform RectTransform => _rectTransform;
        public TargetType TargetType => TargetType.EnemyCard;
        public Guid TargetIdentity => _cardIdentity;

        private Guid _cardIdentity;
        private IGameViewModel _gameViewModel;
        private LocalizeLibrary _localizeLibrary;

        public void Initialize(
            IGameViewModel gameViewModel,
            LocalizeLibrary localizeLibrary)
        {
            _gameViewModel = gameViewModel;
            _localizeLibrary = localizeLibrary;
        }

        public void SetCardInfo(CardInfo cardInfo)
        {
            _cardInfoSubscription.Disposable = null;
            _Render(cardInfo);
            _gameViewModel.ObservableCardInfo(cardInfo.Identity)
                .MatchSome(infoProperty =>
                    _cardInfoSubscription.Disposable = infoProperty.Subscribe(_Render));
        }

        private void _Render(CardInfo cardInfo)
        {
            var cardLocalizeData = _localizeLibrary.Get(LocalizeTitleInfoType.Card, cardInfo.CardDataID);
            var templateValue = cardInfo.GetTemplateValues();

            _cardIdentity = cardInfo.Identity;
            _title.text = cardLocalizeData.Title;
            _info.text = cardLocalizeData.Info.ReplaceTemplateKeys(templateValue);
            _cost.text = cardInfo.Cost
                .Map(cost => cost.ToString())
                .ValueOr(string.Empty);
            _power.text = cardInfo.Power
                .Map(power => power.ToString())
                .ValueOr(string.Empty);
        }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetLocalPositionAndRotation(position, rotation);
        }

        public void Reset()
        {
            _cardInfoSubscription.Disposable = null;
            _cardIdentity = Guid.Empty;
        }

        private void OnDestroy()
        {
            _cardInfoSubscription.Dispose();
        }

        public void OnSelect()
        {
        }
        public void OnDeselect()
        {
        }
    }
}
