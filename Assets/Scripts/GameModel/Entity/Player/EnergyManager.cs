using UnityEngine;

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
    private int _maxEnergy;

    public int Energy => _energy;
    public int MaxEnergy => _maxEnergy;

    public EnergyManager(int energy, int maxEnergy)
    {
        _energy = energy;
        _maxEnergy = maxEnergy;
    }

    public EnergyInfo ToInfo() => new EnergyInfo(_energy, _maxEnergy);

    public GainEnergyResult RecoverEnergy(int amount)
    {
        var deltaEp = _AcceptEnergyGain(amount, out var energyOver);

        return new GainEnergyResult(
            Type: EnergyGainType.RoundStartRecover,
            EnergyPoint: amount,
            DeltaEp: deltaEp,
            OverEp: energyOver
        );
    }
    public LoseEnergyResult ConsumeEnergy(int amount)
    {
        var deltaEp = _AcceptEnergyLoss(amount, out var energyOver);

        return new LoseEnergyResult(
            Type: EnergyLoseType.PlayCardConsume,
            EnergyPoint: amount,
            DeltaEp: deltaEp,
            OverEp: energyOver
        );
    }

    public GainEnergyResult GainEnergy(int amount)
    {
        var deltaEp = _AcceptEnergyGain(amount, out var energyOver);

        return new GainEnergyResult(
            Type: EnergyGainType.GainEffect,
            EnergyPoint: amount,
            DeltaEp: deltaEp,
            OverEp: energyOver
        );
    }
    public LoseEnergyResult LoseEnergy(int amount)
    {
        var deltaEp = _AcceptEnergyLoss(amount, out var energyOver);

        return new LoseEnergyResult(
            Type: EnergyLoseType.LoseEffect,
            EnergyPoint: amount,
            DeltaEp: deltaEp,
            OverEp: energyOver
        );
    }

    private int _AcceptEnergyGain(int amount, out int energyOver)
    {
        var originEnergy = _energy;
        _energy = Mathf.Clamp(_energy + amount, originEnergy, _maxEnergy);
        var deltaEnergy = _energy - originEnergy;
        energyOver = Mathf.Max(amount - deltaEnergy, 0);

        return deltaEnergy;
    }
    private int _AcceptEnergyLoss(int amount, out int energyOver)
    {
        var originEnergy = _energy;
        _energy = Mathf.Clamp(_energy - amount, 0, originEnergy);
        var deltaEnergy = originEnergy - _energy;
        energyOver = Mathf.Max(amount - deltaEnergy, 0);

        return deltaEnergy;
    }
}