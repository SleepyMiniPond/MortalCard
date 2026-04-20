using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

public class EnemySelectedCardView : MonoBehaviour
{
    [SerializeField]
    private AiCardViewFactory _cardViewFactory;
    [SerializeField]
    private Transform _cardViewParent;
    [SerializeField]
    private RectTransform _handCardArea;

    [Header("Deck")]
    [SerializeField]
    private Button _deckButton;
    [SerializeField]
    private TextMeshProUGUI _deckCountText;
    
    [SerializeField]
    private float _cardWidth = 100f;
    [SerializeField]
    private float _widthInterval = 20f;

    public IEnumerable<ISelectableView> SelectableViews => _cardViews;
    public Button DeckButton => _deckButton;

    private List<IAiCardView> _cardViews = new List<IAiCardView>();
    private Dictionary<Guid, IAiCardView> _cardViewDict = new Dictionary<Guid, IAiCardView>();

    private IGameplayModel _statusWatcher;
    private IGameplayActionReciever _reciever;
    private IGameViewModel _gameViewModel;
    private LocalizeLibrary _localizeLibrary;

    public void Init(
        IGameplayModel statusWatcher,
        IGameplayActionReciever reciever,
        IGameViewModel gameViewModel,
        LocalizeLibrary localizeLibrary)
    {
        _statusWatcher = statusWatcher;
        _reciever = reciever;
        _localizeLibrary = localizeLibrary;
        _gameViewModel = gameViewModel;

        _gameViewModel.ObservableCardCollectionInfo(Faction.Enemy, CardCollectionType.Deck)
            .Subscribe(deckInfo => _deckCountText.text = deckInfo.Count.ToString())
            .AddTo(this);
    }

    public void CreateCardView(EnemySelectCardEvent enemySelectCardEvent)
    {
        var cardView = _cardViewFactory.CreatePrefab();
        cardView.transform.SetParent(_cardViewParent, false);
        cardView.SetCardInfo(enemySelectCardEvent.SelectedCardInfo, _localizeLibrary);

        _cardViews.Add(cardView);
        _cardViewDict.Add(enemySelectCardEvent.SelectedCardInfo.Identity, cardView);
        if(_cardViews.Count > 0)
        {
            _RearrangeCardViews();
        }
    }
    public void CreateCardView(AddCardEvent addCardEvent)
    {
        var cardView = _cardViewFactory.CreatePrefab();
        cardView.transform.SetParent(_cardViewParent, false);
        cardView.SetCardInfo(addCardEvent.CardInfo, _localizeLibrary);

        _cardViews.Add(cardView);
        _cardViewDict.Add(addCardEvent.CardInfo.Identity, cardView);
        if(_cardViews.Count > 0)
        {
            _RearrangeCardViews();
        }
    }

    public void RemoveCardView(UsedCardEvent usedCardEvent)
    {
        if(_cardViewDict.TryGetValue(usedCardEvent.UsedCardInfo.Identity, out var cardView))
        {
            _cardViews.Remove(cardView);
            _cardViewDict.Remove(usedCardEvent.UsedCardInfo.Identity);
            _cardViewFactory.RecyclePrefab(cardView as AiCardView);

            _RearrangeCardViews();
        }
    }
    public void RemoveCardView(MoveCardEvent moveCardEvent)
    {
        if(_cardViewDict.TryGetValue(moveCardEvent.CardInfo.Identity, out var cardView))
        {
            _cardViews.Remove(cardView);
            _cardViewDict.Remove(moveCardEvent.CardInfo.Identity);
            _cardViewFactory.RecyclePrefab(cardView as AiCardView);

            _RearrangeCardViews();
        }
    }

    public void RemoveCardView(EnemyUnselectedCardEvent enemyUnselectedCardEvent)
    {
        foreach (var cardIdentity in enemyUnselectedCardEvent.UnselectedCards)
        {
            if (_cardViewDict.TryGetValue(cardIdentity, out var cardView))
            {
                _cardViews.Remove(cardView);
                _cardViewDict.Remove(cardIdentity);
                _cardViewFactory.RecyclePrefab(cardView as AiCardView);
            }
        }
        
        _RearrangeCardViews();
    }
    public void RemoveCardView(DiscardHandCardEvent recycleHandCardEvent)
    {
        foreach(var cardInfo in recycleHandCardEvent.DiscardedCardInfos.Concat(recycleHandCardEvent.ExcludedCardInfos))
        {
            if(_cardViewDict.TryGetValue(cardInfo.Identity, out var cardView))
            {
                _cardViews.Remove(cardView);
                _cardViewDict.Remove(cardInfo.Identity);
                _cardViewFactory.RecyclePrefab(cardView as AiCardView);
            }
        }

        _RearrangeCardViews();
    }

    private void _RearrangeCardViews()
    {
        var widthInterval = _handCardArea.rect.width / _cardViews.Count; 
        widthInterval = widthInterval > (_cardWidth + _widthInterval) ? (_cardWidth + _widthInterval) : widthInterval;

        for (var i = 0; i < _cardViews.Count; i++)
        {
            var cardView = _cardViews[i];
            var x = widthInterval * i - _handCardArea.rect.width / 2 + widthInterval / 2;
            cardView.SetPositionAndRotation( new Vector3(x, 0, 0), Quaternion.identity );
        }
    }
}
