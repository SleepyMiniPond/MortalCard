using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MortalGame.Tests
{

    public class EffectDataResolverRegistryTests
    {
        public static IEnumerable<TestCaseData> CardEffectTypes()
        {
            yield return new TestCaseData(typeof(DamageEffect));
            yield return new TestCaseData(typeof(PenetrateDamageEffect));
            yield return new TestCaseData(typeof(AdditionalAttackEffect));
            yield return new TestCaseData(typeof(EffectiveAttackEffect));
            yield return new TestCaseData(typeof(ShieldEffect));
            yield return new TestCaseData(typeof(HealEffect));
            yield return new TestCaseData(typeof(GainEnergyEffect));
            yield return new TestCaseData(typeof(LoseEnegyEffect));
            yield return new TestCaseData(typeof(AddPlayerBuffEffect));
            yield return new TestCaseData(typeof(RemovePlayerBuffEffect));
            yield return new TestCaseData(typeof(IncreaseDispositionEffect));
            yield return new TestCaseData(typeof(DecreaseDispositionEffect));
            yield return new TestCaseData(typeof(DrawCardEffect));
            yield return new TestCaseData(typeof(DiscardCardEffect));
            yield return new TestCaseData(typeof(ConsumeCardEffect));
            yield return new TestCaseData(typeof(DisposeCardEffect));
            yield return new TestCaseData(typeof(CreateCardEffect));
            yield return new TestCaseData(typeof(CloneCardEffect));
            yield return new TestCaseData(typeof(AddCardBuffEffect));
            yield return new TestCaseData(typeof(RemoveCardBuffEffect));
        }

        public static IEnumerable<TestCaseData> PlayerBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(AdditionalDamagePlayerBuffEffect));
            yield return new TestCaseData(typeof(EffectiveDamagePlayerBuffEffect));
            yield return new TestCaseData(typeof(AddCardBuffPlayerBuffEffect));
            yield return new TestCaseData(typeof(RemoveCardBuffPlayerBuffEffect));
            yield return new TestCaseData(typeof(CardPlayEffectAttributeAdditionPlayerBuffEffect));
        }

        public static IEnumerable<TestCaseData> CharacterBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(EffectiveDamageCharacterBuffEffect));
        }

        [TestCaseSource(nameof(CardEffectTypes))]
        public void CardEffectType_HasResolver(Type effectType)
        {
            Assert.IsTrue(
                EffectDataResolver.HasCardEffectResolver(effectType),
                $"{effectType.Name} 缺少 ICardEffectResolver 註冊");
        }

        [TestCaseSource(nameof(PlayerBuffEffectTypes))]
        public void PlayerBuffEffectType_HasResolver(Type effectType)
        {
            Assert.IsTrue(
                EffectDataResolver.HasPlayerBuffEffectResolver(effectType),
                $"{effectType.Name} 缺少 IPlayerBuffEffectResolver 註冊");
        }

        [TestCaseSource(nameof(CharacterBuffEffectTypes))]
        public void CharacterBuffEffectType_HasResolver(Type effectType)
        {
            Assert.IsTrue(
                EffectDataResolver.HasCharacterBuffEffectResolver(effectType),
                $"{effectType.Name} 缺少 ICharacterBuffEffectResolver 註冊");
        }

        [Test]
        public void CardBuffEffectRegistry_CurrentlyHasNoConcreteEffectTypes()
        {
            Assert.Pass("目前 CardBuffEffect.cs 尚未定義具體 ICardBuffEffect 型別，因此此 registry 暫無必填註冊。");
        }
    }
}
