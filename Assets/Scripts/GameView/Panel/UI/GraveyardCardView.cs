using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class GraveyardCardView : MonoBehaviour
{
    [SerializeField]
    private Button _button;
    [SerializeField]
    private TextMeshProUGUI _graveyardCountText;

    public Button GraveyardButton => _button;

    private IGameViewModel _gameViewModel;
    private CompositeDisposable _disposables = new();

    public void Init(IGameViewModel gameViewModel)
    {
        _gameViewModel = gameViewModel;

        _gameViewModel.ObservableCardCollectionInfo(Faction.Ally, CardCollectionType.Graveyard)
            .Subscribe(graveyardInfo => _graveyardCountText.text = graveyardInfo.Count.ToString())
            .AddTo(_disposables);
    }
}
