using System;
using MortalGame.Exceptions;
using MortalGame.GameModel;
using System.Collections.Generic;
using NUnit.Framework;

namespace MortalGame.Tests
{

    public class EffectCommandHandlerRegistryTests
    {
        public static IEnumerable<TestCaseData> EffectCommandTypes()
        {
            yield return new TestCaseData(typeof(PlayCardEffectCommand));
            yield return new TestCaseData(typeof(DamageEffectCommand));
            yield return new TestCaseData(typeof(HealEffectCommand));
            yield return new TestCaseData(typeof(ShieldEffectCommand));
            yield return new TestCaseData(typeof(GainEnergyEffectCommand));
            yield return new TestCaseData(typeof(LoseEnergyEffectCommand));
            yield return new TestCaseData(typeof(IncreaseDispositionEffectCommand));
            yield return new TestCaseData(typeof(DecreaseDispositionEffectCommand));
            yield return new TestCaseData(typeof(AddPlayerBuffEffectCommand));
            yield return new TestCaseData(typeof(RemovePlayerBuffEffectCommand));
            yield return new TestCaseData(typeof(ModifyPlayerBuffLevelEffectCommand));
            yield return new TestCaseData(typeof(DrawCardEffectCommand));
            yield return new TestCaseData(typeof(MoveCardEffectCommand));
            yield return new TestCaseData(typeof(CreateCardEffectCommand));
            yield return new TestCaseData(typeof(CloneCardEffectCommand));
            yield return new TestCaseData(typeof(AddCardBuffEffectCommand));
            yield return new TestCaseData(typeof(RemoveCardBuffEffectCommand));
            yield return new TestCaseData(typeof(ModifyCardBuffLevelEffectCommand));
            yield return new TestCaseData(typeof(ApplyCardFormOverrideEffectCommand));
            yield return new TestCaseData(typeof(ModifyCardAttributeEffectCommand));
        }

        [TestCase(typeof(ApplyCardFormOverrideEffectCommandHandler))]
        [TestCase(typeof(EnergyEffectCommandHandler))]
        [TestCase(typeof(CardBuffEffectCommandHandler))]
        public void Handle_UnsupportedCommandReportsTypesBeforeAccessingContext(Type ownerType)
        {
            var handler = (IEffectCommandHandler)Activator.CreateInstance(ownerType);
            var command = new DamageEffectCommand(null, 0, default);

            var error = Assert.Throws<UnsupportedInputTypeException>(() => handler.Handle(null, command, null));

            Assert.That(error.OwnerType, Is.EqualTo(ownerType));
            Assert.That(error.ActualType, Is.EqualTo(command.GetType()));
        }

        [TestCaseSource(nameof(EffectCommandTypes))]
        public void EffectCommandType_HasHandler(Type commandType)
        {
            Assert.IsTrue(
                EffectCommandExecutor.HasEffectCommandHandler(commandType),
                $"{commandType.Name} 缺少 IEffectCommandHandler 註冊");
        }
    }
}
