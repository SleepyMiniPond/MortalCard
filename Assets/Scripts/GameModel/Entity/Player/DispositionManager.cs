using System;

namespace MortalGame.GameModel
{

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
            _maxDisposition = Math.Max(0, maxDisposition);
            _disposition = Math.Min(_maxDisposition, Math.Max(0, initialDisposition));
        }

        public IncreaseDispositionResult IncreaseDisposition(int deltaValue)
        {
            var validDeltaValue = Math.Max(0, deltaValue);
            var previousDisposition = _disposition;
            if (!GameplayIntegerMath.Add(previousDisposition, validDeltaValue)
                    .TryGetValue(out var calculatedDisposition))
            {
                return new IncreaseDispositionResult(0, 0, 0);
            }

            _disposition = Math.Min(_maxDisposition, calculatedDisposition);
            var deltaDisposition = _disposition - previousDisposition;
            var overDisposition = validDeltaValue - deltaDisposition;

            return new IncreaseDispositionResult(
                DispositionPoint: validDeltaValue,
                DeltaDisposition: deltaDisposition,
                OverDisposition: overDisposition
            );
        }

        public DecreaseDispositionResult DecreaseDisposition(int deltaValue)
        {
            var validDeltaValue = Math.Max(0, deltaValue);
            var previousDisposition = _disposition;
            if (!GameplayIntegerMath.Subtract(previousDisposition, validDeltaValue)
                    .TryGetValue(out var calculatedDisposition))
            {
                return new DecreaseDispositionResult(0, 0, 0);
            }

            _disposition = Math.Max(0, calculatedDisposition);
            var deltaDisposition = previousDisposition - _disposition;
            var overDisposition = validDeltaValue - deltaDisposition;

            return new DecreaseDispositionResult(
                DispositionPoint: validDeltaValue,
                DeltaDisposition: deltaDisposition,
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

}
