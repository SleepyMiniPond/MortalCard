using System;
using MortalGame.GameModel;
using System.Collections.Generic;
using NUnit.Framework;
using MortalGame.GameData;

namespace MortalGame.Tests
{

    public class EffectDataResolverRegistryTests
    {
        public static IEnumerable<TestCaseData> CardEffectTypes()
        {
            yield return new TestCaseData(typeof(DamageEffect));
            yield return new TestCaseData(typeof(ShieldEffect));
            yield return new TestCaseData(typeof(HealEffect));
            yield return new TestCaseData(typeof(GainEnergyEffect));
            yield return new TestCaseData(typeof(LoseEnegyEffect));
            yield return new TestCaseData(typeof(AddPlayerBuffEffect));
            yield return new TestCaseData(typeof(ModifyPlayerBuffLevelEffect));
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
            yield return new TestCaseData(typeof(ApplyCardFormOverrideEffect));
        }

        public static IEnumerable<TestCaseData> PlayerBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(DamageEffect));
            yield return new TestCaseData(typeof(AddCardBuffPlayerBuffEffect));
            yield return new TestCaseData(typeof(RemoveCardBuffPlayerBuffEffect));
            yield return new TestCaseData(typeof(CardPlayEffectAttributeAdditionPlayerBuffEffect));
        }

        public static IEnumerable<TestCaseData> CharacterBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(DamageEffect));
        }

        public static IEnumerable<TestCaseData> CardBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(DamageEffect));
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

        [TestCaseSource(nameof(CardBuffEffectTypes))]
        public void CardBuffEffectType_HasResolver(Type effectType)
        {
            Assert.IsTrue(
                EffectDataResolver.HasCardBuffEffectResolver(effectType),
                $"{effectType.Name} 缺少 ICardBuffEffectResolver 註冊");
        }

        [Test]
        public void CardOnlyEffect_DoesNotClaimBuffSourceSupport()
        {
            Assert.IsFalse(EffectDataResolver.HasPlayerBuffEffectResolver(typeof(HealEffect)));
            Assert.IsFalse(EffectDataResolver.HasCharacterBuffEffectResolver(typeof(HealEffect)));
            Assert.IsFalse(EffectDataResolver.HasCardBuffEffectResolver(typeof(HealEffect)));
        }
    }
}
