using System;
using UnityEngine;

public interface IHealthManager
{
    int Hp { get; }
    int MaxHp { get; }
    int Dp { get; }
    TakeDamageResult TakeDamage(int amount, GameContext context, DamageType damageType);
    GetHealResult GetHeal(int amount, GameContext context);
    GetShieldResult GetShield(int amount, GameContext context);
}
public class HealthManager : IHealthManager
{
    private int _hp;
    private int _maxHp;
    private int _dp;

    public int Hp => _hp;
    public int MaxHp => _maxHp;
    public int Dp => _dp;

    public HealthManager(int currentHealth, int maxHealth)
    {
        _maxHp = maxHealth;
        _hp = currentHealth;
        _dp = 0;
    }

    public TakeDamageResult TakeDamage(int amount, GameContext context, DamageType damageType)
    {
        int deltaDp = 0;
        int deltaHp = 0;
        int damageOver = 0;

        switch (damageType)
        {
            case DamageType.Normal:
            case DamageType.Additional:
                // Normal and Additional damage: first apply to armor, then to health
                deltaDp = _AcceptArmorDamage(amount, out var damageRemain);
                deltaHp = _AcceptHealthDamage(damageRemain, out damageOver);
                break;

            case DamageType.Penetrate:
            case DamageType.Effective:
                // Penetrate and Effective damage: directly apply to health, bypassing armor
                deltaHp = _AcceptHealthDamage(amount, out damageOver);
                deltaDp = 0;
                break;

            default:
                // Default to normal damage behavior
                deltaDp = _AcceptArmorDamage(amount, out var remainingDamage);
                deltaHp = _AcceptHealthDamage(remainingDamage, out damageOver);
                break;
        }

        return new TakeDamageResult(
            Type: damageType,
            DamagePoint: amount,
            DeltaHp: deltaHp,
            DeltaDp: deltaDp,
            OverHp: damageOver
        );
    }

    public GetHealResult GetHeal(int amount, GameContext context)
    {
        var deltaHp = _AcceptHealthHeal(amount, out var hpOver);

        return new GetHealResult(
            HealPoint: amount,
            DeltaHp: deltaHp,
            OverHp: hpOver
        );
    }
    public GetShieldResult GetShield(int amount, GameContext context)
    {
        var deltaDp = _AcceptArmorGain(amount, out var dpOver);

        return new GetShieldResult(
            ShieldPoint: amount,
            DeltaDp: deltaDp,
            OverDp: dpOver
        );
    }

    private int _AcceptArmorDamage(int amount, out int damageRemain)
    { 
        var originDp = _dp;
        _dp = Mathf.Clamp(_dp - amount, 0, originDp);
        var deltaDp = originDp - _dp;
        damageRemain = Mathf.Max(amount - deltaDp, 0);

        return deltaDp;
    }
    private int _AcceptHealthDamage(int amount, out int damageRemain)
    { 
        var originHp = _hp;
        _hp = Mathf.Clamp(_hp - amount, 0, originHp);
        var deltaHp = originHp - _hp;
        damageRemain = Mathf.Max(amount - deltaHp, 0);

        return deltaHp;
    }

    private int _AcceptArmorGain(int amount, out int dpOver)
    {
        var originDp = _dp;
        _dp = Mathf.Clamp(_dp + amount, originDp, _maxHp);
        var deltaDp = _dp - originDp;
        dpOver = Mathf.Max(amount - deltaDp, 0);

        return deltaDp;
    }
    private int _AcceptHealthHeal(int amount, out int hpOver)
    {
        var originHp = _hp;
        _hp = Mathf.Clamp(_hp + amount, originHp, _maxHp);
        var deltaHp = _hp - originHp;
        hpOver = Mathf.Max(amount - deltaHp, 0);

        return deltaHp;
    }
}
