using System;
using MortalGame.GameData;

namespace MortalGame.GameModel
{

    public interface IEnergyManager
    {
        int Energy { get; }
        int MaxEnergy { get; }
        GainEnergyResult RecoverEnergy(int amount);
        LoseEnergyResult ConsumeEnergy(int amount);
        GainEnergyResult GainEnergy(int amount);
        LoseEnergyResult LoseEnergy(int amount);
        EnergyInfo ToInfo();
    }

    public record EnergyInfo(int CurrentEnergy, int MaxEnergy);

    public class EnergyManager : IEnergyManager
    {
        private int _energy;
        private readonly int _maxEnergy;

        public int Energy => _energy;
        public int MaxEnergy => _maxEnergy;

        public EnergyManager(int energy, int maxEnergy)
        {
            _maxEnergy = Math.Max(0, maxEnergy);
            _energy = Math.Min(_maxEnergy, Math.Max(0, energy));
        }

        public EnergyInfo ToInfo() => new EnergyInfo(_energy, _maxEnergy);

        public GainEnergyResult RecoverEnergy(int amount)
        {
            var validAmount = Math.Max(0, amount);
            var deltaEp = _AcceptEnergyGain(validAmount, out var energyOver);

            return new GainEnergyResult(
                Type: EnergyGainType.RoundStartRecover,
                EnergyPoint: validAmount,
                DeltaEp: deltaEp,
                OverEp: energyOver
            );
        }
        public LoseEnergyResult ConsumeEnergy(int amount)
        {
            var validAmount = Math.Max(0, amount);
            var deltaEp = _AcceptEnergyLoss(validAmount, out var energyOver);

            return new LoseEnergyResult(
                Type: EnergyLoseType.PlayCardConsume,
                EnergyPoint: validAmount,
                DeltaEp: deltaEp,
                OverEp: energyOver
            );
        }

        public GainEnergyResult GainEnergy(int amount)
        {
            var validAmount = Math.Max(0, amount);
            var deltaEp = _AcceptEnergyGain(validAmount, out var energyOver);

            return new GainEnergyResult(
                Type: EnergyGainType.GainEffect,
                EnergyPoint: validAmount,
                DeltaEp: deltaEp,
                OverEp: energyOver
            );
        }
        public LoseEnergyResult LoseEnergy(int amount)
        {
            var validAmount = Math.Max(0, amount);
            var deltaEp = _AcceptEnergyLoss(validAmount, out var energyOver);

            return new LoseEnergyResult(
                Type: EnergyLoseType.LoseEffect,
                EnergyPoint: validAmount,
                DeltaEp: deltaEp,
                OverEp: energyOver
            );
        }

        private int _AcceptEnergyGain(int amount, out int energyOver)
        {
            var originEnergy = _energy;
            if (!GameplayIntegerMath.Add(_energy, amount).TryGetValue(out var calculatedEnergy))
            {
                energyOver = amount;
                return 0;
            }

            _energy = Math.Min(_maxEnergy, Math.Max(originEnergy, calculatedEnergy));
            var deltaEnergy = _energy - originEnergy;
            energyOver = Math.Max(amount - deltaEnergy, 0);

            return deltaEnergy;
        }
        private int _AcceptEnergyLoss(int amount, out int energyOver)
        {
            var originEnergy = _energy;
            if (!GameplayIntegerMath.Subtract(_energy, amount).TryGetValue(out var calculatedEnergy))
            {
                energyOver = amount;
                return 0;
            }

            _energy = Math.Min(originEnergy, Math.Max(0, calculatedEnergy));
            var deltaEnergy = originEnergy - _energy;
            energyOver = Math.Max(amount - deltaEnergy, 0);

            return deltaEnergy;
        }
    }

}
