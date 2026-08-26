using System;
using MortalGame.GameData;

namespace MortalGame.GameModel
{

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
            _maxHp = Math.Max(0, maxHealth);
            _hp = Math.Min(_maxHp, Math.Max(0, currentHealth));
            _dp = 0;
        }

        public TakeDamageResult TakeDamage(int amount, GameContext context, DamageType damageType)
        {
            var validAmount = Math.Max(0, amount);
            int deltaDp = 0;
            int deltaHp = 0;
            int damageOver = 0;

            switch (damageType)
            {
                case DamageType.Normal:
                case DamageType.Additional:
                    // Normal and Additional damage: first apply to armor, then to health
                    deltaDp = _AcceptArmorDamage(validAmount, out var damageRemain);
                    deltaHp = _AcceptHealthDamage(damageRemain, out damageOver);
                    break;

                case DamageType.Penetrate:
                case DamageType.Effective:
                    // Penetrate and Effective damage: directly apply to health, bypassing armor
                    deltaHp = _AcceptHealthDamage(validAmount, out damageOver);
                    deltaDp = 0;
                    break;

                default:
                    // Default to normal damage behavior
                    deltaDp = _AcceptArmorDamage(validAmount, out var remainingDamage);
                    deltaHp = _AcceptHealthDamage(remainingDamage, out damageOver);
                    break;
            }

            return new TakeDamageResult(
                Type: damageType,
                DamagePoint: validAmount,
                DeltaHp: deltaHp,
                DeltaDp: deltaDp,
                OverHp: damageOver
            );
        }

        public GetHealResult GetHeal(int amount, GameContext context)
        {
            var validAmount = Math.Max(0, amount);
            var deltaHp = _AcceptHealthHeal(validAmount, out var hpOver);

            return new GetHealResult(
                HealPoint: validAmount,
                DeltaHp: deltaHp,
                OverHp: hpOver
            );
        }
        public GetShieldResult GetShield(int amount, GameContext context)
        {
            var validAmount = Math.Max(0, amount);
            var deltaDp = _AcceptArmorGain(validAmount, out var dpOver);

            return new GetShieldResult(
                ShieldPoint: validAmount,
                DeltaDp: deltaDp,
                OverDp: dpOver
            );
        }

        private int _AcceptArmorDamage(int amount, out int damageRemain)
        {
            var originDp = _dp;
            if (!GameplayIntegerMath.Subtract(_dp, amount).TryGetValue(out var calculatedDp))
            {
                damageRemain = amount;
                return 0;
            }

            _dp = Math.Min(originDp, Math.Max(0, calculatedDp));
            var deltaDp = originDp - _dp;
            damageRemain = Math.Max(amount - deltaDp, 0);

            return deltaDp;
        }
        private int _AcceptHealthDamage(int amount, out int damageRemain)
        {
            var originHp = _hp;
            if (!GameplayIntegerMath.Subtract(_hp, amount).TryGetValue(out var calculatedHp))
            {
                damageRemain = amount;
                return 0;
            }

            _hp = Math.Min(originHp, Math.Max(0, calculatedHp));
            var deltaHp = originHp - _hp;
            damageRemain = Math.Max(amount - deltaHp, 0);

            return deltaHp;
        }

        private int _AcceptArmorGain(int amount, out int dpOver)
        {
            var originDp = _dp;
            if (!GameplayIntegerMath.Add(_dp, amount).TryGetValue(out var calculatedDp))
            {
                dpOver = amount;
                return 0;
            }

            _dp = Math.Min(_maxHp, Math.Max(originDp, calculatedDp));
            var deltaDp = _dp - originDp;
            dpOver = Math.Max(amount - deltaDp, 0);

            return deltaDp;
        }
        private int _AcceptHealthHeal(int amount, out int hpOver)
        {
            var originHp = _hp;
            if (!GameplayIntegerMath.Add(_hp, amount).TryGetValue(out var calculatedHp))
            {
                hpOver = amount;
                return 0;
            }

            _hp = Math.Min(_maxHp, Math.Max(originHp, calculatedHp));
            var deltaHp = _hp - originHp;
            hpOver = Math.Max(amount - deltaHp, 0);

            return deltaHp;
        }
    }

}
