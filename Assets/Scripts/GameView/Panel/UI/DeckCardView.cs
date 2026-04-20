using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class DeckCardView : MonoBehaviour
{
    [SerializeField]
    private Button _deckButton;
    [SerializeField]
    private TextMeshProUGUI _deckCountText;

    public Button DeckButton => _deckButton;
    
    private IGameViewModel _gameViewModel;
    private CompositeDisposable _disposables = new CompositeDisposable();

    public void Init(IGameViewModel gameViewModel)
    {
        _gameViewModel = gameViewModel;

        _gameViewModel.ObservableCardCollectionInfo(Faction.Ally, CardCollectionType.Deck)
            .Subscribe(deckInfo => _deckCountText.text = deckInfo.Count.ToString())
            .AddTo(_disposables);
    }
}
