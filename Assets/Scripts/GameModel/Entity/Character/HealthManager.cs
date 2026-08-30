using System;
using MortalGame.GameData;

namespace MortalGame.GameModel
{

    public interface IHealthManager
    {
        int Hp { get; }
        int MaxHp { get; }
        int Shield { get; }
        TakeDamageResult TakeDamage(int amount, GameContext context, DamageType damageType);
        GetHealResult GetHeal(int amount, GameContext context);
        GetShieldResult GetShield(int amount, GameContext context);
    }
    public class HealthManager : IHealthManager
    {
        private int _hp;
        private int _maxHp;
        private int _shield;

        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public int Shield => _shield;

        public HealthManager(int currentHealth, int maxHealth)
        {
            _maxHp = Math.Max(0, maxHealth);
            _hp = Math.Min(_maxHp, Math.Max(0, currentHealth));
            _shield = 0;
        }

        public TakeDamageResult TakeDamage(int amount, GameContext context, DamageType damageType)
        {
            var validAmount = Math.Max(0, amount);
            int deltaShield = 0;
            int deltaHp = 0;
            int damageOver = 0;

            switch (damageType)
            {
                case DamageType.Normal:
                case DamageType.Additional:
                    // 一般與額外傷害會先由護盾吸收，再扣除生命。
                    deltaShield = _AcceptShieldDamage(validAmount, out var damageRemain);
                    deltaHp = _AcceptHealthDamage(damageRemain, out damageOver);
                    break;

                case DamageType.Penetrate:
                case DamageType.Effective:
                    // 穿透與有效傷害會略過護盾，直接扣除生命。
                    deltaHp = _AcceptHealthDamage(validAmount, out damageOver);
                    deltaShield = 0;
                    break;

                default:
                    // 未知類型沿用一般傷害行為。
                    deltaShield = _AcceptShieldDamage(validAmount, out var remainingDamage);
                    deltaHp = _AcceptHealthDamage(remainingDamage, out damageOver);
                    break;
            }

            return new TakeDamageResult(
                Type: damageType,
                DamagePoint: validAmount,
                DeltaHp: deltaHp,
                DeltaShield: deltaShield,
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
            var deltaShield = _AcceptShieldGain(validAmount, out var shieldOver);

            return new GetShieldResult(
                ShieldPoint: validAmount,
                DeltaShield: deltaShield,
                OverShield: shieldOver
            );
        }

        private int _AcceptShieldDamage(int amount, out int damageRemain)
        {
            var originShield = _shield;
            if (!GameplayIntegerMath.Subtract(_shield, amount).TryGetValue(out var calculatedShield))
            {
                damageRemain = amount;
                return 0;
            }

            _shield = Math.Min(originShield, Math.Max(0, calculatedShield));
            var deltaShield = originShield - _shield;
            damageRemain = Math.Max(amount - deltaShield, 0);

            return deltaShield;
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

        private int _AcceptShieldGain(int amount, out int shieldOver)
        {
            var originShield = _shield;
            if (!GameplayIntegerMath.Add(_shield, amount).TryGetValue(out var calculatedShield))
            {
                shieldOver = amount;
                return 0;
            }

            _shield = Math.Min(_maxHp, Math.Max(originShield, calculatedShield));
            var deltaShield = _shield - originShield;
            shieldOver = Math.Max(amount - deltaShield, 0);

            return deltaShield;
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
