using System;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBuffView : MonoBehaviour, IRecyclable
{
    [SerializeField]
    private Image _buffIcon;

    [SerializeField]
    private TextMeshProUGUI _levelText;

    [SerializeField]
    private RectTransform _rectTransform;
    
    private CompositeDisposable _disposables = new();
    
    private IGameViewModel _gameViewModel;
    private LocalizeLibrary _localizeLibrary;

    public void Initialize(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary)
    {
        _gameViewModel = gameInfoModel;
        _localizeLibrary = localizeLibrary;
    }

    public void SetBuffInfo(
        PlayerBuffInfo buffInfo,
        SimpleTitleInfoHintView simpleHintView)
    {
        _Render(buffInfo);

        _disposables.Dispose();
        // Disposed object can't be reused by same instance.
        _disposables = new CompositeDisposable();

        _gameViewModel.ObservablePlayerBuffInfo(buffInfo.Identity)
            .MatchSome(reactiveProp =>
            {
                reactiveProp
                    .Subscribe(info => _Render(info))
                    .AddTo(_disposables);

                _buffIcon.OnPointerEnterAsObservable()
                    .WithLatestFrom(reactiveProp, (_, info) => info)
                    .Subscribe(info => _ShowBuffInfo(simpleHintView, info))
                    .AddTo(_disposables);
            });        
        
        _buffIcon.OnPointerExitAsObservable()
            .Subscribe(_ => simpleHintView.Close())
            .AddTo(_disposables);
    }

    private void _Render(PlayerBuffInfo buffInfo)
    {
        _levelText.text = buffInfo.Level.ToString();
    }
    
    private void _ShowBuffInfo(SimpleTitleInfoHintView simpleHintView, PlayerBuffInfo buffInfo)
    { 
        var localizeData = _localizeLibrary.Get(LocalizeTitleInfoType.PlayerBuff, buffInfo.Id);
        var templateValue = buffInfo.GetTemplateValues();
        var title = localizeData.Title;
        var info = localizeData.Info.ReplaceTemplateKeys(templateValue);

        simpleHintView.ShowTitleInfo(title, info, _rectTransform);
    }

    public void Reset()
    {
        _buffIcon.sprite = null;
        _levelText.text = string.Empty;

        _disposables.Dispose();
        // Disposed object can't be reused by same instance.
        _disposables = new CompositeDisposable();
    }
}