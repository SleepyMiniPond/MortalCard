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

        public static IEnumerable<TestCaseData> SharedCoreResolverTypes()
        {
            yield return new TestCaseData(typeof(DamageEffectResolver));
            yield return new TestCaseData(typeof(ShieldEffectResolver));
            yield return new TestCaseData(typeof(HealEffectResolver));
            yield return new TestCaseData(typeof(GainEnergyEffectResolver));
            yield return new TestCaseData(typeof(LoseEnergyEffectResolver));
            yield return new TestCaseData(typeof(IncreaseDispositionEffectResolver));
            yield return new TestCaseData(typeof(DecreaseDispositionEffectResolver));
            yield return new TestCaseData(typeof(DrawCardEffectResolver));
            yield return new TestCaseData(typeof(AddPlayerBuffEffectResolver));
            yield return new TestCaseData(typeof(ModifyPlayerBuffLevelEffectResolver));
            yield return new TestCaseData(typeof(RemovePlayerBuffEffectResolver));
            yield return new TestCaseData(typeof(AddCardBuffEffectResolver));
            yield return new TestCaseData(typeof(RemoveCardBuffEffectResolver));
            yield return new TestCaseData(typeof(DiscardCardEffectResolver));
            yield return new TestCaseData(typeof(ConsumeCardEffectResolver));
            yield return new TestCaseData(typeof(DisposeCardEffectResolver));
            yield return new TestCaseData(typeof(CreateCardEffectResolver));
            yield return new TestCaseData(typeof(CloneCardEffectResolver));
        }

        public static IEnumerable<TestCaseData> PlayerBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(DamageEffect));
            yield return new TestCaseData(typeof(ShieldEffect));
            yield return new TestCaseData(typeof(HealEffect));
            yield return new TestCaseData(typeof(GainEnergyEffect));
            yield return new TestCaseData(typeof(LoseEnegyEffect));
            yield return new TestCaseData(typeof(IncreaseDispositionEffect));
            yield return new TestCaseData(typeof(DecreaseDispositionEffect));
            yield return new TestCaseData(typeof(DrawCardEffect));
            yield return new TestCaseData(typeof(DiscardCardEffect));
            yield return new TestCaseData(typeof(ConsumeCardEffect));
            yield return new TestCaseData(typeof(DisposeCardEffect));
            yield return new TestCaseData(typeof(CreateCardEffect));
            yield return new TestCaseData(typeof(CloneCardEffect));
            yield return new TestCaseData(typeof(AddPlayerBuffEffect));
            yield return new TestCaseData(typeof(ModifyPlayerBuffLevelEffect));
            yield return new TestCaseData(typeof(RemovePlayerBuffEffect));
            yield return new TestCaseData(typeof(AddCardBuffEffect));
            yield return new TestCaseData(typeof(RemoveCardBuffEffect));
            yield return new TestCaseData(typeof(ModifyCardPlayAttributeEffect));
        }

        public static IEnumerable<TestCaseData> CharacterBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(ModifyCardPlayAttributeEffect));
            yield return new TestCaseData(typeof(DamageEffect));
            yield return new TestCaseData(typeof(ShieldEffect));
            yield return new TestCaseData(typeof(HealEffect));
            yield return new TestCaseData(typeof(GainEnergyEffect));
            yield return new TestCaseData(typeof(LoseEnegyEffect));
            yield return new TestCaseData(typeof(IncreaseDispositionEffect));
            yield return new TestCaseData(typeof(DecreaseDispositionEffect));
            yield return new TestCaseData(typeof(DrawCardEffect));
            yield return new TestCaseData(typeof(DiscardCardEffect));
            yield return new TestCaseData(typeof(ConsumeCardEffect));
            yield return new TestCaseData(typeof(DisposeCardEffect));
            yield return new TestCaseData(typeof(CreateCardEffect));
            yield return new TestCaseData(typeof(CloneCardEffect));
            yield return new TestCaseData(typeof(AddPlayerBuffEffect));
            yield return new TestCaseData(typeof(ModifyPlayerBuffLevelEffect));
            yield return new TestCaseData(typeof(RemovePlayerBuffEffect));
            yield return new TestCaseData(typeof(AddCardBuffEffect));
            yield return new TestCaseData(typeof(RemoveCardBuffEffect));
        }

        public static IEnumerable<TestCaseData> CardBuffEffectTypes()
        {
            yield return new TestCaseData(typeof(ModifyCardPlayAttributeEffect));
            yield return new TestCaseData(typeof(DamageEffect));
            yield return new TestCaseData(typeof(ShieldEffect));
            yield return new TestCaseData(typeof(HealEffect));
            yield return new TestCaseData(typeof(GainEnergyEffect));
            yield return new TestCaseData(typeof(LoseEnegyEffect));
            yield return new TestCaseData(typeof(IncreaseDispositionEffect));
            yield return new TestCaseData(typeof(DecreaseDispositionEffect));
            yield return new TestCaseData(typeof(DrawCardEffect));
            yield return new TestCaseData(typeof(DiscardCardEffect));
            yield return new TestCaseData(typeof(ConsumeCardEffect));
            yield return new TestCaseData(typeof(DisposeCardEffect));
            yield return new TestCaseData(typeof(CreateCardEffect));
            yield return new TestCaseData(typeof(CloneCardEffect));
            yield return new TestCaseData(typeof(AddPlayerBuffEffect));
            yield return new TestCaseData(typeof(ModifyPlayerBuffLevelEffect));
            yield return new TestCaseData(typeof(RemovePlayerBuffEffect));
            yield return new TestCaseData(typeof(AddCardBuffEffect));
            yield return new TestCaseData(typeof(RemoveCardBuffEffect));
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

        [TestCaseSource(nameof(SharedCoreResolverTypes))]
        public void SharedCoreResolver_ImplementsAllSourceResolverInterfaces(Type resolverType)
        {
            var resolver = Activator.CreateInstance(resolverType);

            Assert.That(resolver, Is.AssignableTo<ICardEffectResolver>());
            Assert.That(resolver, Is.AssignableTo<IPlayerBuffEffectResolver>());
            Assert.That(resolver, Is.AssignableTo<ICharacterBuffEffectResolver>());
            Assert.That(resolver, Is.AssignableTo<ICardBuffEffectResolver>());
        }

        [Test]
        public void CardFormOverrideEffect_DoesNotClaimBuffSourceSupport()
        {
            Assert.IsFalse(EffectDataResolver.HasPlayerBuffEffectResolver(typeof(ApplyCardFormOverrideEffect)));
            Assert.IsFalse(EffectDataResolver.HasCharacterBuffEffectResolver(typeof(ApplyCardFormOverrideEffect)));
            Assert.IsFalse(EffectDataResolver.HasCardBuffEffectResolver(typeof(ApplyCardFormOverrideEffect)));
        }

        [Test]
        public void UnregisteredMultiSourceEffect_DoesNotClaimBuffSourceSupport()
        {
            Assert.IsFalse(EffectDataResolver.HasPlayerBuffEffectResolver(typeof(UnregisteredMultiSourceEffect)));
            Assert.IsFalse(EffectDataResolver.HasCharacterBuffEffectResolver(typeof(UnregisteredMultiSourceEffect)));
            Assert.IsFalse(EffectDataResolver.HasCardBuffEffectResolver(typeof(UnregisteredMultiSourceEffect)));
        }

        private sealed class UnregisteredMultiSourceEffect :
            ICardEffect,
            IPlayerBuffEffect,
            ICharacterBuffEffect,
            ICardBuffEffect
        {
        }
    }
}
