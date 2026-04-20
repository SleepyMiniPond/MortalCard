using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public interface ILevelMapView
{
    IDisposable RegisterActions(
        Action onClickLevel);
}

public class LevelMapView : MonoBehaviour, ILevelMapView
{
    [SerializeField] private Button _btn;

    public IDisposable RegisterActions(
        Action onClickLevel)
    {
        var disposable = new CompositeDisposable();
        _btn.OnClickAsObservable()
            .Subscribe(_ => onClickLevel?.Invoke())
            .AddTo(disposable);
        return disposable;
    }
}
