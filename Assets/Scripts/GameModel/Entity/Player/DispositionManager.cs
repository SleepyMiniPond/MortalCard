using UnityEngine;

public interface IDispositionManager
{
    int CurrentDisposition { get; }
    int MaxDisposition { get; }

    IncreaseDispositionResult IncreaseDisposition(int deltaValue);
    DecreaseDispositionResult DecreaseDisposition(int deltaValue);
}

public record DispositionInfo(int CurrentDisposition, int MaxDisposition);

public class DispositionManager : IDispositionManager
{
    public int CurrentDisposition => _disposition;
    public int MaxDisposition => _maxDisposition;

    private int _disposition;
    private readonly int _maxDisposition;

    public DispositionManager(int initialDisposition, int maxDisposition)
    {
        _disposition = initialDisposition;
        _maxDisposition = maxDisposition;
    }

    public IncreaseDispositionResult IncreaseDisposition(int deltaValue)
    {
        var previousDisposition = _disposition;
        _disposition += deltaValue;

        int overDisposition = 0;
        if (_disposition > _maxDisposition)
        {
            overDisposition = _disposition - _maxDisposition;
            _disposition = _maxDisposition;
        }

        return new IncreaseDispositionResult(
            DispositionPoint: deltaValue,
            DeltaDisposition: _disposition - previousDisposition,
            OverDisposition: overDisposition
        );
    }

    public DecreaseDispositionResult DecreaseDisposition(int deltaValue)
    {
        var previousDisposition = _disposition;
        _disposition -= deltaValue;

        int overDisposition = 0;
        if (_disposition < 0)
        {
            overDisposition = -_disposition;
            _disposition = 0;
        }

        return new DecreaseDispositionResult(
            DispositionPoint: deltaValue,
            DeltaDisposition: previousDisposition - _disposition,
            OverDisposition: overDisposition
        );
    }
}

public static class DispositionUtility
{
    public static DispositionInfo ToInfo(this IDispositionManager dispositionManager)
    {
        return new DispositionInfo(
            dispositionManager.CurrentDisposition,
            dispositionManager.MaxDisposition);
    }
}