using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class DispositionView : MonoBehaviour
{
    [SerializeField]
    private Image _image;
    [SerializeField]
    private TextMeshProUGUI _dispositionText;

    private IGameViewModel _gameViewModel;
    private LocalizeLibrary _localizeLibrary;
    private DispositionLibrary _dispositionLibrary;
    private SimpleTitleInfoHintView _simpleHintView;

    public void Init(
        LocalizeLibrary localizeLibrary,
        DispositionLibrary dispositionLibrary,
        IGameViewModel gameViewModel,
        SimpleTitleInfoHintView simpleHintView)
    {
        _localizeLibrary = localizeLibrary;
        _dispositionLibrary = dispositionLibrary;
        _gameViewModel = gameViewModel;
        _simpleHintView = simpleHintView;

        _gameViewModel.ObservableDispositionInfo
            .Subscribe(info => _Render(info));

        _image.OnPointerEnterAsObservable()
            .WithLatestFrom(_gameViewModel.ObservableDispositionInfo, (_, info) => info)
            .Subscribe(info =>
            {
                var dispositionId = _dispositionLibrary.GetDispositionId(info.CurrentDisposition);
                var localizeInfo = _localizeLibrary.Get(LocalizeTitleInfoType.KeyWord, dispositionId);
                _simpleHintView.ShowTitleInfo(localizeInfo.Title, localizeInfo.Info, _image.rectTransform);
            })
            .AddTo(this);
        _image.OnPointerExitAsObservable()
            .Subscribe(_ => _simpleHintView.Close())
            .AddTo(this);
    }

    private void _Render(DispositionInfo info)
    {
        _image.fillAmount = (float)info.CurrentDisposition / info.MaxDisposition;

        var dispositionId = _dispositionLibrary.GetDispositionId(info.CurrentDisposition);
        var localizeInfo = _localizeLibrary.Get(LocalizeTitleInfoType.KeyWord, dispositionId);
        _dispositionText.text = localizeInfo.Title;
    }
}
